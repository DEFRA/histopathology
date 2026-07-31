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
        var rows = await conn.QueryAsync<BatchSubmission>(
            "GetBatchSubmissionDetailsByBatchID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<int> AddSubmissionAsync(BatchSubmission submission, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add("BatchID",        submission.BatchID);
        parameters.Add("SubmissionName", submission.SubmissionName);
        parameters.Add("Order",          submission.Order);
        parameters.Add("UserID",         userId);

        await conn.ExecuteAsync("AddBatchSubmission", parameters,
            commandType: System.Data.CommandType.StoredProcedure);
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
        var rows = await conn.QueryAsync<Animal>(
            "GetAnimalsByBatchID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<int> AddAnimalAsync(Animal animal, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add("BatchSubmissionID", animal.BatchSubmissionID);
        parameters.Add("SenderRef",         animal.SenderRef);
        parameters.Add("NextBlockRef",      animal.NextBlockRef);
        parameters.Add("HistologyRef",      (object?)animal.HistologyRef ?? DBNull.Value);
        parameters.Add("OnHold",            animal.OnHold);
        parameters.Add("PMDate",            (object?)animal.PMDate ?? DBNull.Value);
        parameters.Add("PMDateSet",         animal.PMDateSet);
        parameters.Add("IsPGNumber",        animal.IsPGNumber);
        parameters.Add("UserID",            userId);

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
                HistologyRef  = (object?)animal.HistologyRef ?? DBNull.Value,
                animal.OnHold,
                PMDate        = (object?)animal.PMDate ?? DBNull.Value,
                animal.PMDateSet,
                animal.IsPGNumber,
                animal.RowStamp,
                UserID        = userId,
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
    public async Task<int> AddTissueAsync(Tissue tissue, int userId, CancellationToken ct = default)
    {
        var procName = tissue.Owner == TissueOwner.Submission ? "AddTissue" : "AddBlockTissue";
        var keyParam = tissue.Owner == TissueOwner.Submission ? "BatchSubmissionID" : "BlockID";

        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add(keyParam,       tissue.OwnerID);
        parameters.Add("TissueCode",   tissue.TissueCode);
        parameters.Add("NoPieces",     tissue.NoPieces);
        parameters.Add("Comment",      (object?)tissue.Comment ?? DBNull.Value);
        parameters.Add("UserID",       userId);

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
                Comment        = (object?)tissue.Comment       ?? DBNull.Value,
                ArchiveLocation= (object?)tissue.ArchiveLocation ?? DBNull.Value,
                ArchivedDate   = (object?)tissue.ArchivedDate   ?? DBNull.Value,
                ArchiveComment = (object?)tissue.ArchiveComment ?? DBNull.Value,
                tissue.RowStamp,
                UserID         = userId,
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
