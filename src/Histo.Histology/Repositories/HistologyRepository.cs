using Dapper;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Infrastructure;

namespace Histo.Histology.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IHistologyRepository"/>.
/// </summary>
public sealed class HistologyRepository : IHistologyRepository
{
    private readonly IDbConnectionFactory _db;

    public HistologyRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HistologyRef>> GetUnusedRefsAsync(int histologyType, CancellationToken ct = default)
    {
        // GetUnusedHistologyRefs returns an unaliased "HistologyRef" column, which doesn't match
        // the HistologyRef.Ref property name, so Dapper's implicit binding leaves Ref blank —
        // map explicitly instead (same defensive-read pattern as BlockTestRepository's Map()).
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync(
            "GetUnusedHistologyRefs",
            new { HistologyType = histologyType },
            commandType: System.Data.CommandType.StoredProcedure);

        return rows.Select(r =>
        {
            var row = (IDictionary<string, object>)r;
            return new HistologyRef
            {
                Ref = row.TryGetValue("HistologyRef", out var refVal) && refVal is not DBNull
                    ? Convert.ToString(refVal) ?? string.Empty
                    : string.Empty,
                HistologyType = histologyType,
                SenderRef = row.TryGetValue("SenderRef", out var senderVal) && senderVal is not DBNull
                    ? Convert.ToString(senderVal)
                    : null,
            };
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HistologyRef>> GetUsedRefsByBatchAsync(int batchId, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<HistologyRef>(
            "GetUsedHistologyRefsByBatchID",
            new { ID = batchId },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HistologyRef>> GetUnusedBookedRefsAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<HistologyRef>(
            "GetUnUsedBookedHistologyRefs",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HistologyRef>> GetAllUnusedRefsAsync(CancellationToken ct = default)
    {
        // GetUnusedHistologyRefs only returns a single unaliased "HistologyRef" column (doesn't
        // match the HistologyRef.Ref property name) and never returns SenderRef, even though the
        // backing UnUsedHistologyRefs table has it. Queried directly here instead so both columns
        // populate correctly for the search grid.
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<HistologyRef>(
            "SELECT HistologyRef AS Ref, SenderRef FROM UnUsedHistologyRefs WHERE Used = 0 ORDER BY HistologyRef");
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HistologyRefCounter>> GetCountersAsync(CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<HistologyRefCounter>(
            "GetHistologyRefs",
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    /// <inheritdoc/>
    public async Task UpdateCounterAsync(int histologyType, string newNextHistologyRef, byte[]? rowStamp, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "EditHistologyRef",
            new { Type = histologyType, NextHistologyRef = newNextHistologyRef, RowStamp = rowStamp },
            commandType: System.Data.CommandType.StoredProcedure);
    }

    /// <inheritdoc/>
    public async Task<string?> ClaimNextCounterAsync(int histologyType, int upperBoundExclusive, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>(
            @"UPDATE HistologyRef
              SET NextHistologyRef = CAST(CAST(NextHistologyRef AS INT) + 1 AS VARCHAR(10))
              OUTPUT DELETED.NextHistologyRef
              WHERE Type = @Type AND CAST(NextHistologyRef AS INT) < @UpperBound",
            new { Type = histologyType, UpperBound = upperBoundExclusive });
    }

    /// <inheritdoc/>
    public async Task<string?> ClaimUnusedRefAsync(int histologyType, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        // READPAST skips rows another concurrent claim already has locked (rather than blocking
        // on/re-reading them), so two simultaneous callers each land on a different unused row
        // instead of both selecting the same one before either UPDATE commits.
        return await conn.ExecuteScalarAsync<string?>(
            @"UPDATE UnUsedHistologyRefs
              SET Used = 1
              OUTPUT DELETED.HistologyRef
              WHERE HistologyType = @HistologyType AND Used = 0
                AND HistologyRef = (
                    SELECT TOP (1) HistologyRef
                    FROM UnUsedHistologyRefs WITH (UPDLOCK, ROWLOCK, READPAST)
                    WHERE HistologyType = @HistologyType AND Used = 0
                      AND HistologyRef IS NOT NULL AND HistologyRef <> ''
                    ORDER BY HistologyRef)",
            new { HistologyType = histologyType });
    }
}
