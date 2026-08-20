namespace Histo.QualityControl.Models;

/// <summary>
/// Represents a Quality Control note record.
///
/// Legacy source: HistopathologyLib/clsQCNote.vb.
/// Legacy SP: <c>GetBatchQCNotes</c> returns <c>QCNoteRef</c>, <c>ID</c> (batch/submission ID),
/// <c>StainRef</c>, <c>ProjectDescription</c>, <c>Species</c> for the list grid.
/// <c>GetQCNoteHistStainTestInformation</c> returns <c>QCText</c> and <c>RowStamp</c>
/// for the edit form.
///
/// <see cref="RowStamp"/> is a SQL Server <c>timestamp</c>/rowversion byte array
/// used for optimistic concurrency in <c>EditQCNote</c> SP.
/// </summary>
public sealed class QCNote
{
    /// <summary>Batch/Submission ID — from GetBatchQCNotes [Batch].[ID] column.</summary>
    public int ID { get; init; }
    /// <summary>The QC Note reference number (primary key in QCNotes table).</summary>
    public int? QCNoteRef { get; init; }
    public string? StainRef { get; init; }
    public string? ProjectDescription { get; init; }
    public string? Species { get; init; }
    /// <summary>QC note text — populated only by <see cref="IQCNoteRepository.GetByIdAsync"/>.</summary>
    public string Text { get; init; } = string.Empty;
    /// <summary>
    /// SQL Server rowversion (timestamp) — used for optimistic concurrency
    /// in the <c>EditQCNote</c> stored procedure.
    /// </summary>
    public byte[]? RowStamp { get; init; }
}
