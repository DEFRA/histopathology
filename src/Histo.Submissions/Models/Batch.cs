using Histo.Core.Domain;

namespace Histo.Submissions.Models;

/// <summary>
/// Batch header record — the top-level submission container.
///
/// Legacy source: HistopathologyLib/clsBatch.vb — BATCH_TABLE (index 0)
/// from <c>GetCommonBatchTablesByID</c>.
///
/// Batch status string values are defined in <see cref="BatchStatus"/>.
/// </summary>
public sealed class Batch
{
    public int ID { get; init; }
    public string Status { get; init; } = BatchStatus.Submitted;
    public string CustomerRef { get; init; } = string.Empty;
    public string? Comments { get; init; }
    public DateTime? ReceivedDate { get; init; }
    public DateTime? CompletedDate { get; init; }
    public int SubmittedByUserID { get; init; }
    public int UserAreaCode { get; init; }
    public bool IsPreCassetted { get; init; }
    public byte[]? RowStamp { get; init; }
}
