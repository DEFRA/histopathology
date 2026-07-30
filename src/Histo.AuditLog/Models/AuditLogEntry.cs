namespace Histo.AuditLog.Models;

/// <summary>
/// Represents a single audit log entry returned by audit log query stored procedures.
///
/// Legacy source: HistopathologyLib/clsAuditLog.vb — GetAuditLogBySubmission,
/// GetAuditLogByDate, GetAuditLogByUser, GetAuditLogTissue all return DataTables
/// with the same column shape; rows are merged into a single result set.
/// </summary>
public sealed class AuditLogEntry
{
    public int ID { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public int EntityID { get; init; }
    public string Action { get; init; } = string.Empty;
    public int UserID { get; init; }
    public string UserName { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string Detail { get; init; } = string.Empty;
}
