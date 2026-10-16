using Histo.Infrastructure.Exceptions;

namespace Histo.QualityControl.Models;

/// <summary>
/// The test being linked to a new QC note changed since the caller read it, so nothing was created or linked.
/// </summary>
public sealed class QCNoteLinkConflictException : HistoConcurrencyException
{
    public QCNoteLinkConflictException()
        : base("The test was modified by another user, so the QC note was not created.") { }
}
