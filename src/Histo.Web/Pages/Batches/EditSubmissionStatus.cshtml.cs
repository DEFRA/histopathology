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
    private const int LookupContacts = 18;
    private const int LookupProjects = 19;

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

    // Falls back to BatchesForEditing when no context is available (this page's only entry point).
    public string ReturnPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Batches/BatchesForEditing"
        : Session.ReturnPage;

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

        return RedirectToPage(ReturnPage);
    }

    private async Task LoadDisplayFieldsAsync()
    {
        if (Batch is null) return;

        var projectsTask  = _lookups.GetLookupDataAsync(LookupProjects, includeInactive: true);
        var contactsTask  = _lookups.GetLookupDataAsync(LookupContacts, includeInactive: true);
        var speciesTask   = _lookups.GetSpeciesLookupAsync();
        var usersTask     = _users.GetAllUsersAsync();
        var areasTask     = _lookups.GetUserAreasAsync();
        await Task.WhenAll(projectsTask, contactsTask, speciesTask, usersTask, areasTask);

        var projectsById = projectsTask.Result.ToDictionary(i => i.ID.ToString(), i => i.Name);
        var contactsById = contactsTask.Result.ToDictionary(i => i.ID.ToString(), i => i.Name);
        var speciesById  = speciesTask.Result.ToDictionary(i => i.ID.ToString(), i => i.Name, StringComparer.OrdinalIgnoreCase);
        var userById     = usersTask.Result.ToDictionary(u => u.UserID, u => u.Name);
        var areaByCode   = areasTask.Result.ToDictionary(a => a.ID.ToString(), a => a.Name, StringComparer.OrdinalIgnoreCase);

        ProjectName     = !string.IsNullOrWhiteSpace(Batch.ProjectContractCode) && projectsById.TryGetValue(Batch.ProjectContractCode, out var pn) ? pn : Batch.ProjectContractCode;
        PathologistName = !string.IsNullOrWhiteSpace(Batch.ContactName) && contactsById.TryGetValue(Batch.ContactName, out var cn) ? cn : Batch.ContactName;
        SpeciesName     = !string.IsNullOrWhiteSpace(Batch.Species) && speciesById.TryGetValue(Batch.Species, out var sn) ? sn : Batch.Species;
        EnteredByName   = Batch.SubmittedBy.HasValue && userById.TryGetValue(Batch.SubmittedBy.Value, out var eb) ? eb : null;
        EnteredAreaName = !string.IsNullOrEmpty(Batch.SubmittedArea) && areaByCode.TryGetValue(Batch.SubmittedArea, out var ea) ? ea : null;
    }
}
