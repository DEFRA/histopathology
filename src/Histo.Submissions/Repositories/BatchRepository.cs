using Dapper;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;

namespace Histo.Submissions.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IBatchRepository"/>.
/// </summary>
public sealed class BatchRepository : IBatchRepository
{
    private readonly IDbConnectionFactory _db;

    public BatchRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<Batch?> GetByIdAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Batch>(
            "GetCommonBatchTablesByID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetReceivedAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetReceivedBatches",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetInProgressAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetInProgressBatches",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetNotReceivedAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetBatchesNotReceived",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetAllBatchesAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetAllBatches",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetOnHoldAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetBatchesOnHold",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchListResult>> GetCompletedAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<BatchListResult>(
            "GetCompletedBatches",
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
        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add("Status",              batch.Status);
        parameters.Add("CustomerRef",         batch.CustomerRef);
        parameters.Add("Comments",            batch.Comments);
        parameters.Add("SubmittedByUserID",   batch.SubmittedByUserID);
        parameters.Add("UserAreaCode",        batch.UserAreaCode);
        parameters.Add("IsPreCassetted",      batch.IsPreCassetted);
        parameters.Add("BatchType",           batch.BatchType);
        parameters.Add("UserID",              userId);

        await conn.ExecuteAsync("AddBatch", parameters,
            commandType: System.Data.CommandType.StoredProcedure);
        return parameters.Get<int>("RETURN_VALUE");
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Batch batch, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "EditBatch",
            new
            {
                batch.ID,
                batch.Status,
                batch.CustomerRef,
                batch.Comments,
                batch.StatusComments,
                batch.ReceivedDate,
                batch.CompletedDate,
                batch.RowStamp,
                UserID = userId,
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task UpdateStatusAsync(int batchId, string newStatus, byte[] rowStamp, int userId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("RETURN_VALUE", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);
        parameters.Add("ID",       batchId);
        parameters.Add("Status",   newStatus);
        parameters.Add("RowStamp", rowStamp, dbType: System.Data.DbType.Binary);
        parameters.Add("UserID",   userId);

        await conn.ExecuteAsync("EditBatchStatus", parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        var returnValue = parameters.Get<int>("RETURN_VALUE");
        if (returnValue == 1)
            throw new BatchConcurrencyException();
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
        var rows = await conn.QueryAsync<BatchSearchResult>(
            "GetSearchBatchDetails",
            new
            {
                SubmittedBy       = (object?)criteria.SubmittedBy ?? DBNull.Value,
                ProjectContract   = (object?)criteria.ProjectContractCode ?? DBNull.Value,
                ContactName       = (object?)criteria.ContactName ?? DBNull.Value,
                Species           = (object?)criteria.Species ?? DBNull.Value,
                SubmittedArea     = (object?)criteria.SubmittedArea ?? DBNull.Value,
                SubmittedDateFrom = (object?)criteria.SubmittedDateFrom ?? DBNull.Value,
                SubmittedDateTo   = (object?)criteria.SubmittedDateTo ?? DBNull.Value,
                ReceivedDateFrom  = (object?)criteria.ReceivedDateFrom ?? DBNull.Value,
                ReceivedDateTo    = (object?)criteria.ReceivedDateTo ?? DBNull.Value,
                Fixation          = (object?)criteria.Fixation ?? DBNull.Value,
                HistologyRef      = (object?)criteria.HistologyRef ?? DBNull.Value,
                SenderRef         = (object?)criteria.SenderRef ?? DBNull.Value,
                Number            = (object?)criteria.SubmissionNumber ?? DBNull.Value,
                Status            = (object?)criteria.Status ?? DBNull.Value,
                EnteredBy         = (object?)criteria.EnteredBy ?? DBNull.Value,
                All               = 0,
            },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TestItemRow>> GetTestItemRowsAsync(string? projectDesc, int batchType, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<TestItemRow>(
            "GetTestRows",
            new { ProjectContractDesc = (object?)projectDesc ?? DBNull.Value, BatchType = batchType },
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
}
