namespace Histo.Submissions.Models;

/// <summary>
/// Represents a batch submission (a named sample group within a batch).
///
/// Legacy source: HistopathologyLib/clsBatchSubmission.vb — BATCH_SUBMISSION_TABLE
/// (index 6) from <c>GetBatchSubmissionDetailsByBatchID</c>.
/// </summary>
public sealed class BatchSubmission
{
    public int ID { get; init; }
    public int BatchID { get; init; }
    public string SubmissionName { get; init; } = string.Empty;
    public int Order { get; init; }
    public byte[]? RowStamp { get; init; }
}
