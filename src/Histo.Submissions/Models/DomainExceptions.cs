namespace Histo.Submissions.Models;

/// <summary>
/// Exception thrown when a concurrent modification is detected by a batch
/// or submission stored procedure (rowstamp mismatch).
///
/// Legacy source: HistopathologyLib/clsBatch.vb — <c>BatchUpdateException</c>.
/// Renamed to follow C# naming conventions.
/// </summary>
public sealed class BatchConcurrencyException : Exception
{
    public BatchConcurrencyException()
        : base("Another user has modified this batch record.") { }

    public BatchConcurrencyException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when a concurrent modification is detected on an animal record.
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — <c>AnimalUpdateException</c>.
/// </summary>
public sealed class AnimalConcurrencyException : Exception
{
    public AnimalConcurrencyException()
        : base("Another user has modified this sample record.") { }
}
