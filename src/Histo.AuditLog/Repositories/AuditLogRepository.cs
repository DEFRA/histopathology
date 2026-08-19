using Dapper;
using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Infrastructure;

namespace Histo.AuditLog.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IAuditLogRepository"/>.
///
/// The submission audit query mirrors the legacy multi-SP fan-out in clsAuditLog.vb:
///  1. <c>GetAuditLogBySubmission</c> for the batch-level rows.
///  2. <c>GetBatchTissuesIDs</c> + <c>GetAuditLogTissue</c> (Blocked=0) for each tissue.
///  3. <c>GetBatchBlockTissuesIDs</c> + <c>GetAuditLogTissue</c> (Blocked=1) for each block tissue.
///  4. <c>GetBatchBlockAnimalIDs</c> + <c>GetAuditLogTissue</c> for block animals.
/// All results are merged into a single flat list.
/// </summary>
public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnectionFactory _db;

    public AuditLogRepository(IDbConnectionFactory db) => _db = db;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuditLogEntry>> GetBySubmissionAsync(
        int submissionId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        var results = new List<AuditLogEntry>();

        // 1 — batch-level audit rows
        var batchRows = await conn.QueryAsync<AuditLogEntry>(
            "GetAuditLogBySubmission",
            new
            {
                StartDate    = (object?)startDate ?? DBNull.Value,
                EndDate      = (object?)endDate   ?? DBNull.Value,
                SubmissionID = submissionId,
            },
            commandType: System.Data.CommandType.StoredProcedure);
        results.AddRange(batchRows);

        // 2 — tissue IDs (unblocked), then per-tissue audit rows
        var tissueIds = await conn.QueryAsync<int>(
            "GetBatchTissuesIDs",
            new { ID = submissionId },
            commandType: System.Data.CommandType.StoredProcedure);

        foreach (var id in tissueIds)
        {
            var rows = await conn.QueryAsync<AuditLogEntry>(
                "GetAuditLogTissue",
                new
                {
                    ID        = id,
                    Blocked   = 0,
                    StartDate = (object?)startDate ?? DBNull.Value,
                    EndDate   = (object?)endDate   ?? DBNull.Value,
                },
                commandType: System.Data.CommandType.StoredProcedure);
            results.AddRange(rows);
        }

        // 3 — block tissue IDs (blocked)
        var blockTissueIds = await conn.QueryAsync<int>(
            "GetBatchBlockTissuesIDs",
            new { ID = submissionId },
            commandType: System.Data.CommandType.StoredProcedure);

        foreach (var id in blockTissueIds)
        {
            var rows = await conn.QueryAsync<AuditLogEntry>(
                "GetAuditLogTissue",
                new
                {
                    ID        = id,
                    Blocked   = 1,
                    StartDate = (object?)startDate ?? DBNull.Value,
                    EndDate   = (object?)endDate   ?? DBNull.Value,
                },
                commandType: System.Data.CommandType.StoredProcedure);
            results.AddRange(rows);
        }

        // 4 — block animal IDs
        var blockAnimalIds = await conn.QueryAsync<int>(
            "GetBatchBlockAnimalIDs",
            new { ID = submissionId },
            commandType: System.Data.CommandType.StoredProcedure);

        foreach (var id in blockAnimalIds)
        {
            var rows = await conn.QueryAsync<AuditLogEntry>(
                "GetAuditLogTissue",
                new
                {
                    ID        = id,
                    Blocked   = 1,
                    StartDate = (object?)startDate ?? DBNull.Value,
                    EndDate   = (object?)endDate   ?? DBNull.Value,
                },
                commandType: System.Data.CommandType.StoredProcedure);
            results.AddRange(rows);
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuditLogEntry>> GetByDateAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        using var conn = _db.CreateConnection();
        // Legacy SP GetAuditLogByDate accepts a single @LogDate parameter
        // (clsAuditLog.vb::GetDailyAuditLogReport — confirmed from legacy source).
        // The SP filters for that calendar day.  The UI passes a date range; to support
        // multi-day ranges we call the SP once per calendar day and merge the results.
        var results = new List<AuditLogEntry>();
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var rows = await conn.QueryAsync<AuditLogEntry>(
                "GetAuditLogByDate",
                new { LogDate = date },
                commandType: System.Data.CommandType.StoredProcedure);
            results.AddRange(rows);
        }
        return results;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuditLogEntry>> GetByUserAsync(
        int userId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default)
    {
        // Extend endDate to end-of-day so records on the selected date are included.
        var inclusiveEnd = endDate.HasValue ? endDate.Value.Date.AddDays(1).AddTicks(-1) : (DateTime?)null;
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<AuditLogEntry>(
            "GetAuditLogByUser",
            new
            {
                UserID    = userId,
                StartDate = (object?)startDate ?? DBNull.Value,
                EndDate   = (object?)inclusiveEnd ?? DBNull.Value,
            },
            commandType: System.Data.CommandType.StoredProcedure);
        return rows.ToList();
    }

    // -----------------------------------------------------------------------
    // No dynamic mapping helpers needed — the AuditLogDapperSetup.RegisterTypeMaps()
    // call at startup configures a CustomPropertyTypeMap that maps the SP column
    // "DateTime" → AuditLogEntry.ChangedAt, so QueryAsync<AuditLogEntry> works directly.
}
