using Dapper;
using Histo.Core.Domain;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;

namespace Histo.Submissions.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IBatchRepository"/>.
/// </summary>
public sealed class BatchRepository : IBatchRepository
{
    private const string ReturnValueParam = "RETURN_VALUE";
    private const string DateReceivedColumn = "DateReceived";
    private const string ReceivedByColumn = "ReceivedBy";

    private readonly IDbConnectionFactory _db;

    public BatchRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<Batch?> GetByIdAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(
            "GetCommonBatchTablesByID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);

        // Result set 0 = BATCH_TABLE (GetBatchDetails). Consume remaining sets to avoid connection errors.
        var batchRows = await multi.ReadAsync<dynamic>();
        while (!multi.IsConsumed) await multi.ReadAsync<dynamic>();

        var row = batchRows.FirstOrDefault();
        return row is null ? null : MapBatch(row);
    }

    /// <summary>Maps a dynamic row from <c>GetBatchDetails</c> to a <see cref="Batch"/> object.
    /// Handles column name mismatches (e.g. BatchStatus int→Status string, DateReceived→ReceivedDate)
    /// and varchar dates returned as "dd/MM/yyyy" strings.
    /// </summary>
    private static Batch MapBatch(dynamic row)
    {
        var d    = (IDictionary<string, object>)row;
        // GetBatchDetails has a duplicate PostFixationOther column; TryAdd takes first occurrence.
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in d) dict.TryAdd(kvp.Key, kvp.Value);

        static string? Str(Dictionary<string, object> d, string k)
            => d.TryGetValue(k, out var v) && v is not DBNull ? Convert.ToString(v) : null;

        static int Int(Dictionary<string, object> d, string k)
            => d.TryGetValue(k, out var v) && v is not DBNull && v != null ? Convert.ToInt32(v) : 0;

        static int? NullInt(Dictionary<string, object> d, string k)
            => d.TryGetValue(k, out var v) && v is not DBNull && v != null ? (int?)Convert.ToInt32(v) : null;

        static bool Bool(Dictionary<string, object> d, string k)
            => d.TryGetValue(k, out var v) && v is not DBNull && v != null && Convert.ToBoolean(v);

        static bool? NullBool(Dictionary<string, object> d, string k)
            => d.TryGetValue(k, out var v) && v is not DBNull && v != null ? (bool?)Convert.ToBoolean(v) : null;

        static DateTime? ParseDate(Dictionary<string, object> d, string k)
        {
            if (!d.TryGetValue(k, out var v) || v is null) return null;
            if (v is DateTime dt) return dt;
            var s = Convert.ToString(v);
            if (string.IsNullOrEmpty(s)) return null;
            return DateTime.TryParseExact(s, ["dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss"],
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed) ? parsed : null;
        }

        // BatchStatus is int (1-6) in DB; convert to string constant used by the app.
        var batchStatusInt = Int(dict, "BatchStatus");
        var submittedAreaStr = Str(dict, "SubmittedArea");

        return new Batch
        {
            ID                = Int(dict, "ID"),
            Status            = batchStatusInt > 0 ? batchStatusInt.ToString() : BatchStatus.Submitted,
            Comments          = Str(dict, "Comments"),
            StatusComments    = Str(dict, "StatusComments"),
            BatchDate         = ParseDate(dict, "BatchDate"),
            ReceivedDate      = ParseDate(dict, DateReceivedColumn),   // SP column: DateReceived
            CompletedDate     = ParseDate(dict, "DateCompleted"),  // SP column: DateCompleted
            SubmittedByUserID = Int(dict, "SubmittedBy"),          // populate old field for backward compat
            UserAreaCode      = int.TryParse(submittedAreaStr, out var ua) ? ua : 0,
            IsPreCassetted    = Bool(dict, "Cassetted"),           // SP column: Cassetted
            ByPassSort        = Bool(dict, "ByPassSort"),
            RowStamp          = d.TryGetValue("RowStamp", out var rs) ? rs as byte[] : null,
            BatchType         = Int(dict, "BatchType"),
            ProjectContractCode = Str(dict, "ProjectContractCode"),
            ContactName       = Str(dict, "ContactName"),
            Species           = Str(dict, "Species"),
            Fixation          = Str(dict, "Fixation"),
            CustomerReceivedDate = dict.TryGetValue("CustomerReceivedDate", out var crd) && crd is DateTime crdDt ? crdDt : ParseDate(dict, "CustomerReceivedDate"),
            SubmittedBy       = NullInt(dict, "SubmittedBy"),
            SubmittedArea     = submittedAreaStr,
            OtherSubmittedBy  = NullInt(dict, "OtherSubmittedBy"),
            OtherSubmittedArea = Str(dict, "OtherSubmittedArea"),
            SafeToHandle      = NullBool(dict, "SafeToHandle"),
            IsBlocked         = Bool(dict, "IsBlocked"),
            SampleSameProjects = Bool(dict, "SampleSameProjects"),
            AllTissuesAssigned = Bool(dict, "AllTissuesAssigned"),
            TimeReceived      = Str(dict, "TimeReceived"),
            ReceivedBy        = NullInt(dict, ReceivedByColumn),
            PostFixationOther = Str(dict, "PostFixationOther"),
        };
    }

    /// <summary>Converts a nullable string to nullable int for SP FK parameters (int NOT NULL columns).</summary>
    internal static object ToIntParam(string? value, int fallback = 0)
    {
        if (string.IsNullOrEmpty(value)) return fallback;
        return int.TryParse(value, out var i) ? i : fallback;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetReceivedAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetBatchesToBeBlocked",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetInProgressAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetBatchesWithStatus",
            new { BatchStatus = 6 },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetNotReceivedAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetBatchesWithStatus",
            new { BatchStatus = 1 },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetAllBatchesAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetBatchesWithStatus",
            new { BatchStatus = 0 },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetOnHoldAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetBatchesWithStatus",
            new { BatchStatus = 5 },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetCompletedAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetBatchesWithStatus",
            new { BatchStatus = 4 },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetForDispatchAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetBatchesForDispatch",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<int> AddAsync(Batch batch, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var p = BuildAddBatchParams(batch, userId);

        // Try the OUTPUT param approach first (matches the .PRC file definition).
        // If the live SP doesn't have @BatchID OUTPUT (some deployments return the ID via RETURN),
        // SQL Server raises error 8144 "too many arguments" — we then retry with RETURN_VALUE.
        // Error 8144 means the INSERT never ran, so the retry is safe (no duplicate rows).
        p.Add("BatchID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

        try
        {
            await conn.ExecuteAsync("AddBatch", p, commandType: System.Data.CommandType.StoredProcedure);
            var batchId = p.Get<int>("BatchID");
            if (batchId > 0) return batchId;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 8144)
        {
            // SP variant: returns new ID via RETURN statement instead of OUTPUT param
            var p2 = BuildAddBatchParams(batch, userId);
            p2.Add(ReturnValueParam, dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
            await conn.ExecuteAsync("AddBatch", p2, commandType: System.Data.CommandType.StoredProcedure);
            return p2.Get<int>(ReturnValueParam);
        }

        // INSERT ran but @BatchID = 0: INSERT failed (SP returned error code via RETURN).
        // No throw here — BatchService.AddAsync logs and returns 0 to caller.
        return 0;
    }

    /// <summary>Builds the parameter set for the <c>AddBatch</c> SP — shared with <see cref="Histo.Submissions.Repositories.SubmissionRepository.CreateBatchWithCopiedSamplesAsync"/>, which needs to call the same SP within its own transaction.</summary>
    internal static DynamicParameters BuildAddBatchParams(Batch batch, int userId)
    {
        var p = new DynamicParameters();
        p.Add("ProjectContractCode", ToIntParam(batch.ProjectContractCode));
        p.Add("ContactName",         ToIntParam(batch.ContactName));
        p.Add("Species",             (object?)(batch.Species ?? ""));
        p.Add("BatchDate",           batch.BatchDate ?? (object)DateTime.Today);
        p.Add("BatchType",           batch.BatchType);
        p.Add("SubmittedBy",         batch.SubmittedByUserID > 0 ? batch.SubmittedByUserID : userId);
        p.Add("SafeToHandle",        batch.SafeToHandle ?? false);
        p.Add("BatchStatus",         1);
        p.Add("OtherSubmittedBy",    batch.OtherSubmittedBy ?? 0);
        p.Add("OtherSubmittedArea",  batch.OtherSubmittedArea ?? "");
        p.Add("Fixation",            batch.Fixation);
        p.Add("Comments",            batch.Comments);
        p.Add("Cassetted",           batch.IsPreCassetted);
        p.Add("IsBlocked",           batch.IsBlocked);
        p.Add("CustomerReceivedDate",batch.CustomerReceivedDate);
        p.Add("SubmittedArea",       batch.UserAreaCode > 0 ? batch.UserAreaCode.ToString() : null);
        p.Add("SampleSameProjects",  batch.SampleSameProjects);
        p.Add("ByPassSort",          batch.ByPassSort);
        return p;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Batch batch, int userId, CancellationToken ct = default)
    {
        if (!int.TryParse(batch.Status, out var batchStatusInt)) batchStatusInt = 1;
        var submittedArea = batch.UserAreaCode > 0
            ? batch.UserAreaCode.ToString()
            : batch.SubmittedArea;

        using var conn = _db.CreateConnection();
        var p = new DynamicParameters();
        // Map to EditBatch SP parameter names.
        p.Add("ID",                  batch.ID);
        p.Add("ProjectContractCode", ToIntParam(batch.ProjectContractCode));
        p.Add("ContactName",         ToIntParam(batch.ContactName));
        p.Add("Species",             (object?)(batch.Species ?? ""));
        p.Add("BatchDate",           batch.BatchDate);
        p.Add("BatchType",           batch.BatchType);
        p.Add("SubmittedBy",         batch.SubmittedByUserID > 0 ? batch.SubmittedByUserID : (batch.SubmittedBy ?? 0));
        p.Add("SafeToHandle",        batch.SafeToHandle ?? false);
        p.Add("BatchStatus",         batchStatusInt);
        p.Add(DateReceivedColumn,    batch.ReceivedDate);
        p.Add("TimeReceived",        batch.TimeReceived);
        p.Add(ReceivedByColumn,      batch.ReceivedBy);
        p.Add("OtherSubmittedBy",    batch.OtherSubmittedBy ?? 0);
        p.Add("OtherSubmittedArea",  batch.OtherSubmittedArea ?? "");
        p.Add("Cassetted",           batch.IsPreCassetted);
        p.Add("Fixation",            batch.Fixation);
        p.Add("Comments",            batch.Comments);
        p.Add("StatusComments",      batch.StatusComments);
        p.Add("PostFixationOther",   batch.PostFixationOther);
        p.Add("IsBlocked",           batch.IsBlocked);
        p.Add("CustomerReceivedDate",batch.CustomerReceivedDate);
        p.Add("DateCompleted",       batch.CompletedDate);
        p.Add("SubmittedArea",       submittedArea);
        p.Add("SampleSameProjects",  batch.SampleSameProjects);
        p.Add("AllTissuesAssigned",  batch.AllTissuesAssigned);
        p.Add("ByPassSort",          batch.ByPassSort);
        p.Add("UserID",              userId);
        p.Add("RowStamp",            batch.RowStamp, dbType: System.Data.DbType.Binary);

        await conn.ExecuteAsync("EditBatch", p, commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task SetCustomerReceivedDateAsync(int batchId, DateTime? date, byte[] rowStamp, int userId, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(batchId, ct);
        if (existing is null) return;
        // Construct new Batch with only CustomerReceivedDate changed; preserve everything else.
        var updated = new Batch
        {
            ID = existing.ID, Status = existing.Status, Comments = existing.Comments,
            StatusComments = existing.StatusComments, BatchDate = existing.BatchDate,
            ReceivedDate = existing.ReceivedDate, CompletedDate = existing.CompletedDate,
            SubmittedByUserID = existing.SubmittedByUserID, UserAreaCode = existing.UserAreaCode,
            IsPreCassetted = existing.IsPreCassetted, ByPassSort = existing.ByPassSort,
            RowStamp = rowStamp, BatchType = existing.BatchType,
            ProjectContractCode = existing.ProjectContractCode, ContactName = existing.ContactName,
            Species = existing.Species, Fixation = existing.Fixation,
            CustomerReceivedDate = date,
            SubmittedBy = existing.SubmittedBy, SubmittedArea = existing.SubmittedArea,
            OtherSubmittedBy = existing.OtherSubmittedBy, OtherSubmittedArea = existing.OtherSubmittedArea,
            SafeToHandle = existing.SafeToHandle, IsBlocked = existing.IsBlocked,
            SampleSameProjects = existing.SampleSameProjects, AllTissuesAssigned = existing.AllTissuesAssigned,
            TimeReceived = existing.TimeReceived, ReceivedBy = existing.ReceivedBy,
            PostFixationOther = existing.PostFixationOther,
        };
        await UpdateAsync(updated, userId, ct);
    }

    /// <inheritdoc/>
    public async Task SetByPassSortAsync(int batchId, bool byPassSort, int userId, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(batchId, ct);
        if (existing is null) return;
        var updated = new Batch
        {
            ID = existing.ID, Status = existing.Status, Comments = existing.Comments,
            StatusComments = existing.StatusComments, BatchDate = existing.BatchDate,
            ReceivedDate = existing.ReceivedDate, CompletedDate = existing.CompletedDate,
            SubmittedByUserID = existing.SubmittedByUserID, UserAreaCode = existing.UserAreaCode,
            IsPreCassetted = existing.IsPreCassetted, ByPassSort = byPassSort,
            RowStamp = existing.RowStamp, BatchType = existing.BatchType,
            ProjectContractCode = existing.ProjectContractCode, ContactName = existing.ContactName,
            Species = existing.Species, Fixation = existing.Fixation,
            CustomerReceivedDate = existing.CustomerReceivedDate,
            SubmittedBy = existing.SubmittedBy, SubmittedArea = existing.SubmittedArea,
            OtherSubmittedBy = existing.OtherSubmittedBy, OtherSubmittedArea = existing.OtherSubmittedArea,
            SafeToHandle = existing.SafeToHandle, IsBlocked = existing.IsBlocked,
            SampleSameProjects = existing.SampleSameProjects, AllTissuesAssigned = existing.AllTissuesAssigned,
            TimeReceived = existing.TimeReceived, ReceivedBy = existing.ReceivedBy,
            PostFixationOther = existing.PostFixationOther,
        };
        await UpdateAsync(updated, userId, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<int>> GetAnimalsWithUnassignedTissuesAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();

        // GetBatchBlockDetails returns one row per block, so it answers "which animals have a
        // block", never "which tissues are in a block" — the comparison has to be made against
        // BlockTissues, mapped back to the owning animal via the block.
        var blockToAnimal = (await conn.QueryAsync<dynamic>(
                "GetBatchBlockDetails",
                new { ID = batchId },
                commandType: System.Data.CommandType.StoredProcedure))
            .Select(r => (IDictionary<string, object>)r)
            .Select(d => (
                BlockId: d.TryGetValue("ID", out var bid) && bid is not DBNull ? Convert.ToInt32(bid) : 0,
                AnimalId: d.TryGetValue("AnimalID", out var aid) && aid is not DBNull ? Convert.ToInt32(aid) : 0))
            .Where(x => x.BlockId > 0 && x.AnimalId > 0)
            .GroupBy(x => x.BlockId)
            .ToDictionary(g => g.Key, g => g.First().AnimalId);

        var assignedByAnimal = new Dictionary<int, HashSet<string>>();
        foreach (var d in (await conn.QueryAsync<dynamic>(
                "GetBatchBlockTissues",
                new { ID = batchId },
                commandType: System.Data.CommandType.StoredProcedure))
            .Select(r => (IDictionary<string, object>)r))
        {
            var blockId = d.TryGetValue("BlockID", out var bid) && bid is not DBNull ? Convert.ToInt32(bid) : 0;
            if (!blockToAnimal.TryGetValue(blockId, out var animalId)) continue;

            var code = d.TryGetValue("TissueCode", out var tc) && tc is not DBNull ? Convert.ToString(tc)?.Trim() : null;
            if (string.IsNullOrEmpty(code)) continue;

            if (!assignedByAnimal.TryGetValue(animalId, out var codes))
                assignedByAnimal[animalId] = codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            codes.Add(code);
        }

        var unassigned = new HashSet<int>();
        foreach (var d in (await conn.QueryAsync<dynamic>(
                "GetBatchTissues",
                new { ID = batchId },
                commandType: System.Data.CommandType.StoredProcedure))
            .Select(r => (IDictionary<string, object>)r))
        {
            var animalId = d.TryGetValue("AnimalID", out var aid) && aid is not DBNull ? Convert.ToInt32(aid) : 0;
            if (animalId <= 0) continue;

            var code = d.TryGetValue("TissueCode", out var tc) && tc is not DBNull ? Convert.ToString(tc)?.Trim() : null;
            if (string.IsNullOrEmpty(code)) continue;

            if (!assignedByAnimal.TryGetValue(animalId, out var codes) || !codes.Contains(code))
                unassigned.Add(animalId);
        }

        return unassigned;
    }

    /// <inheritdoc/>
    public async Task RefreshAllTissuesAssignedAsync(int batchId, int userId, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(batchId, ct);
        if (existing is null) return;

        using var conn = _db.CreateConnection();
        var animals = (await conn.QueryAsync<dynamic>(
                "GetBatchAnimal",
                new { ID = batchId },
                commandType: System.Data.CommandType.StoredProcedure))
            .Select(r => (IDictionary<string, object>)r)
            .Select(d => d.TryGetValue("ID", out var id) ? Convert.ToInt32(id) : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var unassignedAnimals = await GetAnimalsWithUnassignedTissuesAsync(batchId, ct);

        var allTissuesAssigned = animals.Count > 0 && unassignedAnimals.Count == 0;
        if (allTissuesAssigned == existing.AllTissuesAssigned) return;

        var updated = new Batch
        {
            ID = existing.ID,
            Status = existing.Status,
            Comments = existing.Comments,
            StatusComments = existing.StatusComments,
            BatchDate = existing.BatchDate,
            ReceivedDate = existing.ReceivedDate,
            CompletedDate = existing.CompletedDate,
            SubmittedByUserID = existing.SubmittedByUserID,
            UserAreaCode = existing.UserAreaCode,
            IsPreCassetted = existing.IsPreCassetted,
            ByPassSort = existing.ByPassSort,
            RowStamp = existing.RowStamp,
            BatchType = existing.BatchType,
            ProjectContractCode = existing.ProjectContractCode,
            ContactName = existing.ContactName,
            Species = existing.Species,
            Fixation = existing.Fixation,
            CustomerReceivedDate = existing.CustomerReceivedDate,
            SubmittedBy = existing.SubmittedBy,
            SubmittedArea = existing.SubmittedArea,
            OtherSubmittedBy = existing.OtherSubmittedBy,
            OtherSubmittedArea = existing.OtherSubmittedArea,
            SafeToHandle = existing.SafeToHandle,
            IsBlocked = existing.IsBlocked,
            SampleSameProjects = existing.SampleSameProjects,
            AllTissuesAssigned = allTissuesAssigned,
            TimeReceived = existing.TimeReceived,
            ReceivedBy = existing.ReceivedBy,
            PostFixationOther = existing.PostFixationOther,
        };

        await UpdateAsync(updated, userId, ct);
    }

    /// <inheritdoc/>
    public async Task SetCompletedAsync(int batchId, DateTime completedDate, int userId, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(batchId, ct);
        if (existing is null) return;
        // Goes through EditBatch rather than EditBatchStatus, which would null out the
        // receipt columns (DateReceived/TimeReceived/StatusComments/PostFixationOther).
        var updated = new Batch
        {
            ID = existing.ID, Status = BatchStatus.Completed, Comments = existing.Comments,
            StatusComments = existing.StatusComments, BatchDate = existing.BatchDate,
            ReceivedDate = existing.ReceivedDate, CompletedDate = completedDate,
            SubmittedByUserID = existing.SubmittedByUserID, UserAreaCode = existing.UserAreaCode,
            IsPreCassetted = existing.IsPreCassetted, ByPassSort = existing.ByPassSort,
            RowStamp = existing.RowStamp, BatchType = existing.BatchType,
            ProjectContractCode = existing.ProjectContractCode, ContactName = existing.ContactName,
            Species = existing.Species, Fixation = existing.Fixation,
            CustomerReceivedDate = existing.CustomerReceivedDate,
            SubmittedBy = existing.SubmittedBy, SubmittedArea = existing.SubmittedArea,
            OtherSubmittedBy = existing.OtherSubmittedBy, OtherSubmittedArea = existing.OtherSubmittedArea,
            SafeToHandle = existing.SafeToHandle, IsBlocked = existing.IsBlocked,
            SampleSameProjects = existing.SampleSameProjects, AllTissuesAssigned = existing.AllTissuesAssigned,
            TimeReceived = existing.TimeReceived, ReceivedBy = existing.ReceivedBy,
            PostFixationOther = existing.PostFixationOther,
        };
        await UpdateAsync(updated, userId, ct);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateStatusAsync(int batchId, string newStatus, int userId, CancellationToken ct = default)
    {
        if (!int.TryParse(newStatus, out var batchStatusInt)) batchStatusInt = 1;

        // EditBatchStatus overwrites the receipt columns unconditionally, so the current values
        // must be read and passed straight back — otherwise any status change blanks out
        // DateReceived/TimeReceived/ReceivedBy/StatusComments/PostFixationOther.
        var existing = await GetByIdAsync(batchId, ct);

        // When marking as Received, auto-populate the receipt stamp (mirrors legacy ReceiveBatch.aspx).
        var isReceiving = newStatus == BatchStatus.Received;
        var dateReceived = isReceiving ? existing?.ReceivedDate ?? DateTime.Now : existing?.ReceivedDate;
        var receivedBy = isReceiving ? userId : existing?.ReceivedBy;
        int? timeReceived = int.TryParse(existing?.TimeReceived, out var tr) ? tr : null;

        using var conn = _db.CreateConnection();
        var p = new DynamicParameters();
        p.Add(ReturnValueParam, dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        p.Add("ID",              batchId);
        p.Add("BatchStatus",     batchStatusInt);
        p.Add(DateReceivedColumn, dateReceived);
        p.Add("TimeReceived",    timeReceived);
        p.Add(ReceivedByColumn,  receivedBy);
        p.Add("StatusComments",  existing?.StatusComments);
        p.Add("PostFixationOther", existing?.PostFixationOther);

        await conn.ExecuteAsync("EditBatchStatus", p, commandType: System.Data.CommandType.StoredProcedure);

        var returnValue = p.Get<int>(ReturnValueParam);
        // SP returns -1 when no rows were updated (concurrency conflict or batch not found).
        if (returnValue == -1) throw new BatchConcurrencyException();
        return returnValue == 0;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetCommentsAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<string>(
            "GetAllBatchComments",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    // -----------------------------------------------------------------------
    // Search (read-only)
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchSearchResult>> SearchAsync(BatchSearchCriteria criteria, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            "GetSearchBatchDetails",
            new
            {
                SubmittedBy = criteria.SubmittedBy,
                ProjectContract = criteria.ProjectContractCode,
                ContactName = criteria.ContactName,
                Species = criteria.Species,
                SubmittedArea = criteria.SubmittedArea,
                SubmittedDateFrom = criteria.SubmittedDateFrom,
                SubmittedDateTo = criteria.SubmittedDateTo,
                ReceivedDateFrom = criteria.ReceivedDateFrom,
                ReceivedDateTo = criteria.ReceivedDateTo,
                Fixation = criteria.Fixation,
                HistologyRef = criteria.HistologyRef,
                SenderRef = criteria.SenderRef,
                Number = criteria.SubmissionNumber,
                Status = criteria.Status,
                EnteredBy = criteria.EnteredBy,
                All = 0,
            },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.Select(MapSearchResult).ToList();
    }

    /// <summary>
    /// Maps a dynamic row from <c>GetSearchBatchDetails</c> to a <see cref="BatchSearchResult"/>.
    /// The SP returns the status column as <c>BatchStatus</c> (int) — not <c>Status</c> — matching
    /// <see cref="MapBatch"/>'s handling of the same column on <c>GetCommonBatchTablesByID</c>.
    /// Using naive <c>QueryAsync&lt;BatchSearchResult&gt;</c> left <c>Status</c> always null, since
    /// no column is literally named "Status", breaking every status-gated action button downstream.
    /// </summary>
    private static BatchSearchResult MapSearchResult(dynamic row)
    {
        var dict = new Dictionary<string, object>((IDictionary<string, object>)row, StringComparer.OrdinalIgnoreCase);

        static string? Str(Dictionary<string, object> d, string k)
            => d.TryGetValue(k, out var v) && v is not DBNull && v is not null ? Convert.ToString(v) : null;

        static int Int(Dictionary<string, object> d, string k)
            => d.TryGetValue(k, out var v) && v is not DBNull && v is not null ? Convert.ToInt32(v) : 0;

        static DateTime? ParseDate(Dictionary<string, object> d, string k)
        {
            if (!d.TryGetValue(k, out var v) || v is DBNull || v is null) return null;
            if (v is DateTime dt) return dt;
            var s = Convert.ToString(v);
            if (string.IsNullOrEmpty(s)) return null;
            return DateTime.TryParseExact(s, ["dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss"],
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed) ? parsed : null;
        }

        var batchStatusInt = Int(dict, "BatchStatus");

        return new BatchSearchResult
        {
            ID                    = Int(dict, "ID"),
            ProjectDescription    = Str(dict, "ProjectDescription"),
            ContactDescription    = Str(dict, "ContactDescription"),
            Species               = Str(dict, "Species"),
            BatchDate             = ParseDate(dict, "BatchDate"),
            DateReceived          = ParseDate(dict, DateReceivedColumn),
            DateCompleted         = ParseDate(dict, "DateCompleted"),
            CustomerReceivedDate  = ParseDate(dict, "CustomerReceivedDate"),
            Status                = batchStatusInt > 0 ? batchStatusInt.ToString() : Str(dict, "Status"),
            SubmittedBy           = Str(dict, "SubmittedBy"),
            BatchType             = Str(dict, "BatchType"),
            SafeToHandle          = Str(dict, "SafeToHandle"),
            ReceivedTime          = Str(dict, "ReceivedTime"),
            ReceivedBy            = Str(dict, ReceivedByColumn),
            OtherSubmittedBy      = Str(dict, "OtherSubmittedBy"),
            Comments              = Str(dict, "Comments"),
        };
    }


    /// <inheritdoc/>
    public async Task<IReadOnlyList<TestItemRow>> GetTestItemRowsAsync(string? projectDesc, int batchType, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<TestItemRow>(
            "GetTestRows",
            new { ProjectContractDesc = projectDesc, BatchType = batchType },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    // -----------------------------------------------------------------------
    // Fix Completed Dates (admin data-correction utility)
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<int>> GetBatchIdsLinkedToBlocksAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<int>(
            "GetBatchesLinkedToBlocks",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TestDispatchStatus>> GetHistologyDispatchStatusAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<TestDispatchStatus>(
            "GetHistologyDispatched",
            new { BatchID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TestDispatchStatus>> GetStainDispatchStatusAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<TestDispatchStatus>(
            "GetStainDispatched",
            new { BatchID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TestDispatchStatus>> GetAntibodiesDispatchStatusAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<TestDispatchStatus>(
            "GetAntibodiesDispatched",
            new { BatchID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task UpdateCompletedDateAsync(int batchId, DateTime completedDate, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "EditBatchCompletedDate",
            new { CompletedDate = completedDate, BatchID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    // -----------------------------------------------------------------------
    // Batch-level test type selections (Histology / Antibodies / Special Stains)
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<BatchTestSelections> GetBatchTestSelectionsAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(
            "GetCommonBatchTablesByID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);

        // Result set 0 = BATCH_TABLE (batch header) — read and discard; we only need 1-3.
        await multi.ReadAsync<dynamic>();

        var histology = (await multi.ReadAsync<BatchTestSelectionRow>()).ToList(); // index 1
        var antibodies = (await multi.ReadAsync<BatchTestSelectionRow>()).ToList(); // index 2
        var stains = (await multi.ReadAsync<BatchTestSelectionRow>()).ToList(); // index 3

        return new BatchTestSelections
        {
            Histology = histology,
            Antibodies = antibodies,
            Stains = stains,
        };
    }

    /// <inheritdoc/>
    public async Task<string?> GetSubmittedAsCodeAsync(int batchId, CancellationToken ct = default)
    {
        try
        {
            using var conn = _db.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(
                "GetCommonBatchTablesByID",
                new { ID = batchId },
                commandType: System.Data.CommandType.StoredProcedure);

            // BATCH_SUBMITTEDAS_TABLE is at result-set index 5 (clsBatch.vb constant).
            // Discard result-sets 0–4 (BATCH_TABLE, HISTOLOGY, ANTIBODIES, STAINS, unnamed).
            for (int i = 0; i < 5; i++)
                await multi.ReadAsync<dynamic>();

            var rows = (await multi.ReadAsync<dynamic>()).ToList();
            // Code comes from a fixed-width char column — untrimmed it fails every == comparison
            // callers make against the lookup list.
            return rows.Count > 0 ? ((string?)rows[0].Code?.ToString())?.Trim() : null;
        }
        catch
        {
            // SP may return fewer result sets for newly-created batches that have no submitted-as record.
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SaveSubmittedAsAsync(int batchId, string code, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "AddSubmittedAs",
            new { BatchID = batchId, Code = code, UserID = userId },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task SaveBatchTestSelectionsAsync(
        int batchId,
        IReadOnlyList<string> histologyCodes,
        IReadOnlyList<string> antibodyCodes,
        IReadOnlyList<string> stainCodes,
        int userId,
        CancellationToken ct = default)
    {
        var current = await GetBatchTestSelectionsAsync(batchId, ct);

        using var conn = _db.CreateConnection();
        await ApplyTestSelectionDeltaAsync(conn, batchId, userId,
            current.Histology, histologyCodes, "AddHistology", "DeleteHistology");
        await ApplyTestSelectionDeltaAsync(conn, batchId, userId,
            current.Antibodies, antibodyCodes, "AddAntibodies", "DeleteAntibodies");
        await ApplyTestSelectionDeltaAsync(conn, batchId, userId,
            current.Stains, stainCodes, "AddSpecialStain", "DeleteSpecialStain");
    }

    /// <summary>
    /// Applies an insert/delete delta for one test-type table.
    /// Codes present in <paramref name="newCodes"/> but absent from <paramref name="current"/> are inserted.
    /// Rows in <paramref name="current"/> whose code is absent from <paramref name="newCodes"/> are deleted.
    /// </summary>
    private static async Task ApplyTestSelectionDeltaAsync(
        System.Data.IDbConnection conn,
        int batchId,
        int userId,
        IReadOnlyList<BatchTestSelectionRow> current,
        IReadOnlyList<string> newCodes,
        string addSp,
        string deleteSp)
    {
        var currentSet = current.ToDictionary(r => r.Code, r => r.ID, StringComparer.OrdinalIgnoreCase);
        var newSet = newCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in current)
        {
            if (!newSet.Contains(row.Code))
            {
                await conn.ExecuteAsync(deleteSp,
                    new { ID = row.ID },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
        }

        //foreach (var code in newCodes)
        foreach (var code in newCodes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!currentSet.ContainsKey(code))
            {
                await conn.ExecuteAsync(addSp,
                    new { BatchID = batchId, Code = code, UserID = userId },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Post-fixation selections (Receive Submission workflow)
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetPostFixationCodesAsync(int batchId, CancellationToken ct = default)
    {
        var rows = await GetPostFixationRowsAsync(batchId, ct);
        return rows.Select(r => r.Code).ToList();
    }

    /// <inheritdoc/>
    public async Task SavePostFixationCodesAsync(int batchId, IReadOnlyList<string> codes, int userId, CancellationToken ct = default)
    {
        var current = await GetPostFixationRowsAsync(batchId, ct);

        using var conn = _db.CreateConnection();
        await ApplyTestSelectionDeltaAsync(conn, batchId, userId,
            current, codes, "AddPostFixation", "DeletePostFixation");
    }

    private async Task<List<BatchTestSelectionRow>> GetPostFixationRowsAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(
            "GetCommonBatchTablesByID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);

        // Discard result-sets 0–3 (BATCH_TABLE, HISTOLOGY, ANTIBODIES, STAIN);
        // BATCH_POSTFIXATION_TABLE is result-set index 4.
        for (int i = 0; i < 4; i++)
            await multi.ReadAsync<dynamic>();

        var rows = await multi.ReadAsync<BatchTestSelectionRow>();
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task CompleteBlockAssignmentAsync(int batchId, bool allTissuesAssigned, int userId, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(batchId, ct);
        if (existing is null) return;

        // Legacy: BatchBlocks.aspx.vb::btSubmit_Click and BatchSummary.aspx.vb::btSubmit_Click
        // both persist a blocked/in-progress state together with the all-tissues-assigned flag.
        // This is the real transition, not the generic EditBatchStatus path, which never updates
        // those fields and therefore cannot advance the batch to the correct workflow state.
        var updated = new Batch
        {
            ID = existing.ID,
            Status = BatchStatus.InProgress,
            Comments = existing.Comments,
            StatusComments = existing.StatusComments,
            BatchDate = existing.BatchDate,
            ReceivedDate = existing.ReceivedDate,
            CompletedDate = existing.CompletedDate,
            SubmittedByUserID = existing.SubmittedByUserID,
            UserAreaCode = existing.UserAreaCode,
            IsPreCassetted = existing.IsPreCassetted,
            ByPassSort = existing.ByPassSort,
            RowStamp = existing.RowStamp,
            BatchType = existing.BatchType,
            ProjectContractCode = existing.ProjectContractCode,
            ContactName = existing.ContactName,
            Species = existing.Species,
            Fixation = existing.Fixation,
            CustomerReceivedDate = existing.CustomerReceivedDate,
            SubmittedBy = existing.SubmittedBy,
            SubmittedArea = existing.SubmittedArea,
            OtherSubmittedBy = existing.OtherSubmittedBy,
            OtherSubmittedArea = existing.OtherSubmittedArea,
            SafeToHandle = existing.SafeToHandle,
            IsBlocked = true,
            SampleSameProjects = existing.SampleSameProjects,
            AllTissuesAssigned = allTissuesAssigned,
            TimeReceived = existing.TimeReceived,
            ReceivedBy = existing.ReceivedBy,
            PostFixationOther = existing.PostFixationOther,
        };

        await UpdateAsync(updated, userId, ct);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Confirmed live (2026-09-25) that legacy's real SPs — <c>GetTestHistologyCounts</c>/
    /// <c>GetTestAntibodiesCounts</c>/<c>GetTestStainsCounts</c> — DO exist in this database.
    /// The earlier "6 SPs confirmed not to exist" note was checking the WRONG names: legacy's
    /// <c>clsBatch.CountHistologysTestItems</c>/<c>CountAntibodesTestItems</c>/<c>CountStainTestItems</c>
    /// are thin VB wrappers whose actual <c>FillDataTable</c> call names are these 3 SPs, not
    /// their own method names — the same "trust the .aspx.vb call site, not the wrapper name"
    /// lesson already documented elsewhere in this repo. Reproduces
    /// <c>SearchTest.aspx.vb::btnCount_Click</c>/<c>ProcessHistologyData</c>/<c>ProcessAntibodiesData</c>/
    /// <c>ProcessStainData</c> exactly: one SP call per (test type × active premium charge), each
    /// row keyed by a dynamically-named result column equal to the premium charge's own
    /// Description (confirmed via <c>sp_helptext</c> — the SP itself builds and EXECs that SQL).
    /// </remarks>
    public async Task<IReadOnlyList<Models.TestPremiumChargeCount>> GetTestPremiumChargeCountsAsync(
        string? projectDesc, int batchType,
        IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes,
        DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var premiumCharges = await GetActivePremiumChargeDescriptionsAsync(conn);

        var results = new List<Models.TestPremiumChargeCount>();
        var filter = new PremiumChargeFilter(projectDesc, batchType, startDate, endDate);
        await AddCountsAsync(conn, results, "GetTestHistologyCounts", histologyCodes, premiumCharges, filter);
        await AddCountsAsync(conn, results, "GetTestAntibodiesCounts", antibodyCodes, premiumCharges, filter);
        await AddCountsAsync(conn, results, "GetTestStainsCounts", stainCodes, premiumCharges, filter);
        return results;
    }

    /// <inheritdoc/>
    /// <remarks>See <see cref="GetTestPremiumChargeCountsAsync"/> — same 3 real SPs, Batch variant
    /// (<c>GetTestHistologyBatch</c>/<c>GetTestAntibodiesBatch</c>/<c>GetTestStainsBatch</c>),
    /// reproducing <c>SearchTest.aspx.vb::bntBatch_Click</c>/<c>AttachEventHanders</c>.</remarks>
    public async Task<IReadOnlyList<Models.TestPremiumChargeBatchRef>> GetTestPremiumChargeBatchesAsync(
        string? projectDesc, int batchType,
        IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes,
        DateTime? startDate = null, DateTime? endDate = null,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var premiumCharges = await GetActivePremiumChargeDescriptionsAsync(conn);

        var results = new List<Models.TestPremiumChargeBatchRef>();
        var filter = new PremiumChargeFilter(projectDesc, batchType, startDate, endDate);
        await AddBatchesAsync(conn, results, "GetTestHistologyBatch", histologyCodes, premiumCharges, filter);
        await AddBatchesAsync(conn, results, "GetTestAntibodiesBatch", antibodyCodes, premiumCharges, filter);
        await AddBatchesAsync(conn, results, "GetTestStainsBatch", stainCodes, premiumCharges, filter);
        return results;
    }

    /// <summary>Legacy: <c>LookupData.GetLookupData(LOOKUP_PREMIUM_CHARGES)</c> — active-only <c>luPremiumCharges.Description</c> values, looped once per test type.</summary>
    private static async Task<IReadOnlyList<string>> GetActivePremiumChargeDescriptionsAsync(System.Data.IDbConnection conn)
    {
        // GetluPremiumCharges returns ID, Code, Description, IsActive (ID first) — QueryAsync<string>
        // would silently bind the FIRST column (ID) instead of Description, so read it as dynamic
        // and pick the Description column explicitly.
        var rows = await conn.QueryAsync<dynamic>(
            "GetluPremiumCharges", commandType: System.Data.CommandType.StoredProcedure);
        return rows
            .Select(r => (string?)((IDictionary<string, object>)r)["Description"])
            .Where(d => !string.IsNullOrEmpty(d))
            .Select(d => d!)
            .ToList();
    }

    /// <summary>
    /// Legacy: <c>GetSelectedTests</c> — selected codes joined by <c>','</c> with NO outer quotes,
    /// since the target SP interpolates it directly into <c>bs.Code in ('...')</c>.
    /// </summary>
    private static string JoinTestCodes(IReadOnlyList<string> codes) => string.Join("','", codes);

    /// <summary>Groups the shared filter parameters for <see cref="AddCountsAsync"/>/<see cref="AddBatchesAsync"/> so both stay under the 7-parameter limit.</summary>
    private sealed record PremiumChargeFilter(string? ProjectDescription, int BatchType, DateTime? StartDate, DateTime? EndDate);

    private static async Task AddCountsAsync(
        System.Data.IDbConnection conn, List<Models.TestPremiumChargeCount> results,
        string procName, IReadOnlyList<string> codes, IReadOnlyList<string> premiumCharges,
        PremiumChargeFilter filter)
    {
        if (codes.Count == 0) return;
        var tests = JoinTestCodes(codes);

        foreach (var testCode in premiumCharges)
        {
            var rows = await conn.QueryAsync<dynamic>(procName, new
            {
                ProjectContractCode = filter.ProjectDescription ?? "",
                SubmittedDateFrom = filter.StartDate,
                SubmittedDateTo = filter.EndDate,
                BatchType = filter.BatchType != 0,
                Tests = tests,
                TestCode = testCode,
            }, commandType: System.Data.CommandType.StoredProcedure);

            foreach (var row in rows)
            {
                // The SP names its own count column dynamically after @TestCode (e.g. "TC 1401") —
                // read by that exact key rather than a fixed property name.
                var d = (IDictionary<string, object>)row;
                var count = d.TryGetValue(testCode, out var c) && c is not DBNull ? Convert.ToInt32(c) : 0;
                var project = d.TryGetValue("Description", out var p) && p is not DBNull ? Convert.ToString(p) : null;
                results.Add(new Models.TestPremiumChargeCount
                {
                    ProjectDescription = project,
                    PremiumCode = testCode,
                    Count = count,
                });
            }
        }
    }

    private static async Task AddBatchesAsync(
        System.Data.IDbConnection conn, List<Models.TestPremiumChargeBatchRef> results,
        string procName, IReadOnlyList<string> codes, IReadOnlyList<string> premiumCharges,
        PremiumChargeFilter filter)
    {
        if (codes.Count == 0) return;
        var tests = JoinTestCodes(codes);

        foreach (var testCode in premiumCharges)
        {
            var batchIds = await conn.QueryAsync<int>(procName, new
            {
                ProjectContractCode = filter.ProjectDescription ?? "",
                SubmittedDateFrom = filter.StartDate,
                SubmittedDateTo = filter.EndDate,
                BatchType = filter.BatchType != 0,
                Tests = tests,
                TestCode = testCode,
            }, commandType: System.Data.CommandType.StoredProcedure);

            foreach (var id in batchIds)
                results.Add(new Models.TestPremiumChargeBatchRef { PremiumCode = testCode, BatchID = id });
        }
    }
}

