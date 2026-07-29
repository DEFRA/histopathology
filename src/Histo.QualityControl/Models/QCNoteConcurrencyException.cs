namespace Histo.QualityControl.Models;

/// <summary>
/// Thrown when an attempt to update a QC note fails because another user has
/// modified the record since it was last read.
///
/// Legacy source: HistopathologyLib/clsQCNote.vb — the <c>EditQCNote</c> SP
/// returns value 1 to signal a concurrent modification. The legacy code threw
/// <c>Exception("Another user has altered the QC Note record.")</c>. This
/// typed exception replaces that pattern.
/// </summary>
public sealed class QCNoteConcurrencyException : Exception
{
    public QCNoteConcurrencyException()
        : base("Another user has altered the QC Note record.") { }
}
