using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Records the customer received date (the "Date Returned" workflow) on a completed batch.
///
/// Legacy source: <c>BatchDetails.aspx</c> in receive mode
/// (<c>SessionVars.SV_ReceiveBatch = True</c>) — the legacy application used a single page
/// for new, edit, view, and date-returned workflows. In the new application these are
/// separated into distinct GDS pages following the one-thing-per-page principle.
///
/// Only <c>CustomerReceivedDate</c> is editable here; the batch status is NOT changed.
/// This matches the legacy <c>btnSave_Click</c> behaviour when <c>SV_ReceiveBatch = True</c>:
/// the save path explicitly skips the status reset to "Submitted" and only updates
/// the <c>CustomerReceivedDate</c> field via <c>UpdateBatchDetails</c>.
///
/// NOTE: <see cref="IBatchService.SetCustomerReceivedDateAsync"/> calls the <c>EditBatch</c>
/// stored procedure with a <c>CustomerReceivedDate</c> parameter. Ensure the SP accepts
/// this parameter before deploying to a new environment.
/// </summary>
public class DateReturnedModel : HistoPageModel
{
    private readonly IBatchService _batches;

    public DateReturnedModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public Batch? Batch { get; private set; }
    public string? Error { get; private set; }

    [BindProperty]
    public DateTime? CustomerReceivedDate { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Date returned";
        ViewData["PageTitle"] = "Date returned";

        if (Session.BatchID is null or <= 0)
            return RedirectToPage("/Submissions/ViewSubmissions");

        Batch = await _batches.GetByIdAsync(Session.BatchID.Value);
        if (Batch is null)
            return RedirectToPage("/Submissions/ViewSubmissions");

        // Guard: only Completed batches can have a date returned recorded.
        // Legacy: this page is only reachable via btnReceiveSubmission which is
        // enabled only when sBatchStatus = STATUS_COMPLETED.
        if (Batch.Status != BatchStatus.Completed)
        {
            Error = "The date returned can only be recorded for a completed submission.";
            return Page();
        }

        // Pre-fill from the existing value if already recorded.
        CustomerReceivedDate = Batch.CustomerReceivedDate;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Date returned";
        ViewData["PageTitle"] = "Date returned";

        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch?.RowStamp is null)
        {
            Error = "Submission not found.";
            return Page();
        }

        if (CustomerReceivedDate is null)
        {
            Error = "Enter the date returned.";
            return Page();
        }

        if (CustomerReceivedDate > DateTime.Today)
        {
            Error = "The date returned cannot be in the future.";
            return Page();
        }

        var ok = await _batches.SetCustomerReceivedDateAsync(
            Batch.ID, CustomerReceivedDate, Batch.RowStamp, Session.UserID);

        if (!ok)
        {
            Error = "Could not save the date returned. The submission may have been modified by another user. Please try again.";
            return Page();
        }

        return RedirectToPage("/Batches/BatchDetails");
    }
}
