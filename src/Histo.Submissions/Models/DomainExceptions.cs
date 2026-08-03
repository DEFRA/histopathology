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

/// <summary>
/// Exception thrown when the <c>EditAnimalSenderRef</c> / <c>EditAnimalHistologyRef</c>
/// stored procedures reject a Sender Ref / Histology Ref rename — the original
/// Sender Ref was not found, or the new reference is already used by another sample.
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — <c>AnimalUpdateException</c>,
/// as raised from <c>UpdateAnimalSenderRef</c> / <c>UpdateAnimalHistologyRef</c>.
/// </summary>
public sealed class AnimalRefUpdateException : Exception
{
    public AnimalRefUpdateException(string message) : base(message) { }
}
