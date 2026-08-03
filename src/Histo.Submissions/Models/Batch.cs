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

/// <summary>
/// One test-item dispatch status row, used only by the Fix Completed Dates admin
/// utility to determine whether every test on a batch has been dispatched.
///
/// Legacy source: <c>FixCompletedDates.aspx.vb</c> — <c>GetBatchHistology</c>,
/// <c>GetBatchStain</c> and <c>GetBatchAntibodies</c> (which call
/// <c>GetHistologyDispatched</c>, <c>GetStainDispatched</c> and
/// <c>GetAntibodiesDispatched</c> respectively) all return the same shape.
/// </summary>
public sealed class TestDispatchStatus
{
    public bool Dispatched { get; init; }
    public DateTime? DispatchedDate { get; init; }
}
