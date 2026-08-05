using Histo.Core.Domain;

namespace Histo.Submissions.Models;

/// <summary>
/// Batch header record — the top-level submission container.
///
/// Legacy source: HistopathologyLib/clsBatch.vb — BATCH_TABLE (index 0)
/// from <c>GetCommonBatchTablesByID</c>.
///
/// Batch status string values are defined in <see cref="BatchStatus"/>.
/// Batch type constants are defined in <see cref="BatchTypeConstants"/>.
/// </summary>
public sealed class Batch
{
    public int ID { get; init; }
    public string Status { get; init; } = BatchStatus.Submitted;
    public string CustomerRef { get; init; } = string.Empty;
    public string? Comments { get; init; }
    public string? StatusComments { get; init; }
    public DateTime? BatchDate { get; init; }
    public DateTime? ReceivedDate { get; init; }
    public DateTime? CompletedDate { get; init; }
    public int SubmittedByUserID { get; init; }
    public int UserAreaCode { get; init; }
    public bool IsPreCassetted { get; init; }
    public byte[]? RowStamp { get; init; }

    /// <summary>
    /// Submission type: 0 = TSE, 1 = Non-TSE.
    /// Legacy source: <c>Common.vb</c> — SUBMISSION_TSE = 0, SUBMISSION_NONTSE = 1.
    /// Drives which antibody and histology lookup tables are shown in BatchDetails.
    /// </summary>
    public int BatchType { get; init; } = BatchTypeConstants.Tse;

    // ---- Display-only fields populated by GetCommonBatchTablesByID ----

    /// <summary>Project/contract description (joined from luProjects). Matches BatchListResult.ProjectDescription.</summary>
    public string? ProjectDescription { get; init; }

    /// <summary>Pathologist/contact description (joined from luContacts). Matches BatchListResult.ContactDescription.</summary>
    public string? ContactDescription { get; init; }

    /// <summary>Species name (joined from species lookup). Matches BatchListResult.Species.</summary>
    public string? Species { get; init; }

    /// <summary>Fixation/fixative description (joined from luFixation).</summary>
    public string? FixationDescription { get; init; }
}

/// <summary>
/// Integer constants for <see cref="Batch.BatchType"/>.
/// Legacy source: HistopathologySystem/Common.vb.
/// </summary>
public static class BatchTypeConstants
{
    /// <summary>TSE submission (default). Legacy: SUBMISSION_TSE = 0.</summary>
    public const int Tse = 0;

    /// <summary>Non-TSE submission. Legacy: SUBMISSION_NONTSE = 1.</summary>
    public const int NonTse = 1;

    public static string DisplayName(int batchType) =>
        batchType == NonTse ? "Non-TSE" : "TSE";
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
