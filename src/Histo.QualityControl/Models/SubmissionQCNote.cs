namespace Histo.QualityControl.Models;

/// <summary>
/// A submission's QC note. <see cref="LinkedTestRowStamp"/> is set only when the note was just created and
/// the test was linked in the same transaction — callers must use it for that test's next save.
/// </summary>
public sealed record SubmissionQCNote(int QCNoteRef, byte[]? LinkedTestRowStamp);
