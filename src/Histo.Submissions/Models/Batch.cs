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
    /// <summary>
    /// When <see langword="true"/> the sample list is displayed in block-insertion order instead of
    /// the default SenderRef / HistologyRef ascending sort. Persisted in <c>tblBatch.ByPassSort</c>.
    /// Legacy source: <c>BatchBlockSummary.aspx.vb</c>::<c>chkByPassSort_CheckedChanged</c>.
    /// </summary>
    public bool ByPassSort { get; set; }
    public byte[]? RowStamp { get; init; }

    /// <summary>
    /// Submission type: 0 = TSE, 1 = Non-TSE.
    /// Legacy source: <c>Common.vb</c> — SUBMISSION_TSE = 0, SUBMISSION_NONTSE = 1.
    /// Drives which antibody and histology lookup tables are shown in BatchDetails.
    /// </summary>
    public int BatchType { get; init; } = BatchTypeConstants.Tse;

    // ---- Display-only fields populated by GetCommonBatchTablesByID ----

    /// <summary>
    /// Raw project/contract code stored in tblBatch (foreign key to LOOKUP_PROJECTS = 19).
    /// Legacy source: <c>GetCommonBatchTablesByID</c> BATCH_TABLE column "ProjectContractCode"
    /// (see <c>BatchDetails.aspx.vb</c>::<c>SelectItemInDropDownList(ddlProjectCode, .Item("ProjectContractCode")...)</c>).
    /// Unlike <see cref="BatchListResult.ProjectDescription"/>/<see cref="BatchSearchResult.ProjectDescription"/>
    /// (which come from SPs that already JOIN to luProjects), this SP does not join — it returns the raw
    /// code, which must be resolved to a description via <c>ILookupService.GetLookupDataAsync(19)</c> in the view layer.
    /// </summary>
    public string? ProjectContractCode { get; init; }

    /// <summary>
    /// Raw contact/pathologist code stored in tblBatch (foreign key to LOOKUP_CONTACTS = 18).
    /// Legacy source: <c>GetCommonBatchTablesByID</c> BATCH_TABLE column "ContactName" — despite the
    /// column name, this holds the numeric Contact ID, not a display name (see
    /// <c>BatchDetails.aspx.vb</c>::<c>SelectItemInDropDownList(ddlContactName, .Item("ContactName")...)</c>).
    /// Resolved to a description via <c>ILookupService.GetLookupDataAsync(18)</c> in the view layer.
    /// </summary>
    public string? ContactName { get; init; }

    /// <summary>Species name (joined from species lookup). Matches BatchListResult.Species.</summary>
    public string? Species { get; init; }

    /// <summary>
    /// Raw fixation/fixative code stored in tblBatch (foreign key to LOOKUP_FIXATION = 10).
    /// Legacy source: <c>GetCommonBatchTablesByID</c> BATCH_TABLE column "Fixation" (see
    /// <c>BatchDetails.aspx.vb</c>::<c>SelectItemInDropDownList(ddlFixation, .Item("Fixation")...)</c>).
    /// Like <see cref="ProjectContractCode"/>/<see cref="ContactName"/>, this SP does not join to a
    /// description — it returns the raw code, which must be resolved via
    /// <c>ILookupService.GetLookupDataAsync(10)</c> in the view layer.
    /// </summary>
    public string? Fixation { get; init; }

    /// <summary>
    /// Date the samples were returned to the customer.
    /// Legacy source: <c>tblBatch.CustomerReceivedDate</c>, set via the
    /// "Date Returned" workflow on <c>BatchDetails.aspx</c> in receive mode
    /// (<c>SessionVars.SV_ReceiveBatch = True</c>).
    /// </summary>
    public DateTime? CustomerReceivedDate { get; init; }

    // ---- User identity fields — raw IDs resolved to names in the view layer ----

    /// <summary>
    /// ID of the VLA staff member who entered this submission into the system.
    /// Legacy source: <c>tblBatch.SubmittedBy</c>, label "Entered By" on BatchDetails.aspx.
    /// Distinct from <see cref="OtherSubmittedBy"/> (the external customer submitter).
    /// Resolved to a display name via <c>GetAllUsersAsync</c> in the view layer.
    /// </summary>
    public int? SubmittedBy { get; init; }

    /// <summary>
    /// User area code (varchar) of the entering VLA staff member.
    /// Legacy source: <c>tblBatch.SubmittedArea varchar(10)</c>, label "Entered Area" on BatchDetails.aspx.
    /// Resolved to a description via <c>GetUserAreasAsync</c> in the view layer.
    /// </summary>
    public string? SubmittedArea { get; init; }

    /// <summary>
    /// ID of the external user who submitted this batch (the customer).
    /// Legacy source: <c>tblBatch.OtherSubmittedBy</c>, label "Submitted By" on BatchDetails.aspx.
    /// Distinct from <see cref="SubmittedBy"/> (the internal VLA entering user).
    /// Resolved to a display name via <c>GetAllUsersAsync</c> in the view layer.
    /// </summary>
    public int? OtherSubmittedBy { get; init; }

    /// <summary>
    /// User area code (varchar) of the external submitter.
    /// Legacy source: <c>tblBatch.OtherSubmittedArea varchar(10)</c>, label "Submitted Area" on BatchDetails.aspx.
    /// Resolved to a description via <c>GetUserAreasAsync</c> in the view layer.
    /// </summary>
    public string? OtherSubmittedArea { get; init; }

    /// <summary>
    /// Whether the samples are adequately fixed (formalin fixation).
    /// Legacy source: <c>tblBatch.SafeToHandle</c> (SQL bit field),
    /// label "Is it adequately fixed?" on BatchDetails.aspx.
    /// Rendered as "Yes", "No", or "Not specified" in the view.
    /// </summary>
    public bool? SafeToHandle { get; init; }

    // ---- Additional fields mapped from GetBatchDetails SP ----

    /// <summary>Flag set when all tissues have been assigned to blocks.</summary>
    public bool AllTissuesAssigned { get; init; }

    /// <summary>Flag set when all samples share the same project code.</summary>
    public bool SampleSameProjects { get; init; }

    /// <summary>Whether the batch is blocked (all blocks assigned).</summary>
    public bool IsBlocked { get; init; }

    /// <summary>Additional post-fixation information. Legacy source: <c>tblBatch.PostFixationOther</c>.</summary>
    public string? PostFixationOther { get; init; }

    /// <summary>Time the batch was received (stored as int in legacy). Legacy source: <c>tblBatch.TimeReceived</c>.</summary>
    public string? TimeReceived { get; init; }

    /// <summary>User ID who received this batch. Legacy source: <c>tblBatch.ReceivedBy</c>.</summary>
    public int? ReceivedBy { get; init; }
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
