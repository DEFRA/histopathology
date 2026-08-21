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
        parameters.Add("Status", batch.Status);
        parameters.Add("CustomerRef", batch.CustomerRef);
        parameters.Add("Comments", batch.Comments);
        parameters.Add("SubmittedByUserID", batch.SubmittedByUserID);
        parameters.Add("UserAreaCode", batch.UserAreaCode);
        parameters.Add("IsPreCassetted", batch.IsPreCassetted);
        parameters.Add("BatchType", batch.BatchType);
        parameters.Add("UserID", userId);

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
    public async Task SetCustomerReceivedDateAsync(int batchId, DateTime? date, byte[] rowStamp, int userId, CancellationToken ct = default)
    {
        // Load existing batch to preserve all current field values — only CustomerReceivedDate changes.
        var existing = await GetByIdAsync(batchId, ct);
        if (existing is null) return;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "EditBatch",
            new
            {
                existing.ID,
                existing.Status,
                existing.CustomerRef,
                existing.Comments,
                existing.StatusComments,
                existing.ReceivedDate,
                existing.CompletedDate,
                CustomerReceivedDate = date,
                RowStamp = rowStamp,
                UserID = userId,
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task SetByPassSortAsync(int batchId, bool byPassSort, int userId, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(batchId, ct);
        if (existing is null) return;
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "EditBatch",
            new
            {
                existing.ID,
                existing.Status,
                existing.CustomerRef,
                existing.Comments,
                existing.StatusComments,
                existing.ReceivedDate,
                existing.CompletedDate,
                ByPassSort = byPassSort,
                RowStamp = existing.RowStamp,
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
        parameters.Add("ID", batchId);
        parameters.Add("Status", newStatus);
        parameters.Add("RowStamp", rowStamp, dbType: System.Data.DbType.Binary);
        parameters.Add("UserID", userId);

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
                SubmittedBy = (object?)criteria.SubmittedBy ?? DBNull.Value,
                ProjectContract = (object?)criteria.ProjectContractCode ?? DBNull.Value,
                ContactName = (object?)criteria.ContactName ?? DBNull.Value,
                Species = (object?)criteria.Species ?? DBNull.Value,
                SubmittedArea = (object?)criteria.SubmittedArea ?? DBNull.Value,
                SubmittedDateFrom = (object?)criteria.SubmittedDateFrom ?? DBNull.Value,
                SubmittedDateTo = (object?)criteria.SubmittedDateTo ?? DBNull.Value,
                ReceivedDateFrom = (object?)criteria.ReceivedDateFrom ?? DBNull.Value,
                ReceivedDateTo = (object?)criteria.ReceivedDateTo ?? DBNull.Value,
                Fixation = (object?)criteria.Fixation ?? DBNull.Value,
                HistologyRef = (object?)criteria.HistologyRef ?? DBNull.Value,
                SenderRef = (object?)criteria.SenderRef ?? DBNull.Value,
                Number = (object?)criteria.SubmissionNumber ?? DBNull.Value,
                Status = (object?)criteria.Status ?? DBNull.Value,
                EnteredBy = (object?)criteria.EnteredBy ?? DBNull.Value,
                All = 0,
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
            return rows.Count > 0 ? rows[0].Code?.ToString() : null;
        }
        catch
        {
            // SP may return fewer result sets for newly-created batches that have no submitted-as record.
            return null;
        }
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
}
