using Histo.Core.Domain;
using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces <c>EditBatch.aspx</c> — "Edit Submission Status".
///
/// Legacy behaviour preserved:
/// <list type="bullet">
///   <item>Status can be changed to any value except "Received" (that transition
///     is reserved for the Receive Submission workflow via <c>ReceiveBatch</c>).</item>
///   <item>Status cannot be changed to "In Progress" if the current status is
///     still "Submitted" (the batch must be received first).</item>
///   <item>When status is set to "Completed", <c>DateCompleted</c> is recorded
///     automatically by the <c>EditBatch</c> stored procedure.</item>
/// </list>
/// Additionally exposes CustomerRef, Comments, and IsPreCassetted — batch header
/// fields also editable from this screen (legacy <c>BatchDetails.aspx</c> fields
/// that were surfaced in the combined edit flow).
/// </summary>
public class EditBatchModel : HistoPageModel
{
    private readonly BatchService _batches;

    public EditBatchModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    // ---- editable fields ----
    [BindProperty] public string  CustomerRef    { get; set; } = string.Empty;
    [BindProperty] public string? Comments       { get; set; }
    [BindProperty] public bool    IsPreCassetted { get; set; }
    [BindProperty] public string? Status         { get; set; }
    [BindProperty] public string? StatusComments { get; set; }

    public Batch?  Batch     { get; private set; }
    public string? SaveError { get; private set; }

    /// <summary>Current status at page-load time — used for validation on POST.</summary>
    [BindProperty] public string? OriginalStatus { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"]     = "Edit submission status";
        ViewData["PageTitle"] = "Edit submission status";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");

        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch is null) return RedirectToPage("/Index");

        CustomerRef    = Batch.CustomerRef;
        Comments       = Batch.Comments;
        IsPreCassetted = Batch.IsPreCassetted;
        Status         = Batch.Status;
        StatusComments = Batch.StatusComments;
        OriginalStatus = Batch.Status;
        Session.BatchType = Batch.BatchType;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"]     = "Edit submission status";
        ViewData["PageTitle"] = "Edit submission status";

        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch?.RowStamp is null) return RedirectToPage("/Index");

        // ---- Status transition validation (mirrors legacy EditBatch.aspx.vb) ----
        if (Status == BatchStatus.Received && OriginalStatus != BatchStatus.Received)
        {
            SaveError = "The submission can only be marked as Received using the Receive Submission workflow.";
            return Page();
        }

        if (Status == BatchStatus.InProgress && OriginalStatus == BatchStatus.Submitted)
        {
            SaveError = "The submission cannot be set to In Progress while it is still Submitted. " +
                        "Receive the submission first using Receive Submissions.";
            return Page();
        }

        // ---- Persist batch header (CustomerRef / Comments / IsPreCassetted) ----
        var updated = new Batch
        {
            ID             = Batch.ID,
            Status         = Batch.Status,          // status updated separately below
            CustomerRef    = CustomerRef,
            Comments       = Comments,
            IsPreCassetted = IsPreCassetted,
            StatusComments = StatusComments,
            BatchDate      = Batch.BatchDate,
            ReceivedDate   = Batch.ReceivedDate,
            CompletedDate  = Batch.CompletedDate,
            SubmittedByUserID = Batch.SubmittedByUserID,
            UserAreaCode   = Batch.UserAreaCode,
            BatchType      = Batch.BatchType,
            RowStamp       = Batch.RowStamp,
        };

        if (!await _batches.UpdateAsync(updated, Session.UserID))
        {
            SaveError = "Failed to save batch details. Please try again.";
            return Page();
        }

        // ---- Persist status change (if changed) ----
        if (!string.IsNullOrEmpty(Status) && Status != OriginalStatus)
        {
            var ok = await _batches.UpdateStatusAsync(
                Session.BatchID ?? 0, Status, Batch.RowStamp, Session.UserID);
            if (!ok)
            {
                SaveError = "Failed to update status. The batch may have been modified by another user.";
                return Page();
            }
        }

        return RedirectToPage("/Batches/BatchesForEditing");
    }
}
