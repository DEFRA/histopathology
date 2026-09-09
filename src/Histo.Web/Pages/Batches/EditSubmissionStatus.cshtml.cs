using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces the status-management half of the former combined <c>EditBatch.aspx</c> page —
/// "Edit submission status". Split out of <see cref="EditBatchModel"/> so status changes
/// (a distinct action with its own audit trail) and header-field edits are no longer mixed
/// on one page, per the GDS "one thing per page" review. Reached only from
/// <see cref="BatchesForEditingModel"/>, whose entire purpose is status management.
/// </summary>
public class EditSubmissionStatusModel : HistoPageModel
{
    private readonly IBatchService  _batches;
    private readonly ILookupService _lookups;
    private readonly IUserService   _users;

    public EditSubmissionStatusModel(ISessionService session, IBatchService batches, ILookupService lookups, IUserService users)
        : base(session)
    {
        _batches = batches;
        _lookups = lookups;
        _users   = users;
    }

    [BindProperty] public string? Status         { get; set; }
    [BindProperty] public string? StatusComments  { get; set; }
    [BindProperty] public string? OriginalStatus  { get; set; }

    public Batch?  Batch     { get; private set; }
    public string? SaveError { get; private set; }

    /// <summary>Read-only context so the user can see what they're changing the status of.</summary>
    public string? ProjectName { get; private set; }
    public string? PathologistName { get; private set; }
    public string? SpeciesName { get; private set; }
    public string? EnteredByName { get; private set; }
    public string? EnteredAreaName { get; private set; }
    public string? SubmittedByName { get; private set; }
    public string? SubmittedAreaName { get; private set; }

    // Falls back to BatchesForEditing when no context is available (this page's only entry point).
    public string ReturnPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Batches/BatchesForEditing"
        : Session.ReturnPage;

    /// <summary>
    /// <see cref="ReturnPage"/> plus the sort/page query string captured when the user left the
    /// list, so Back/Cancel restore the exact filter/sort/page state instead of resetting to
    /// page 1 defaults. Used only for hrefs — never pass this to <c>RedirectToPage</c>, which
    /// requires a bare page name.
    /// </summary>
    public string ReturnUrl => ReturnPage + (Session.ReturnPageQuery ?? string.Empty);

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"]     = "Edit submission status";
        ViewData["PageTitle"] = "Edit submission status";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");

        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch is null) return RedirectToPage("/Index");

        Status         = Batch.Status;
        StatusComments = Batch.StatusComments;
        OriginalStatus = Batch.Status;

        await LoadDisplayFieldsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        ViewData["Title"]     = "Edit submission status";
        ViewData["PageTitle"] = "Edit submission status";

        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch?.RowStamp is null) return RedirectToPage("/Index");

        await LoadDisplayFieldsAsync();

        // ---- Status transition validation (moved from EditBatchModel) ----
        if (Status == BatchStatus.Received && OriginalStatus != BatchStatus.Received)
        {
            SaveError = "Mark a submission as Received using the Receive Submissions workflow.";
            return Page();
        }
        if (Status == BatchStatus.InProgress && OriginalStatus == BatchStatus.Submitted)
        {
            SaveError = "The submission cannot be set to In Progress while still Submitted. Receive it first.";
            return Page();
        }

        // ---- Set DateCompleted when status changes to Completed ----
        var completedDate = Batch.CompletedDate;
        if (Status == BatchStatus.Completed && OriginalStatus != BatchStatus.Completed)
            completedDate = DateTime.Today;
        else if (Status != BatchStatus.Completed)
            completedDate = null;

        // Full field carry-forward — this page never touches header fields, so every field
        // EditBatch owns must be passed through unchanged (EditBatch SP takes the whole row).
        var updated = new Batch
        {
            ID                  = Batch.ID,
            Status              = Status ?? Batch.Status,
            Comments            = Batch.Comments,
            StatusComments      = StatusComments,
            BatchDate           = Batch.BatchDate,
            ReceivedDate        = Batch.ReceivedDate,
            CompletedDate       = completedDate,
            SubmittedByUserID   = Batch.SubmittedByUserID,
            UserAreaCode        = Batch.UserAreaCode,
            IsPreCassetted      = Batch.IsPreCassetted,
            ByPassSort          = Batch.ByPassSort,
            RowStamp            = Batch.RowStamp,
            BatchType           = Batch.BatchType,
            ProjectContractCode = Batch.ProjectContractCode,
            ContactName         = Batch.ContactName,
            Species             = Batch.Species,
            Fixation            = Batch.Fixation,
            CustomerReceivedDate = Batch.CustomerReceivedDate,
            SubmittedBy         = Batch.SubmittedBy,
            SubmittedArea       = Batch.SubmittedArea,
            OtherSubmittedBy    = Batch.OtherSubmittedBy,
            OtherSubmittedArea  = Batch.OtherSubmittedArea ?? "",
            SafeToHandle        = Batch.SafeToHandle,
            IsBlocked           = Batch.IsBlocked,
            SampleSameProjects  = Batch.SampleSameProjects,
            AllTissuesAssigned  = Batch.AllTissuesAssigned,
            TimeReceived        = Batch.TimeReceived,
            ReceivedBy          = Batch.ReceivedBy,
            PostFixationOther   = Batch.PostFixationOther,
        };

        try
        {
            await _batches.UpdateAsync(updated, Session.UserID);
        }
        catch (Exception)
        {
            SaveError = "Failed to save the submission status. Please try again.";
            return Page();
        }

        return RedirectToPage(ReturnPage, ParseReturnQuery());
    }

    /// <summary>Parses <see cref="ISessionService.ReturnPageQuery"/> into route values so a
    /// post-save redirect restores the list's sort/page state, not just its bare page name.</summary>
    private Dictionary<string, string> ParseReturnQuery()
    {
        var result = new Dictionary<string, string>();
        var query = Session.ReturnPageQuery;
        if (string.IsNullOrEmpty(query)) return result;

        foreach (var pair in Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(query))
            result[pair.Key] = pair.Value.ToString();
        return result;
    }

    private async Task LoadDisplayFieldsAsync()
    {
        if (Batch is null) return;

        var summary = await BatchSummaryDisplayResolver.ResolveAsync(Batch, _lookups, _users);
        ProjectName       = summary.ProjectName;
        PathologistName   = summary.PathologistName;
        SpeciesName       = summary.SpeciesName;
        EnteredByName     = summary.EnteredByName;
        EnteredAreaName   = summary.EnteredAreaName;
        SubmittedByName   = summary.SubmittedByName;
        SubmittedAreaName = summary.SubmittedAreaName;
    }
}
