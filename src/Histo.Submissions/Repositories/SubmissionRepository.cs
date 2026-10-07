using Dapper;
using Histo.Core.Domain;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using System.Data;
using System.Globalization;

namespace Histo.Submissions.Repositories;

/// <summary>
/// Dapper implementation of <see cref="ISubmissionRepository"/>.
/// Covers batch submissions (sample groups), animals, and tissues.
/// </summary>
public sealed class SubmissionRepository : ISubmissionRepository
{
    private const int MaxMouseRangeEntries = 1000;
    private readonly IDbConnectionFactory _db;

    public SubmissionRepository(IDbConnectionFactory db) => _db = db;

    // -----------------------------------------------------------------------
    // Batch Submissions
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchSubmission>> GetSubmissionsByBatchAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        // GetBatchSubmissionDetailsByBatchID returns 3 result sets:
        //   0 = BATCH_SUBMISSION_TABLE, 1 = BATCH_TISSUES_TABLE, 2 = BATCH_ANIMAL_TABLE.
        // Legacy assembles these into a DataSet already containing 6 common-batch tables,
        // giving assembled indices 6/7/8, but within this SP submissions are at index 0.
        using var multi = await conn.QueryMultipleAsync(
            "GetBatchSubmissionDetailsByBatchID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);

        var rows = await multi.ReadAsync<BatchSubmission>();
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<int> AddSubmissionAsync(BatchSubmission submission, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        // Legacy SP signature: ID (0 = new), BatchID, AnimalID, Order, OldID (out), NewID (out).
        // AnimalID is NOT NULL on the table; legacy's typed DataSet defaulted new rows to 0
        // when no animal is known yet (the "default empty submission" case), but
        // clsBatchSubmission.vb::NewRecord(dtBatchSubmission, id, batchId, animalId) shows the SP
        // does accept a real AnimalID once one is known — pass submission.AnimalID (defaults to 0)
        // rather than always hardcoding 0.
        parameters.Add("ID",        0,                      dbType: System.Data.DbType.Int32);
        parameters.Add("BatchID",   submission.BatchID,     dbType: System.Data.DbType.Int32);
        parameters.Add("AnimalID",  submission.AnimalID,    dbType: System.Data.DbType.Int32);
        parameters.Add("Order",     submission.Order,       dbType: System.Data.DbType.Int32);
        parameters.Add("OldID",     dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        parameters.Add("NewID",     dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync("AddBatchSubmission", parameters,
            commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int>("NewID");
    }

    /// <inheritdoc/>
    public async Task<bool> CreateMouseRangeAsync(int batchId, int? sourceAnimalId, string mouseNumberFrom, string mouseNumberTo, int userId, CancellationToken ct = default)
    {
        if (!TryGetMouseRangeBounds(mouseNumberFrom, mouseNumberTo, out var fromId, out var toId))
            return false;

        var rangeSize = toId - fromId + 1;
        if (rangeSize > MaxMouseRangeEntries)
            return false;

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            var siblings = await GetSubmissionsByBatchAsync(conn, tx, batchId);
            var nextOrder = siblings.Count > 0 ? siblings.Max(s => s.Order) + 1 : 1;
            var sourceTissues = await GetSourceTissuesAsync(conn, tx, batchId, sourceAnimalId, siblings);

            foreach (var currentNumber in Enumerable.Range(fromId, rangeSize))
            {
                var mouseNumber = SenderRefHelpers.FormatMouseNumber(currentNumber);
                var animalId = await CreateMouseAnimalAsync(conn, tx, mouseNumber);
                var submissionId = await AddSubmissionAsync(conn, tx,
                    new BatchSubmission { BatchID = batchId, AnimalID = animalId, SubmissionName = "Default", Order = nextOrder++ });

                await CopySourceTissuesAsync(conn, tx, sourceTissues, submissionId);
            }

            await tx.CommitAsync(ct);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> CreateBatchWithCopiedSamplesAsync(
        Batch batch,
        IReadOnlyList<string> histologyCodes,
        IReadOnlyList<string> antibodyCodes,
        IReadOnlyList<string> stainCodes,
        string? submittedAsCode,
        IReadOnlyList<CopiedSamplePlan> plan,
        int userId,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        // Names the statement in flight so a SQL failure can say which step broke.
        var step = "create the submission header";

        try
        {
            var newBatchId = await AddBatchAsync(conn, tx, batch, userId);
            if (newBatchId <= 0)
                throw new InvalidOperationException("Failed to create the new submission header.");

            step = "save the histology selections";
            foreach (var code in histologyCodes.Distinct(StringComparer.OrdinalIgnoreCase))
                await conn.ExecuteAsync("AddHistology", new { BatchID = newBatchId, Code = code, UserID = userId }, tx, commandType: System.Data.CommandType.StoredProcedure);
            step = "save the antibody selections";
            foreach (var code in antibodyCodes.Distinct(StringComparer.OrdinalIgnoreCase))
                await conn.ExecuteAsync("AddAntibodies", new { BatchID = newBatchId, Code = code, UserID = userId }, tx, commandType: System.Data.CommandType.StoredProcedure);
            step = "save the special stain selections";
            foreach (var code in stainCodes.Distinct(StringComparer.OrdinalIgnoreCase))
                await conn.ExecuteAsync("AddSpecialStain", new { BatchID = newBatchId, Code = code, UserID = userId }, tx, commandType: System.Data.CommandType.StoredProcedure);
            step = "save the submission type";
            if (!string.IsNullOrWhiteSpace(submittedAsCode))
                await conn.ExecuteAsync("AddSubmittedAs", new { BatchID = newBatchId, Code = submittedAsCode, UserID = userId }, tx, commandType: System.Data.CommandType.StoredProcedure);

            foreach (var sample in plan)
            {
                var animal = new Animal
                {
                    BatchSubmissionID  = 0,
                    SenderRef          = sample.NewSenderRef,
                    NextBlockRef       = sample.SourceAnimal.NextBlockRef,
                    // Never the source's own ref — AddAnimal silently declines a duplicate.
                    HistologyRef       = sample.NewHistologyRef,
                    HistoRefSet        = !string.IsNullOrWhiteSpace(sample.NewHistologyRef),
                    BookedHistologyRef = false,
                    OnHold             = sample.SourceAnimal.OnHold,
                    PMDate             = sample.SourceAnimal.PMDate,
                    PMDateSet          = sample.SourceAnimal.PMDateSet,
                    IsPGNumber         = sample.SourceAnimal.IsPGNumber,
                };

                int newAnimalId;
                try
                {
                    step = $"create sample {sample.NewSenderRef}";
                    newAnimalId = await AddAnimalAsync(conn, tx, animal);
                }
                catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
                {
                    throw new InvalidOperationException(
                        $"A sample with sender reference '{sample.NewSenderRef}' already exists. Change it on the Copy submission page and try again.", ex);
                }

                if (newAnimalId <= 0)
                    throw new InvalidOperationException(
                        $"Could not create sample '{sample.NewSenderRef}' — the sender reference may already be in use. Change it on the Copy submission page and try again.");

                var submission = new BatchSubmission
                {
                    BatchID        = newBatchId,
                    AnimalID       = newAnimalId,
                    SubmissionName = sample.SourceSubmission.SubmissionName,
                    Order          = sample.SourceSubmission.Order,
                };

                step = $"link sample {sample.NewSenderRef} to the submission";
                var newSubmissionId = await AddSubmissionAsync(conn, tx, submission);
                if (newSubmissionId <= 0)
                    throw new InvalidOperationException($"Failed to create the submission record for {sample.NewSenderRef}.");

                step = $"copy the tissues for sample {sample.NewSenderRef}";
                await CopySourceTissuesAsync(conn, tx, sample.Tissues, newSubmissionId);

                foreach (var block in sample.Blocks)
                {
                    step = $"copy block {block.BlockRef} for sample {sample.NewSenderRef}";
                    var newBlockId = await AddBlockAsync(conn, tx, newBatchId, newAnimalId, block);
                    if (newBlockId <= 0)
                        throw new InvalidOperationException(
                            $"Could not copy block '{block.BlockRef}' for sample '{sample.NewSenderRef}'.");

                    await CopySourceTissuesAsync(conn, tx, block.Tissues, newBlockId);

                    step = $"copy the test selections for block {block.BlockRef}";
                    await AddBlockTestsAsync(conn, tx, newBatchId, newBlockId, userId, block);
                }
            }

            await tx.CommitAsync(ct);
            return newBatchId;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            await tx.RollbackAsync(ct);
            // Names the failing step and the SQL error number only — never the raw SQL text.
            throw new InvalidOperationException($"Could not {step} (database error {ex.Number}). Nothing was saved.", ex);
        }
        catch
        {
            // Rethrow rather than returning 0: SubmissionService logs the real failure, which a
            // silent 0 hid behind a generic "Failed to create the submission" page error.
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Calls the <c>AddBatch</c> SP within an existing transaction, including the OUTPUT-param/
    /// RETURN_VALUE fallback already needed by <see cref="Histo.Submissions.Repositories.BatchRepository.AddAsync"/>
    /// for the same SP (deployment-dependent shape).
    /// </summary>
    private static async Task<int> AddBatchAsync(IDbConnection conn, IDbTransaction tx, Batch batch, int userId)
    {
        var p = BatchRepository.BuildAddBatchParams(batch, userId);
        p.Add("BatchID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        try
        {
            await conn.ExecuteAsync("AddBatch", p, tx, commandType: System.Data.CommandType.StoredProcedure);
            var batchId = p.Get<int>("BatchID");
            if (batchId > 0) return batchId;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 8144)
        {
            var p2 = BatchRepository.BuildAddBatchParams(batch, userId);
            p2.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
            await conn.ExecuteAsync("AddBatch", p2, tx, commandType: System.Data.CommandType.StoredProcedure);
            return p2.Get<int>("RETURN_VALUE");
        }

        return 0;
    }

    private static bool TryGetMouseRangeBounds(string mouseNumberFrom, string mouseNumberTo, out int fromId, out int toId)
    {
        if (!ValidationHelpers.ValidateMouseNumber(mouseNumberFrom)
            || !ValidationHelpers.ValidateMouseNumber(mouseNumberTo)
            || !SenderRefHelpers.TryParseMouseNumber(mouseNumberFrom, out fromId)
            || !SenderRefHelpers.TryParseMouseNumber(mouseNumberTo, out toId))
        {
            fromId = 0;
            toId = 0;
            return false;
        }

        if (fromId >= toId)
        {
            fromId = 0;
            toId = 0;
            return false;
        }

        return true;
    }

    private static async Task<IReadOnlyList<Tissue>> GetSourceTissuesAsync(IDbConnection conn, IDbTransaction tx, int batchId, int? sourceAnimalId, IReadOnlyList<BatchSubmission> siblings)
    {
        if (sourceAnimalId is not > 0)
            return [];

        var sourceSubmission = siblings.FirstOrDefault(s => s.AnimalID == sourceAnimalId.Value);
        return sourceSubmission is null ? [] : await GetTissuesBySubmissionAsync(conn, tx, batchId, sourceSubmission.ID);
    }

    private static async Task<int> CreateMouseAnimalAsync(IDbConnection conn, IDbTransaction tx, string mouseNumber)
    {
        var animalId = await AddAnimalAsync(conn, tx, new Animal
        {
            BatchSubmissionID = 0,
            SenderRef = mouseNumber,
            NextBlockRef = "01",
            HistologyRef = null,
            HistoRefSet = false,
            OnHold = false,
            PMDate = null,
            PMDateSet = false,
            IsPGNumber = false,
            BookedHistologyRef = false,
        });

        if (animalId <= 0)
            throw new InvalidOperationException($"Failed to create animal {mouseNumber}.");

        return animalId;
    }

    /// <summary>
    /// Copies tissues onto a new owner. The result of each insert is deliberately not checked:
    /// AddTissue/AddBlockTissue have no identity OUTPUT param, so their RETURN_VALUE is the SP's
    /// status code (0 = success). A genuine failure raises a SqlException, which rolls back.
    /// </summary>
    private static async Task CopySourceTissuesAsync(IDbConnection conn, IDbTransaction tx, IReadOnlyList<Tissue> sourceTissues, int newOwnerId)
    {
        foreach (var tissue in sourceTissues)
            await CopyTissueAsync(conn, tx, tissue, newOwnerId);
    }

    private static async Task<IReadOnlyList<BatchSubmission>> GetSubmissionsByBatchAsync(IDbConnection conn, IDbTransaction tx, int batchId)
    {
        using var multi = await conn.QueryMultipleAsync(
            "GetBatchSubmissionDetailsByBatchID",
            new { ID = batchId },
            transaction: tx,
            commandType: System.Data.CommandType.StoredProcedure);

        var rows = await multi.ReadAsync<BatchSubmission>();
        return rows.ToList();
    }

    private static async Task<IReadOnlyList<Tissue>> GetTissuesBySubmissionAsync(IDbConnection conn, IDbTransaction tx, int batchId, int submissionId)
    {
        var rows = await conn.QueryAsync<dynamic>(
            "GetBatchTissues",
            new { ID = batchId },
            transaction: tx,
            commandType: System.Data.CommandType.StoredProcedure);

        return rows
            .Select(r => (IDictionary<string, object>)r)
            .Where(d => d.TryGetValue("BatchSubmissionID", out var bsid) && Convert.ToInt32(bsid) == submissionId)
            .Select(d => MapTissueRow(d, submissionId, TissueOwner.Submission))
            .ToList();
    }

    private static Tissue MapTissueRow(IDictionary<string, object> d, int ownerId, TissueOwner owner)
    {
        var tissueCode = d.TryGetValue("TissueCode", out var tc) ? Convert.ToString(tc)?.Trim() ?? string.Empty : string.Empty;
        DateTime? archivedDate = d.TryGetValue("ArchivedDate", out var ad) && ad is not DBNull
            && DateTime.TryParse(Convert.ToString(ad), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
            ? parsed
            : null;

        return new Tissue
        {
            ID = d.TryGetValue("ID", out var id) ? Convert.ToInt32(id) : 0,
            OwnerID = ownerId,
            Owner = owner,
            TissueCode = tissueCode,
            NoPieces = d.TryGetValue("NoPieces", out var np) ? Convert.ToInt16(np) : (short)0,
            Comment = d.TryGetValue("Comment", out var cm) && cm is not DBNull ? Convert.ToString(cm) : null,
            ArchiveLocation = d.TryGetValue("ArchiveLocation", out var al) && al is not DBNull ? Convert.ToString(al) : null,
            ArchivedDate = archivedDate,
            ArchiveComment = d.TryGetValue("ArchiveComment", out var ac) && ac is not DBNull ? Convert.ToString(ac) : null,
            RowStamp = d.TryGetValue("RowStamp", out var rs) ? rs as byte[] : null,
        };
    }

    private static async Task<int> AddSubmissionAsync(IDbConnection conn, IDbTransaction tx, BatchSubmission submission)
    {
        var parameters = new DynamicParameters();
        parameters.Add("ID", 0, dbType: System.Data.DbType.Int32);
        parameters.Add("BatchID", submission.BatchID, dbType: System.Data.DbType.Int32);
        parameters.Add("AnimalID", submission.AnimalID, dbType: System.Data.DbType.Int32);
        parameters.Add("Order", submission.Order, dbType: System.Data.DbType.Int32);
        parameters.Add("OldID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        parameters.Add("NewID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync("AddBatchSubmission", parameters, transaction: tx, commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int>("NewID");
    }

    private static async Task<int> AddAnimalAsync(IDbConnection conn, IDbTransaction tx, Animal animal)
    {
        var parameters = new DynamicParameters();
        parameters.Add("SenderRef", animal.SenderRef);
        parameters.Add("HistologyRef", animal.HistologyRef, dbType: System.Data.DbType.String);
        parameters.Add("NextBlockRef", animal.NextBlockRef);
        // Unlike EditAnimal's, this SP's @PMDate takes the legacy dd/MM/yyyy string as-is —
        // parsing it to a DateTime here breaks every insert.
        parameters.Add("PMDate", (object?)animal.PMDate ?? DBNull.Value, dbType: System.Data.DbType.String);
        parameters.Add("OnHold", animal.OnHold);
        parameters.Add("NewID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync("AddAnimal", parameters, transaction: tx, commandType: System.Data.CommandType.StoredProcedure);
        // AddAnimal leaves @NewID NULL when it declines the insert (e.g. the SenderRef or
        // HistologyRef is already in use) rather than raising an error.
        return parameters.Get<int?>("NewID") ?? 0;
    }

    /// <summary>
    /// Calls the <c>AddBlock</c> SP inside the copy transaction — mirrors
    /// <c>BlockRepository.SaveAsync</c>'s insert branch (no @UserID; new id via @NewID output).
    /// </summary>
    private static async Task<int> AddBlockAsync(IDbConnection conn, IDbTransaction tx, int newBatchId, int newAnimalId, CopiedBlockPlan block)
    {
        var parameters = new DynamicParameters();
        parameters.Add("ID", 0);
        parameters.Add("BatchID", newBatchId);
        parameters.Add("AnimalID", newAnimalId);
        parameters.Add("BlockRef", block.BlockRef);
        parameters.Add("CustomerRef", block.CustomerRef);
        parameters.Add("RepeatBlock", block.RepeatBlock);
        parameters.Add("Comment", block.Comment);
        // Legacy clsBlock.NewBlock always stamps a copied block STATUS_USED (1) rather than
        // carrying the source block's own status across.
        parameters.Add("Status", 1);
        parameters.Add("Order", block.Order);
        parameters.Add("OldID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        parameters.Add("NewID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync("AddBlock", parameters, transaction: tx, commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int?>("NewID") ?? 0;
    }

    /// <summary>
    /// Inserts a copied block's test-type ticks using the same SPs as
    /// <c>BlockTestRepository.SaveTestSelectionsAsync</c> — a new block has no existing rows, so
    /// this is the insert half of that delta, run inside the copy transaction.
    /// </summary>
    private static async Task AddBlockTestsAsync(IDbConnection conn, IDbTransaction tx, int newBatchId, int newBlockId, int userId, CopiedBlockPlan block)
    {
        await AddCodesAsync(block.HistologyCodes, "AddBlockHistology");
        await AddCodesAsync(block.AntibodyCodes, "AddBlockAntibodies");
        await AddCodesAsync(block.StainCodes, "AddBlockStain");

        async Task AddCodesAsync(IReadOnlyList<string> codes, string addSp)
        {
            foreach (var code in codes.Distinct(StringComparer.OrdinalIgnoreCase))
                await conn.ExecuteAsync(addSp,
                    new { BlockID = newBlockId, Code = code, Comment = (string?)null, UserID = userId, BatchID = newBatchId },
                    tx, commandType: System.Data.CommandType.StoredProcedure);
        }
    }

    private static async Task<int> CopyTissueAsync(IDbConnection conn, IDbTransaction tx, Tissue source, int newOwnerId)
    {
        var tissue = new Tissue
        {
            OwnerID = newOwnerId,
            Owner = source.Owner,
            TissueCode = source.TissueCode,
            NoPieces = source.NoPieces,
            Comment = source.Comment,
        };

        return await AddTissueAsync(conn, tx, tissue);
    }

    private static async Task<int> AddTissueAsync(IDbConnection conn, IDbTransaction tx, Tissue tissue)
    {
        var procName = tissue.Owner == TissueOwner.Submission ? "AddTissue" : "AddBlockTissue";
        var keyParam = tissue.Owner == TissueOwner.Submission ? "BatchSubmissionID" : "BlockID";

        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add(keyParam, tissue.OwnerID);
        parameters.Add("TissueCode", tissue.TissueCode);
        parameters.Add("NoPieces", tissue.NoPieces);
        parameters.Add("Comment", (object?)tissue.Comment ?? DBNull.Value, dbType: System.Data.DbType.String);

        await conn.ExecuteAsync(procName, parameters, transaction: tx, commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int>("RETURN_VALUE");
    }

    /// <inheritdoc/>
    public async Task UpdateSubmissionAsync(BatchSubmission submission, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "EditBatchSubmission",
            new
            {
                submission.ID,
                submission.BatchID,
                submission.SubmissionName,
                submission.Order,
                submission.RowStamp,
                UserID = userId,
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    // -----------------------------------------------------------------------
    // Animals
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Animal>> GetAnimalsByBatchAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        // Legacy source: clsAnimal.vb::GetAnimalsForBatch → SP "GetBatchAnimal" @ID = batchId.
        // QueryAsync<Animal> works here because Animal uses set (not init) properties,
        // allowing Dapper's DefaultTypeMap to set every column via its IL-emitted callvirt.
        var rows = await conn.QueryAsync<Animal>(
            "GetBatchAnimal",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        // GetBatchAnimal joins Batch -> BatchSubmission -> Animal with no DISTINCT/GROUP BY, so an
        // animal referenced by more than one BatchSubmission in the same batch (e.g. a Pre-Cassetted
        // submission reusing an existing animal's ID — see AddSubmissionModel.OnPostAsync) comes
        // back once per BatchSubmission row, not once per animal — collapse to one row per animal
        // here, same as GetBlockAnimalsByBatchAsync already does for its own block-grained result set.
        var result = rows.GroupBy(a => a.ID).Select(g => g.First()).ToList();
        ApplyHistoRefAndPMDateSetFlags(result);
        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Animal>> GetBlockAnimalsByBatchAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        // Legacy source: clsBatch.vb::GetBatchBlockDetails → SP "GetBatchBlocksByID" @ID = batchId.
        // BATCH_BLOCK_ANIMAL = 11 in the assembled DataSet = result-set index 5 within GetBatchBlocksByID
        // (indices 0–4 are: BATCH_BLOCK_TABLE, BATCH_BLOCK_TISSUES, BATCH_BLOCK_HISTOLOGY,
        // BATCH_BLOCK_ANTIBODIES, BATCH_BLOCK_STAIN). This is the exact data source used by
        // BatchBlockSummary.aspx via clsBatchSummary.CreateSenderHistoRefData, which reads
        // SenderRef and HistologyRef from dsDataSet.Tables(BATCH_BLOCK_ANIMAL).
        using var multi = await conn.QueryMultipleAsync(
            "GetBatchBlocksByID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        const int blockAnimalResultSetIndex = 5;
        for (var i = 0; i < blockAnimalResultSetIndex; i++)
            await multi.ReadAsync<dynamic>();
        var rows = await multi.ReadAsync<Animal>();
        // BATCH_BLOCK_ANIMAL is block-grained — an animal with several blocks is returned once per
        // block. Every caller treats this as an animal list (and several key dictionaries by ID),
        // so collapse to one row per animal here.
        var result = rows.GroupBy(a => a.ID).Select(g => g.First()).ToList();
        ApplyHistoRefAndPMDateSetFlags(result);
        return result;
    }

    /// <summary>
    /// Neither GetBatchAnimal nor GetBatchBlockAnimal return HistoRefSet/PMDateSet as real columns
    /// (the Animal table has no such columns) — legacy computes both in-memory from whether
    /// HistologyRef/PMDate are non-blank (Common.vb, applied identically to BATCH_ANIMAL_TABLE and
    /// BATCH_BLOCK_ANIMAL). Without this, both flags silently stay false on every fresh load,
    /// breaking any "already set, now locked" check derived from them.
    /// </summary>
    private static void ApplyHistoRefAndPMDateSetFlags(IEnumerable<Animal> animals)
    {
        foreach (var a in animals)
        {
            a.HistoRefSet = !string.IsNullOrWhiteSpace(a.HistologyRef);
            a.PMDateSet = !string.IsNullOrWhiteSpace(a.PMDate);
        }
    }

    /// <inheritdoc/>
    public async Task<int> AddAnimalAsync(Animal animal, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        // Legacy SP signature (clsAnimal.vb::AddAnimal / UpdateAnimalRow's Added-row branch):
        // SenderRef, HistologyRef, NextBlockRef, PMDate, OnHold, @NewID (output) — no
        // BatchSubmissionID/PMDateSet/IsPGNumber/UserID parameters exist on this SP for inserts.
        parameters.Add("SenderRef", animal.SenderRef);
        parameters.Add("HistologyRef", animal.HistologyRef, dbType: System.Data.DbType.String);
        parameters.Add("NextBlockRef", animal.NextBlockRef);
        parameters.Add("PMDate", (object?)animal.PMDate ?? DBNull.Value, dbType: System.Data.DbType.String);
        parameters.Add("OnHold", animal.OnHold);
        parameters.Add("NewID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync("AddAnimal", parameters,
            commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int?>("NewID") ?? 0;
    }

    /// <inheritdoc/>
    public async Task UpdateAnimalAsync(Animal animal, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        // EditAnimal declares only ID/SenderRef/HistologyRef/NextBlockRef/OnHold/PMDate/RowStamp/
        // UserID — passing PMDateSet/IsPGNumber (not SP parameters) throws "too many arguments
        // specified", which SubmissionService.UpdateAnimalAsync swallows and reports as false, so
        // the update silently no-ops (Sender/Histology Ref/PM Date/OnHold never actually saved).
        // @PMDate is a real `datetime` parameter — Animal.PMDate is the legacy dd/MM/yyyy display
        // string, so it must be parsed to an actual DateTime rather than handed to Dapper as a raw
        // nvarchar, or SQL Server throws "Error converting data type nvarchar to datetime" (always
        // for an empty string), which fails the whole UPDATE — dropping every other field too.
        var isoPmDate = DateFormatHelpers.ToIsoDate(animal.PMDate);
        object pmDateParam = isoPmDate is not null && DateTime.TryParse(isoPmDate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedPmDate)
            ? parsedPmDate
            : DBNull.Value;

        await conn.ExecuteAsync(
            "EditAnimal",
            new
            {
                animal.ID,
                animal.SenderRef,
                animal.NextBlockRef,
                HistologyRef = (object?)animal.HistologyRef ?? DBNull.Value,
                animal.OnHold,
                PMDate = pmDateParam,
                animal.RowStamp,
                UserID = userId,
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task DeleteAnimalAsync(int animalId, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        // DeleteAnimal only declares @ID — passing @UserID as well makes SQL Server reject every
        // call with "too many arguments specified", which SubmissionService's catch-all silently
        // turned into a false "may still have blocks or tissues" error for every delete attempt.
        await conn.ExecuteAsync(
            "DeleteAnimal",
            new { ID = animalId },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task UpdateAnimalSenderRefAsync(string senderRef, string newSenderRef, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add("SenderRef", senderRef);
        parameters.Add("NewSenderRef", newSenderRef);
        parameters.Add("UserID", userId);

        await conn.ExecuteAsync("EditAnimalSenderRef", parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        var returnValue = parameters.Get<int>("RETURN_VALUE");
        switch (returnValue)
        {
            case 1:
                throw new AnimalRefUpdateException("The Sample Sender Reference was not found.");
            case 3:
                throw new AnimalRefUpdateException("The New Sender Reference has already been used for another sample.");
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAnimalHistologyRefAsync(string senderRef, string? newHistologyRef, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add("SenderRef", senderRef);
        parameters.Add("NewHistologyRef", (object?)newHistologyRef ?? DBNull.Value, dbType: System.Data.DbType.String);
        parameters.Add("UserID", userId);

        await conn.ExecuteAsync("EditAnimalHistologyRef", parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        var returnValue = parameters.Get<int>("RETURN_VALUE");
        switch (returnValue)
        {
            case 1:
                throw new AnimalRefUpdateException("The Sample Sender Reference was not found.");
            case 3:
                throw new AnimalRefUpdateException("The new Histology Reference has already been used for another sample.");
        }
    }

    // -----------------------------------------------------------------------
    // Tissues
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Tissue>> GetTissuesBySubmissionAsync(int batchId, int submissionId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetBatchTissues",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows
            .Select(r => (IDictionary<string, object>)r)
            .Where(d => d.TryGetValue("BatchSubmissionID", out var bsid) && Convert.ToInt32(bsid) == submissionId)
            .Select(d => new Tissue
            {
                ID = d.TryGetValue("ID", out var id) ? Convert.ToInt32(id) : 0,
                OwnerID = submissionId,
                Owner = TissueOwner.Submission,
                // Trimmed: TissueCode is fixed-width in the database, so untrimmed values fail
                // exact-match comparisons against the tissue lookup's Code.
                TissueCode = d.TryGetValue("TissueCode", out var tc) ? Convert.ToString(tc)?.Trim() ?? "" : "",
                NoPieces = d.TryGetValue("NoPieces", out var np) ? Convert.ToInt16(np) : (short)0,
                Comment = d.TryGetValue("Comment", out var cm) && cm is not DBNull ? Convert.ToString(cm) : null,
                ArchiveLocation = d.TryGetValue("ArchiveLocation", out var al) && al is not DBNull ? Convert.ToString(al) : null,
                ArchivedDate = d.TryGetValue("ArchivedDate", out var ad) && ad is not DBNull && DateTime.TryParse(Convert.ToString(ad), out var adv) ? adv : null,
                ArchiveComment = d.TryGetValue("ArchiveComment", out var ac) && ac is not DBNull ? Convert.ToString(ac) : null,
                RowStamp = d.TryGetValue("RowStamp", out var rs) ? rs as byte[] : null,
            })
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Tissue>> GetBatchSubmissionTissuesAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        // BATCH_TISSUES_TABLE = result-set index 7 within GetBatchSubmissionDetailsByBatchID
        // (indices 0-5 = common tables, 6 = BATCH_SUBMISSION_TABLE, 7 = BATCH_TISSUES_TABLE).
        using var multi = await conn.QueryMultipleAsync(
            "GetBatchSubmissionDetailsByBatchID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        // BATCH_TISSUES_TABLE is at result-set index 1 within GetBatchSubmissionDetailsByBatchID
        // (0 = BATCH_SUBMISSION_TABLE, 1 = BATCH_TISSUES_TABLE, 2 = BATCH_ANIMAL_TABLE).
        const int tissueTableIndex = 1;
        for (var i = 0; i < tissueTableIndex; i++)
            await multi.ReadAsync<dynamic>();
        var rows = await multi.ReadAsync<dynamic>();
        return rows.Select(r =>
        {
            var d = (IDictionary<string, object>)r;
            // Try both common FK column names — SP may use either alias.
            var submId = d.TryGetValue("BatchSubmissionID", out var bsid) ? Convert.ToInt32(bsid) :
                         d.TryGetValue("SubmissionID", out var sid) ? Convert.ToInt32(sid) : 0;
            return new Tissue
            {
                ID = d.TryGetValue("ID", out var id) ? Convert.ToInt32(id) : 0,
                OwnerID = submId,
                Owner = TissueOwner.Submission,
                TissueCode = d.TryGetValue("TissueCode", out var tc) ? Convert.ToString(tc)?.Trim() ?? "" : "",
                NoPieces = d.TryGetValue("NoPieces", out var np) ? Convert.ToInt16(np) : (short)0,
                Comment = d.TryGetValue("Comment", out var c) && c is not DBNull ? Convert.ToString(c) : null,
                ArchiveLocation = d.TryGetValue("ArchiveLocation", out var al) && al is not DBNull ? Convert.ToString(al) : null,
                ArchivedDate = d.TryGetValue("ArchivedDate", out var ad) && ad is not DBNull ? Convert.ToDateTime(ad) : null,
                ArchiveComment = d.TryGetValue("ArchiveComment", out var ac) && ac is not DBNull ? Convert.ToString(ac) : null,
                RowStamp = d.TryGetValue("RowStamp", out var rs) && rs is not DBNull ? (byte[])rs : null,
            };
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<int> AddTissueAsync(Tissue tissue, int userId, CancellationToken ct = default)
    {
        var procName = tissue.Owner == TissueOwner.Submission ? "AddTissue" : "AddBlockTissue";
        var keyParam = tissue.Owner == TissueOwner.Submission ? "BatchSubmissionID" : "BlockID";

        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add(keyParam, tissue.OwnerID);
        parameters.Add("TissueCode", tissue.TissueCode);
        parameters.Add("NoPieces", tissue.NoPieces);
        parameters.Add("Comment", (object?)tissue.Comment ?? DBNull.Value, dbType: System.Data.DbType.String);
        // Legacy source: clsTissue.vb::UpdateTissueDetails — AddInsertParam list is
        // {keyField, TissueCode, NoPieces, Comment} only. No @UserID parameter on
        // AddTissue/AddBlockTissue (UserID is only an AddUpdateParam, used by Edit/Delete).

        await conn.ExecuteAsync(procName, parameters,
            commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int>("RETURN_VALUE");
    }

    /// <inheritdoc/>
    public async Task UpdateTissueAsync(Tissue tissue, int userId, CancellationToken ct = default)
    {
        var procName = tissue.Owner == TissueOwner.Submission ? "EditTissue" : "EditBlockTissue";
        var keyParam = tissue.Owner == TissueOwner.Submission ? "BatchSubmissionID" : "BlockID";

        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("ID", tissue.ID);
        parameters.Add(keyParam, tissue.OwnerID);
        parameters.Add("TissueCode", tissue.TissueCode);
        parameters.Add("NoPieces", tissue.NoPieces);
        parameters.Add("Comment", (object?)tissue.Comment ?? DBNull.Value, dbType: System.Data.DbType.String);
        parameters.Add("UserID", userId);
        parameters.Add("RowStamp", tissue.RowStamp);
        // Legacy source: clsTissue.vb::UpdateTissueDetails — Archive* AddUpdateParams are only
        // registered when sKeyField = "BatchSubmissionID" (EditBlockTissue has no Archive params).
        if (tissue.Owner == TissueOwner.Submission)
        {
            parameters.Add("ArchiveLocation", (object?)tissue.ArchiveLocation ?? DBNull.Value, dbType: System.Data.DbType.String);
            parameters.Add("ArchivedDate", (object?)tissue.ArchivedDate ?? DBNull.Value, dbType: System.Data.DbType.DateTime);
            parameters.Add("ArchiveComment", (object?)tissue.ArchiveComment ?? DBNull.Value, dbType: System.Data.DbType.String);
        }

        await conn.ExecuteAsync(procName, parameters, commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Tissue>> GetTissuesByBatchAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetBatchBlockTissues",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows
            .Select(r => (IDictionary<string, object>)r)
            .Select(d => new Tissue
            {
                ID = d.TryGetValue("ID", out var id) ? Convert.ToInt32(id) : 0,
                OwnerID = d.TryGetValue("BlockID", out var bid) ? Convert.ToInt32(bid) : 0,
                Owner = TissueOwner.Block,
                TissueCode = d.TryGetValue("TissueCode", out var tc) ? Convert.ToString(tc)?.Trim() ?? "" : "",
                NoPieces = d.TryGetValue("NoPieces", out var np) ? Convert.ToInt16(np) : (short)0,
                Comment = d.TryGetValue("Comment", out var cm) && cm is not DBNull ? Convert.ToString(cm) : null,
                RowStamp = d.TryGetValue("RowStamp", out var rs) ? rs as byte[] : null,
            })
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Tissue>> GetTissuesByBlockAsync(int batchId, int blockId, CancellationToken ct = default)
    {
        var all = await GetTissuesByBatchAsync(batchId, ct);
        return all.Where(t => t.OwnerID == blockId).ToList();
    }

    // -----------------------------------------------------------------------
    // Search (read-only)
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PmDateSearchResult>> GetByPmDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<PmDateSearchResult>(
            "GetSearchPMDates",
            new { FromDate = fromDate, ToDate = toDate },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SenderSearchResult>> GetAnimalsBySenderRefAsync(string senderRef, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<SenderSearchResult>(
            "GetAnimalsBySenderRef",
            new { SenderRef = senderRef },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SenderSearchResult>> GetAnimalBySenderAsync(string senderRef, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        // GetAnimalBySender performs an exact match on SenderRef — legacy source:
        // clsAnimal.vb::GetAnimalBySender, used by EditHistologyRef.aspx::getHistologyRef.
        // Use dynamic mapping with a case-insensitive dictionary to handle column-name
        // variations between SP versions (e.g. HistologyRef vs HistoRef).
        var rows = await conn.QueryAsync<dynamic>(
            "GetAnimalBySender",
            new { SenderRef = senderRef },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(r =>
        {
            var d = new Dictionary<string, object?>(
                ((IDictionary<string, object>)r).ToDictionary(p => p.Key, p => (object?)p.Value),
                StringComparer.OrdinalIgnoreCase);
            return new SenderSearchResult
            {
                ID = d.TryGetValue("ID", out var id) ? Convert.ToInt32(id) : 0,
                SenderRef = d.TryGetValue("SenderRef", out var sr) ? Convert.ToString(sr) : null,
                HistologyRef = d.TryGetValue("HistologyRef", out var hr) ? Convert.ToString(hr) :
                               d.TryGetValue("HistoRef", out var hr2) ? Convert.ToString(hr2) : null,
            };
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetExistingSenderRefsAsync(IEnumerable<string> senderRefs, CancellationToken ct = default)
    {
        var refs = senderRefs.ToList();
        if (refs.Count == 0) return [];

        // No legacy SP covers a bulk existence check — direct query, same pattern as
        // GetAllUnusedRefsAsync (HistologyRepository) where the SP shape doesn't fit.
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<string>(
            "SELECT SenderRef FROM Animal WHERE SenderRef IN @SenderRefs",
            new { SenderRefs = refs });
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TissueArchiveInfo>> GetTissueArchiveAsync(
        string? senderRef, string? histologyRef, string? archiveLocation, string? tissueCode, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<TissueArchiveInfo>(
            "GetAnimalTissuesArchiveInformation",
            new { SenderRef = senderRef, HistologyRef = histologyRef, ArchiveLocation = archiveLocation, TissueCode = tissueCode },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    // Table-ID -> stored procedure mapping mirroring the legacy Select Case in
    // clsAnimal.vb GetImportedData. Any ID not in this map (including "All") falls
    // back to GetAllImportedData, matching the legacy Case Else branch.
    private static readonly Dictionary<string, string> ImportedDataProcs = new()
    {
        ["1"] = "Get2001EXTSUB",
        ["2"] = "Get2001NEUROSUB",
        ["3"] = "Get2002EXTSUB",
        ["4"] = "Get2002EXTSUBNOCPU",
        ["5"] = "Get2002MOUSESUB",
        ["6"] = "Get2002NEUROSUB",
        ["7"] = "Get2003EXTSUB",
        ["8"] = "Get2003MOUSESUB",
        ["9"] = "Get2003NEUROSUB",
        ["10"] = "Get2004EXTSUB",
        ["11"] = "Get2004MOUSESUB",
        ["12"] = "Get2004NEUROSUB",
        ["13"] = "Get2005TBDIAGSUB",
        ["14"] = "GetICCSUBMI11999TO12JAN2001",
        ["15"] = "GetICCSUBMI1TISSUEONLYTO12THJAN2001",
    };

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ImportedDataRow>> GetImportedDataAsync(string? selectedTable, CancellationToken ct = default)
    {
        // Legacy: an empty selection is a no-op (Case "" -> Do nothing) — no query is run.
        if (string.IsNullOrEmpty(selectedTable))
            return [];

        var procName = ImportedDataProcs.TryGetValue(selectedTable, out var mapped)
            ? mapped
            : "GetAllImportedData";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<ImportedDataRow>(
            procName,
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalTissuesAsync(
        string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<AnimalTissueSearchResult>(
            "GetAnimalBatchTissues",
            new { SenderRef = senderRef, HistologyRef = histologyRef, TissueCode = tissueCode, ProjectDesc = projectDesc },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalBlockTissuesAsync(
        string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<AnimalTissueSearchResult>(
            "GetAnimalBlockTissues",
            new { SenderRef = senderRef, HistologyRef = histologyRef, TissueCode = tissueCode, ProjectDesc = projectDesc },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task DeleteTissueAsync(int tissueId, TissueOwner owner, int userId, CancellationToken ct = default)
    {
        var procName = owner == TissueOwner.Submission ? "DeleteTissue" : "DeleteBlockTissue";

        // Both SPs declare only @ID — passing @UserID throws "too many arguments specified",
        // which SubmissionService.DeleteTissueAsync swallows and reports as false. The caller
        // (BlockDetailsModel/SubmissionDetailsModel) doesn't check that result, so the delete
        // silently no-ops and the row remains. userId is accepted for interface/audit-call
        // symmetry with the other CRUD methods but isn't a parameter either SP supports.
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            procName,
            new { ID = tissueId },
            commandType: System.Data.CommandType.StoredProcedure);
    }
}
