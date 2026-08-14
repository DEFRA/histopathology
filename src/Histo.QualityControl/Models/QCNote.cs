namespace Histo.QualityControl.Models;

/// <summary>
/// Represents a Quality Control note record.
///
/// Legacy source: HistopathologyLib/clsQCNote.vb — CreateQCNoteTable columns.
/// Legacy SP: <c>GetBatchQCNotes</c> returns <c>QCNoteRef</c>, <c>ID</c> (submission),
/// <c>StainRef</c>, <c>ProjectDescription</c>, <c>Species</c> for the list grid.
///
/// <see cref="RowStamp"/> is a SQL Server <c>timestamp</c>/rowversion byte array
/// used for optimistic concurrency in <c>EditQCNote</c> SP. It must be passed
/// back unchanged when updating an existing record. A stale rowstamp causes the
/// SP to return value 1 (concurrent modification), which is surfaced as
/// <see cref="QCNoteConcurrencyException"/>.
/// </summary>
public sealed class QCNote
{
    public int ID { get; init; }
    /// <summary>The QC Note reference number (primary key in legacy system).</summary>
    public int? QCNoteRef { get; init; }
    public int CreatedBy { get; init; }
    public string DateCreated { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;

    // ── Additional columns from GetBatchQCNotes SP ──────────────────────────
    public string? StainRef { get; init; }
    public string? ProjectDescription { get; init; }
    public string? Species { get; init; }

    /// <summary>
    /// SQL Server rowversion (timestamp) — used for optimistic concurrency
    /// in the <c>EditQCNote</c> stored procedure. Must be preserved and returned
    /// on updates.
    /// </summary>
    public byte[]? RowStamp { get; init; }
}
