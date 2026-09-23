namespace Histo.AuditLog.Models;

/// <summary>
/// Represents a single audit log entry returned by audit log query stored procedures.
///
/// Legacy source: HistopathologyLib/clsAuditLog.vb — GetAuditLogBySubmission,
/// GetAuditLogByDate, GetAuditLogByUser, GetAuditLogTissue all return DataTables
/// with the same column shape; rows are merged into a single result set.
///
/// Legacy SP column shape: <c>TableName</c>, <c>FieldName</c>, <c>DateTime</c>,
/// <c>UserName</c>, <c>BeforeValue</c>, <c>AfterValue</c>, <c>Reason</c>, <c>KeyID</c>.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>
    /// Business key of the audited record. Not a strict integer: the audit SPs
    /// return <c>AuditLog.ID</c> untouched, and for lookup-table edits (e.g.
    /// <c>luArchiveLocation</c>) it is that table's varchar <c>Code</c>, not a
    /// numeric surrogate key. A previous <c>int</c> declaration threw a
    /// FormatException on any such row, silently swallowed by the service's
    /// catch-all and returning an empty result for the whole date/user/submission
    /// query. Not displayed anywhere (the grid's "Key" column is <see cref="KeyID"/>).
    /// </summary>
    public string? ID { get; init; }

    // ── Legacy granular columns (compliance-critical) ────────────────────────
    /// <summary>Database table that was modified (e.g. BATCH_TABLE, ANIMAL_TABLE).</summary>
    public string? TableName { get; init; }
    /// <summary>Field/column that was modified.</summary>
    public string? FieldName { get; init; }
    /// <summary>Value before the change.</summary>
    public string? BeforeValue { get; init; }
    /// <summary>Value after the change.</summary>
    public string? AfterValue { get; init; }
    /// <summary>Reason recorded for the change.</summary>
    public string? Reason { get; init; }
    /// <summary>Primary key of the modified record.</summary>
    public string? KeyID { get; init; }

    // ── Supporting columns ───────────────────────────────────────────────────
    public string EntityType { get; init; } = string.Empty;
    public int EntityID { get; init; }
    public string Action { get; init; } = string.Empty;
    public int UserID { get; init; }
    public string UserName { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string Detail { get; init; } = string.Empty;
}
