using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces <c>EditBatch.aspx</c> — "Edit submission".
/// Provides the complete set of editable batch header fields matching legacy <c>BatchDetails.aspx</c>
/// edit mode, plus status management from the original <c>EditBatch.aspx</c>.
/// </summary>
public class EditBatchModel : HistoPageModel
{
    private const int LookupContacts = 18;
    private const int LookupProjects = 19;
    private const int LookupFixation = 10;
    private const int LookupUserArea = 13;

    private readonly IBatchService   _batches;
    private readonly ILookupService  _lookups;
    private readonly IUserService    _users;

    public EditBatchModel(ISessionService session, IBatchService batches, ILookupService lookups, IUserService users)
        : base(session)
    {
        _batches = batches;
        _lookups = lookups;
        _users   = users;
    }

    // ---- Required editable fields ----
    [BindProperty] public string? ProjectContractCode { get; set; }
    [BindProperty] public string? ContactName         { get; set; }
    [BindProperty] public string? SpeciesId           { get; set; }
    [BindProperty] public string? BatchDateStr        { get; set; }
    [BindProperty] public int     BatchTypeField      { get; set; }

    // ---- Optional editable fields ----
    [BindProperty] public string? Fixation            { get; set; }
    [BindProperty] public bool    SafeToHandle        { get; set; }
    [BindProperty] public bool    IsPreCassetted      { get; set; }
    [BindProperty] public string? Comments            { get; set; }
    [BindProperty] public int?    OtherSubmittedBy    { get; set; }
    [BindProperty] public string? OtherSubmittedArea  { get; set; }

    // ---- Status fields ----
    [BindProperty] public string? Status         { get; set; }
    [BindProperty] public string? StatusComments { get; set; }
    [BindProperty] public string? OriginalStatus { get; set; }

    // ---- Read-only display ----
    public Batch?  Batch     { get; private set; }
    public string? SaveError { get; private set; }

    // Falls back to BatchesForEditing when no context is available (legacy SV_RedirectCancelPage)
    public string ReturnPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Batches/BatchesForEditing"
        : Session.ReturnPage;

    // ---- Lookup data for dropdowns ----
    public IReadOnlyList<LookupItem> Projects    { get; private set; } = [];
    public IReadOnlyList<LookupItem> Contacts    { get; private set; } = [];
    public IReadOnlyList<LookupItem> SpeciesList { get; private set; } = [];
    public IReadOnlyList<LookupItem> Fixations   { get; private set; } = [];
    public IReadOnlyList<LookupItem> UserAreas   { get; private set; } = [];
    public IReadOnlyList<User>       AllUsers    { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"]     = "Edit submission";
        ViewData["PageTitle"] = "Edit submission";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");

        try { Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0); }
        catch (Exception ex) { SaveError = $"Error loading submission: {ex.Message}"; await LoadLookupsAsync(); return Page(); }
        if (Batch is null) return RedirectToPage("/Index");

        // Entering the Edit submission journey unambiguously means "not read-only" — clears a stale
        // IsViewSubmissionMode flag left over from a prior ViewSubmissions/SearchSubmissions visit,
        // which otherwise kept Add/Edit/Copy sample hidden on BatchBlockSummary. Mirrors legacy
        // btnEditSubmission_Click: Session(SV_ViewSubmission) = False.
        Session.IsViewSubmissionMode = false;

        // Pre-populate editable fields from loaded batch
        ProjectContractCode = Batch.ProjectContractCode;
        ContactName         = Batch.ContactName;
        SpeciesId           = Batch.Species;
        BatchDateStr        = Batch.BatchDate?.ToString("dd/MM/yyyy") ?? DateTime.Today.ToString("dd/MM/yyyy");
        BatchTypeField      = Batch.BatchType;
        Fixation            = Batch.Fixation;
        SafeToHandle        = Batch.SafeToHandle ?? false;
        IsPreCassetted      = Batch.IsPreCassetted;
        Comments            = Batch.Comments;
        OtherSubmittedBy    = Batch.OtherSubmittedBy;
        OtherSubmittedArea  = Batch.OtherSubmittedArea;
        Status              = Batch.Status;
        StatusComments      = Batch.StatusComments;
        OriginalStatus      = Batch.Status;

        RestoreDraft(); // unsaved edits made before a detour to pick list management

        Session.BatchType = Batch.BatchType;
        await LoadLookupsAsync();
        return Page();
    }

    /// <summary>
    /// TempData slot holding unsaved edits while the user detours to pick list management.
    /// Mirrors legacy <c>btnNewProject</c>/<c>btnNewContact</c>, which called
    /// <c>UpdateSessionWithBatchDetails()</c> before redirecting so nothing was lost.
    /// </summary>
    private const string DraftKey = "EditBatch_Draft";

    private sealed record EditDraft(
        string? ProjectContractCode,
        string? ContactName,
        string? SpeciesId,
        string? BatchDateStr,
        int BatchTypeField,
        string? Fixation,
        bool SafeToHandle,
        bool IsPreCassetted,
        string? Comments,
        int? OtherSubmittedBy,
        string? OtherSubmittedArea,
        string? Status,
        string? StatusComments,
        string? OriginalStatus);

    /// <summary>
    /// Replaces legacy <c>btnNewSubmittedBy</c> / <c>btnNewProject</c> / <c>btnNewContact</c> —
    /// saves the part-edited submission, then opens the maintenance page for that field's value
    /// list with a return link back to this form.
    /// </summary>
    public IActionResult OnPostManagePickList(string field)
    {
        TempData[DraftKey] = System.Text.Json.JsonSerializer.Serialize(new EditDraft(
            ProjectContractCode, ContactName, SpeciesId, BatchDateStr, BatchTypeField,
            Fixation, SafeToHandle, IsPreCassetted, Comments, OtherSubmittedBy,
            OtherSubmittedArea, Status, StatusComments, OriginalStatus));

        var returnUrl = Url.Page("/Batches/EditBatch");

        return field switch
        {
            "submittedBy" => RedirectToPage("/Admin/UserMaintenance", new { returnUrl }),
            "project"     => RedirectToPage("/Admin/PickListUserArea", new { tableId = LookupProjects, returnUrl }),
            "pathologist" => RedirectToPage("/Admin/PickListUserArea", new { tableId = LookupContacts, returnUrl }),
            _             => RedirectToPage("/Batches/EditBatch"),
        };
    }

    private void RestoreDraft()
    {
        if (TempData[DraftKey] is not string json) return;

        EditDraft? draft;
        try { draft = System.Text.Json.JsonSerializer.Deserialize<EditDraft>(json); }
        catch (System.Text.Json.JsonException) { return; }
        if (draft is null) return;

        ProjectContractCode = draft.ProjectContractCode;
        ContactName         = draft.ContactName;
        SpeciesId           = draft.SpeciesId;
        BatchDateStr        = draft.BatchDateStr;
        BatchTypeField      = draft.BatchTypeField;
        Fixation            = draft.Fixation;
        SafeToHandle        = draft.SafeToHandle;
        IsPreCassetted      = draft.IsPreCassetted;
        Comments            = draft.Comments;
        OtherSubmittedBy    = draft.OtherSubmittedBy;
        OtherSubmittedArea  = draft.OtherSubmittedArea;
        Status              = draft.Status;
        StatusComments      = draft.StatusComments;
        OriginalStatus      = draft.OriginalStatus;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"]     = "Edit submission";
        ViewData["PageTitle"] = "Edit submission";

        try { Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0); }
        catch (Exception ex) { SaveError = "Failed to load the submission. Please go back and try again."; _ = ex; await LoadLookupsAsync(); return Page(); }
        if (Batch?.RowStamp is null) return RedirectToPage("/Index");

        await LoadLookupsAsync();

        // ---- Status transition validation ----
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

        // ---- Parse BatchDate ----
        DateTime? batchDate = Batch.BatchDate;
        if (!string.IsNullOrWhiteSpace(BatchDateStr))
        {
            if (!DateTime.TryParseExact(BatchDateStr, ["dd/MM/yyyy", "d/M/yyyy"],
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                SaveError = "Enter a valid date of submission in DD/MM/YYYY format.";
                return Page();
            }
            batchDate = parsedDate;
        }

        // ---- Set DateCompleted when status changes to Completed ----
        var completedDate = Batch.CompletedDate;
        if (Status == BatchStatus.Completed && OriginalStatus != BatchStatus.Completed)
            completedDate = DateTime.Today;
        else if (Status != BatchStatus.Completed)
            completedDate = null;

        var updated = new Batch
        {
            ID                  = Batch.ID,
            Status              = Status ?? Batch.Status,
            Comments            = Comments,
            StatusComments      = StatusComments,
            BatchDate           = batchDate,
            ReceivedDate        = Batch.ReceivedDate,
            CompletedDate       = completedDate,
            SubmittedByUserID   = Batch.SubmittedByUserID,
            UserAreaCode        = Batch.UserAreaCode,
            IsPreCassetted      = IsPreCassetted,
            ByPassSort          = Batch.ByPassSort,
            RowStamp            = Batch.RowStamp,
            BatchType           = BatchTypeField,
            ProjectContractCode = ProjectContractCode,
            ContactName         = ContactName,
            Species             = SpeciesId,
            Fixation            = Fixation,
            CustomerReceivedDate = Batch.CustomerReceivedDate,
            SubmittedBy         = Batch.SubmittedBy,
            SubmittedArea       = Batch.SubmittedArea,
            OtherSubmittedBy    = OtherSubmittedBy,
            OtherSubmittedArea  = OtherSubmittedArea ?? "",
            SafeToHandle        = SafeToHandle,
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
        catch (Exception ex)
        {
            SaveError = "Failed to save the submission. Please try again.";
            return Page();
        }

        return RedirectToPage(ReturnPage);
    }

    private async Task LoadLookupsAsync()
    {
        var projectsTask  = _lookups.GetLookupDataAsync(LookupProjects);
        var contactsTask  = _lookups.GetLookupDataAsync(LookupContacts);
        var speciesTask   = _lookups.GetSpeciesLookupAsync();
        var fixationTask  = _lookups.GetLookupDataAsync(LookupFixation);
        var areaTask      = _lookups.GetLookupDataAsync(LookupUserArea);
        var usersTask     = _users.GetAllUsersAsync();
        await Task.WhenAll(projectsTask, contactsTask, speciesTask, fixationTask, areaTask, usersTask);
        Projects    = projectsTask.Result;
        Contacts    = contactsTask.Result;
        SpeciesList = speciesTask.Result;
        Fixations   = fixationTask.Result;
        UserAreas   = areaTask.Result;
        AllUsers    = [.. usersTask.Result];
    }
}
