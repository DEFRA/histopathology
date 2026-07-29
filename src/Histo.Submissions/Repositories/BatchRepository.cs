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
    public async Task<IReadOnlyList<Batch>> GetReceivedAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<Batch>(
            "GetReceivedBatches",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Batch>> GetInProgressAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<Batch>(
            "GetInProgressBatches",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Batch>> GetNotReceivedAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<Batch>(
            "GetBatchesNotReceived",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Batch>> GetOnHoldAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<Batch>(
            "GetBatchesOnHold",
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
}
