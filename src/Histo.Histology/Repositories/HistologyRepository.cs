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
        // GetUnusedHistologyRefs takes NO parameters and returns the whole pool — passing
        // @HistologyType made SQL Server reject every call with "too many arguments specified".
        // The pool table has no type column either; a ref's type is its number range.
        // It also returns an unaliased "HistologyRef" column, which doesn't match the
        // HistologyRef.Ref property name, so Dapper's implicit binding leaves Ref blank —
        // map explicitly instead (same defensive-read pattern as BlockTestRepository's Map()).
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync(
            "GetUnusedHistologyRefs",
            commandType: System.Data.CommandType.StoredProcedure);

        return rows.Select(r =>
        {
            var row = (IDictionary<string, object>)r;
            return new HistologyRef
            {
                Ref = row.TryGetValue("HistologyRef", out var refVal) && refVal is not DBNull
                    ? Convert.ToString(refVal)?.Trim() ?? string.Empty
                    : string.Empty,
                HistologyType = histologyType,
                SenderRef = row.TryGetValue("SenderRef", out var senderVal) && senderVal is not DBNull
                    ? Convert.ToString(senderVal)
                    : null,
            };
        })
        .Where(r => HistologyRefTypeCode.FromExistingRef(r.Ref) == histologyType)
        .ToList();
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
    public async Task<string?> DrawNextRefAsync(int histologyType, CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        var p = new DynamicParameters();
        p.Add("Type", histologyType, dbType: System.Data.DbType.Int32);
        p.Add("NextHistologyRef", dbType: System.Data.DbType.String, size: 5, direction: System.Data.ParameterDirection.Output);
        p.Add("RowStamp", dbType: System.Data.DbType.Binary, size: 8, direction: System.Data.ParameterDirection.Output);

        await conn.ExecuteAsync("GetNextHistologyRef", p, commandType: System.Data.CommandType.StoredProcedure);

        var next = p.Get<string?>("NextHistologyRef");
        return string.IsNullOrWhiteSpace(next) ? null : next.Trim();
    }
}
