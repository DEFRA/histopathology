namespace Histo.Submissions.Models;

/// <summary>
/// Represents an animal/sample record within a batch submission.
///
/// Legacy source: HistopathologyLib/clsAnimal.vb — DataTable columns produced by
/// <c>GetBatchAnimal</c> (via <c>GetAnimalsForBatch</c>).
///
/// PG-number auto-reversal logic is in <see cref="Histo.Core.Domain.AnimalHelpers"/>.
/// </summary>
public sealed class Animal
{
    public int ID { get; init; }
    public int BatchSubmissionID { get; init; }
    public string SenderRef { get; init; } = string.Empty;
    public string NextBlockRef { get; init; } = "01";
    public bool HistoRefSet { get; init; }
    public string? HistologyRef { get; init; }
    public bool OnHold { get; init; }
    public string? PMDate { get; init; }
    public bool PMDateSet { get; init; }
    public bool IsPGNumber { get; init; }
    public bool BookedHistologyRef { get; init; }
    public byte[]? RowStamp { get; init; }
}
