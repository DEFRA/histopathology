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
    public int ID { get; set; }
    public int BatchSubmissionID { get; set; }
    public string SenderRef { get; set; } = string.Empty;
    public string NextBlockRef { get; set; } = "01";
    public bool HistoRefSet { get; set; }
    public string? HistologyRef { get; set; }
    public bool OnHold { get; set; }
    public string? PMDate { get; set; }
    public bool PMDateSet { get; set; }
    public bool IsPGNumber { get; set; }
    public bool BookedHistologyRef { get; set; }
    public byte[]? RowStamp { get; set; }
}
