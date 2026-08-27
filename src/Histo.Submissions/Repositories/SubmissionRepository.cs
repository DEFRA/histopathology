using Dapper;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;

namespace Histo.Submissions.Repositories;

/// <summary>
/// Dapper implementation of <see cref="ISubmissionRepository"/>.
/// Covers batch submissions (sample groups), animals, and tissues.
/// </summary>
public sealed class SubmissionRepository : ISubmissionRepository
{
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
        // when no animal is known yet (the "default empty submission" case) — passing DBNull
        // violates the NOT NULL constraint, so 0 is sent instead to match legacy behaviour.
        parameters.Add("ID",        0,                      dbType: System.Data.DbType.Int32);
        parameters.Add("BatchID",   submission.BatchID,     dbType: System.Data.DbType.Int32);
        parameters.Add("AnimalID",  0,                      dbType: System.Data.DbType.Int32);
        parameters.Add("Order",     submission.Order,       dbType: System.Data.DbType.Int32);
        parameters.Add("OldID",     dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
        parameters.Add("NewID",     dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync("AddBatchSubmission", parameters,
            commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int>("NewID");
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
        return rows.ToList();
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
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<int> AddAnimalAsync(Animal animal, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add("BatchSubmissionID", animal.BatchSubmissionID);
        parameters.Add("SenderRef", animal.SenderRef);
        parameters.Add("NextBlockRef", animal.NextBlockRef);
        parameters.Add("HistologyRef", animal.HistologyRef, dbType: System.Data.DbType.String);
        parameters.Add("OnHold", animal.OnHold);
        parameters.Add("PMDate", (object?)animal.PMDate ?? DBNull.Value);
        parameters.Add("PMDateSet", animal.PMDateSet);
        parameters.Add("IsPGNumber", animal.IsPGNumber);
        parameters.Add("UserID", userId);

        await conn.ExecuteAsync("AddAnimal", parameters,
            commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int>("RETURN_VALUE");
    }

    /// <inheritdoc/>
    public async Task UpdateAnimalAsync(Animal animal, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "EditAnimal",
            new
            {
                animal.ID,
                animal.SenderRef,
                animal.NextBlockRef,
                HistologyRef = (object?)animal.HistologyRef ?? DBNull.Value,
                animal.OnHold,
                PMDate = (object?)animal.PMDate ?? DBNull.Value,
                animal.PMDateSet,
                animal.IsPGNumber,
                animal.RowStamp,
                UserID = userId,
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task DeleteAnimalAsync(int animalId, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "DeleteAnimal",
            new { ID = animalId, UserID = userId },
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
        parameters.Add("NewHistologyRef", (object?)newHistologyRef ?? DBNull.Value);
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
    public async Task<IReadOnlyList<Tissue>> GetTissuesBySubmissionAsync(int submissionId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<Tissue>(
            "GetTissuesBySubmissionID",
            new { ID = submissionId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
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
                OwnerID = submId,
                Owner = TissueOwner.Submission,
                TissueCode = d.TryGetValue("TissueCode", out var tc) ? Convert.ToString(tc) ?? "" : "",
                NoPieces = d.TryGetValue("NoPieces", out var np) ? Convert.ToInt16(np) : (short)0,
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
        parameters.Add("Comment", (object?)tissue.Comment ?? DBNull.Value);
        parameters.Add("UserID", userId);

        await conn.ExecuteAsync(procName, parameters,
            commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int>("RETURN_VALUE");
    }

    /// <inheritdoc/>
    public async Task UpdateTissueAsync(Tissue tissue, int userId, CancellationToken ct = default)
    {
        var procName = tissue.Owner == TissueOwner.Submission ? "EditTissue" : "EditBlockTissue";

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            procName,
            new
            {
                tissue.ID,
                tissue.TissueCode,
                tissue.NoPieces,
                Comment = (object?)tissue.Comment ?? DBNull.Value,
                ArchiveLocation = (object?)tissue.ArchiveLocation ?? DBNull.Value,
                ArchivedDate = (object?)tissue.ArchivedDate ?? DBNull.Value,
                ArchiveComment = (object?)tissue.ArchiveComment ?? DBNull.Value,
                tissue.RowStamp,
                UserID = userId,
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Tissue>> GetTissuesByBlockAsync(int blockId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<Tissue>(
            "GetTissuesByBlockID",
            new { ID = blockId },
            commandType: System.Data.CommandType.StoredProcedure);

        // Owner is not a DB column — these are always block-owned tissues.
        return rows.Select(t => new Tissue
        {
            ID = t.ID,
            OwnerID = blockId,
            Owner = TissueOwner.Block,
            TissueCode = t.TissueCode,
            NoPieces = t.NoPieces,
            Comment = t.Comment,
            ArchiveLocation = t.ArchiveLocation,
            ArchivedDate = t.ArchivedDate,
            ArchiveComment = t.ArchiveComment,
            RowStamp = t.RowStamp,
        }).ToList();
    }

    // -----------------------------------------------------------------------
    // Search (read-only)
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PmDateSearchResult>> GetByPmDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
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

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            procName,
            new { ID = tissueId, UserID = userId },
            commandType: System.Data.CommandType.StoredProcedure);
    }
}
