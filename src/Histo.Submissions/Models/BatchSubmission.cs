namespace Histo.Submissions.Models;

/// <summary>
/// Represents a batch submission (a named sample group within a batch).
///
/// Legacy source: HistopathologyLib/clsBatchSubmission.vb — BATCH_SUBMISSION_TABLE
/// (index 6) from <c>GetBatchSubmissionDetailsByBatchID</c>.
/// </summary>
public sealed class BatchSubmission
{
    public int ID { get; set; }
    public int BatchID { get; set; }
    public int AnimalID { get; set; }
    public string SubmissionName { get; set; } = string.Empty;
    public int Order { get; set; }
    public byte[]? RowStamp { get; set; }
}
