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

        // The SP's own "already used" check builds the year with convert(varchar(2), datepart(yy, ...)),
        // which overflows to "*" for a 4-digit year and so never matches a real "26/NNNNN" ref — it
        // cannot be relied on. A counter sitting at or below a number already issued would otherwise
        // hand that number out again, so skip any number a sample already holds.
        for (var attempt = 0; attempt < MaxDrawAttempts; attempt++)
        {
            var p = new DynamicParameters();
            p.Add("Type", histologyType, dbType: System.Data.DbType.Int32);
            p.Add("NextHistologyRef", dbType: System.Data.DbType.String, size: 5, direction: System.Data.ParameterDirection.Output);
            p.Add("RowStamp", dbType: System.Data.DbType.Binary, size: 8, direction: System.Data.ParameterDirection.Output);

            await conn.ExecuteAsync("GetNextHistologyRef", p, commandType: System.Data.CommandType.StoredProcedure);

            var next = p.Get<string?>("NextHistologyRef");
            if (string.IsNullOrWhiteSpace(next)) return null;

            var drawn = next.Trim();
            var taken = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Animal WHERE RTRIM(HistologyRef) = @Ref",
                new { Ref = Histo.Core.Domain.HistologyRefHelpers.ForCurrentYear(drawn) });
            if (taken == 0) return drawn;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> DrawNextRefRunAsync(int histologyType, int count, int lowerInclusive, int upperExclusive, CancellationToken ct = default)
    {
        if (count <= 0) return [];

        using var conn = _db.CreateConnection();
        for (var attempt = 0; attempt < MaxRunAttempts; attempt++)
        {
            // One statement: the row lock it holds reserves the whole run, so no other request can
            // draw between its numbers. TRY_CONVERT skips a non-numeric counter (e.g. the stray 'df');
            // the bounds mean a run that would leave the type's range never touches the counter.
            var start = await conn.ExecuteScalarAsync<int?>(
                """
                UPDATE HistologyRef
                   SET NextHistologyRef = CONVERT(varchar(5), CONVERT(int, NextHistologyRef) + @Count)
                OUTPUT CONVERT(int, DELETED.NextHistologyRef)
                 WHERE [Type] = @Type
                   AND TRY_CONVERT(int, NextHistologyRef) >= @Lower
                   AND TRY_CONVERT(int, NextHistologyRef) + @Count <= @Upper
                   AND TRY_CONVERT(int, NextHistologyRef) + @Count <= 99999
                """,
                new { Type = histologyType, Count = count, Lower = lowerInclusive, Upper = upperExclusive });
            if (start is null) return [];

            var numbers = Enumerable.Range(start.Value, count).Select(n => n.ToString("D5")).ToList();
            var withYear = numbers.Select(Histo.Core.Domain.HistologyRefHelpers.ForCurrentYear).ToList();

            // A number already on a sample can't be part of a consecutive run, so discard this whole
            // run and reserve a fresh one rather than skipping inside it.
            var taken = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Animal WHERE RTRIM(HistologyRef) IN @Refs",
                new { Refs = withYear });
            if (taken == 0) return numbers;
        }

        return [];
    }

    private const int MaxDrawAttempts = 1000;
    private const int MaxRunAttempts = 50;
}
