using Histo.AuditLog.Models;

namespace Histo.AuditLog.Interfaces;

/// <summary>
/// Data access contract for audit log queries.
///
/// Legacy source: HistopathologyLib/clsAuditLog.vb — three public query methods
/// translated to async Dapper pattern. The composite submission query (which
/// joins tissues and block tissues) is preserved as a single async method that
/// mirrors the multi-SP fan-out in the original VB implementation.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Returns all audit log entries for a batch submission, optionally filtered
    /// by date range. Includes tissue-level entries from <c>GetAuditLogTissue</c>
    /// (both unblocked and blocked).
    ///
    /// Legacy source: <c>GetSubmissionAuditLogReport</c> in clsAuditLog.vb.
    /// </summary>
    Task<IReadOnlyList<AuditLogEntry>> GetBySubmissionAsync(
        int submissionId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default);

    /// <summary>
    /// Returns audit log entries filtered by date range.
    /// Maps to <c>GetAuditLogByDate</c> stored procedure.
    /// </summary>
    Task<IReadOnlyList<AuditLogEntry>> GetByDateAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);

    /// <summary>
    /// Returns audit log entries for a specific user, optionally filtered by date range.
    /// Maps to <c>GetAuditLogByUser</c> stored procedure.
    /// </summary>
    Task<IReadOnlyList<AuditLogEntry>> GetByUserAsync(
        int userId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default);
}
