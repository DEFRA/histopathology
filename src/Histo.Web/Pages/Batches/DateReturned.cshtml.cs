using Histo.Administration.Interfaces;
using Histo.Administration.Models;
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
    private const int LookupContacts = 18;  // Legacy source: HistopathologySystem/Common.vb — LOOKUP_CONTACTS
    private const int LookupProjects = 19;  // Legacy source: HistopathologySystem/Common.vb — LOOKUP_PROJECTS

    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;

    public DateReturnedModel(ISessionService session, IBatchService batches, ILookupService lookups)
        : base(session)
    {
        _batches = batches;
        _lookups = lookups;
    }

    public Batch? Batch { get; private set; }
    public string? Error { get; private set; }

    /// <summary>Resolved project/contract code description — see BatchDetailsModel.ProjectName for details.</summary>
    public string? ProjectName { get; private set; }

    /// <summary>Resolved pathologist/contact description — see BatchDetailsModel.PathologistName for details.</summary>
    public string? PathologistName { get; private set; }

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

        await ResolveDisplayNamesAsync();

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

        await ResolveDisplayNamesAsync();

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

    /// <summary>
    /// Resolves the raw ProjectContractCode / ContactName codes on <see cref="Batch"/> to display
    /// descriptions via LOOKUP_PROJECTS (19) / LOOKUP_CONTACTS (18) — same root-cause fix as
    /// BatchDetailsModel, since GetCommonBatchTablesByID returns raw codes, not joined descriptions.
    /// </summary>
    private async Task ResolveDisplayNamesAsync()
    {
        if (Batch is null) return;

        var projectsTask = _lookups.GetLookupDataAsync(LookupProjects);
        var contactsTask = _lookups.GetLookupDataAsync(LookupContacts);
        await Task.WhenAll(projectsTask, contactsTask);

        ProjectName     = ResolveName(Batch.ProjectContractCode, projectsTask.Result);
        PathologistName = ResolveName(Batch.ContactName, contactsTask.Result);
    }

    private static string? ResolveName(string? code, IReadOnlyList<LookupItem> items)
    {
        if (string.IsNullOrWhiteSpace(code)) return code;
        var match = items.FirstOrDefault(i => string.Equals(i.ID.ToString(), code, StringComparison.OrdinalIgnoreCase));
        return match?.Name ?? code;
    }
}
