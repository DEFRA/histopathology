namespace Histo.QualityControl.Models;

/// <summary>Outcome of <see cref="Histo.QualityControl.Interfaces.IQCNoteService.AppendAsync"/>.</summary>
public enum QCNoteAppendResult
{
    Appended,
    NothingToAppend,
    NotFound,
    TooLong,
    Concurrency,
}
