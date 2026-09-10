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
/// Also surfaces histology/antibody/special-stain test-type editing inline (previously the separate
/// <c>EditBatchTests</c> page), matching the Create Submission journey's consistent single-page experience.
/// </summary>
public class EditBatchModel : HistoPageModel
{
    private const int LookupContacts = 18;
    private const int LookupProjects = 19;
    private const int LookupFixation = 10;
    private const int LookupUserArea = 13;
    private const int LookupSubmittedAs = 11;
    private const int LookupTseAntibodies    = 4;
    private const int LookupNonTseAntibodies = 5;
    private const int LookupSpecialStain     = 6;

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

    // ---- Test-type selections (merged from the former EditBatchTests page) ----
    [BindProperty] public List<string> SelectedHistologyCodes { get; set; } = [];
    [BindProperty] public List<string> SelectedAntibodyCodes  { get; set; } = [];
    [BindProperty] public List<string> SelectedStainCodes     { get; set; } = [];

    // ---- Read-only display ----
    public Batch?  Batch     { get; private set; }
    public string? SaveError { get; private set; }

    /// <summary>Legacy: Batch.ascx lblEnteredByVal — read-only, resolved from Batch.SubmittedBy. Never editable on Edit Submission.</summary>
    public string? EnteredByName { get; private set; }
    /// <summary>Legacy: Batch.ascx lblEnteredAreaVal — read-only, resolved from Batch.SubmittedArea. Never editable on Edit Submission.</summary>
    public string? EnteredAreaName { get; private set; }
    /// <summary>Read-only, resolved from LOOKUP_SUBMITTEDAS (11). Fixed at creation — never editable on Edit Submission.</summary>
    public string? SubmittedAsDescription { get; private set; }

    // Falls back to BatchesForEditing when no context is available (legacy SV_RedirectCancelPage)
    public string ReturnPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Batches/BatchesForEditing"
        : Session.ReturnPage;

    /// <summary>
    /// <see cref="ReturnPage"/> plus the sort/page query string captured when the user left the
    /// list, so Back/Cancel restore the exact filter/sort/page state. Hrefs only — never pass to
    /// <c>RedirectToPage</c>, which requires a bare page name.
    /// </summary>
    public string ReturnUrl => ReturnPage + (Session.ReturnPageQuery ?? string.Empty);

    // ---- Lookup data for dropdowns ----
    public IReadOnlyList<LookupItem> Projects    { get; private set; } = [];
    public IReadOnlyList<LookupItem> Contacts    { get; private set; } = [];
    public IReadOnlyList<LookupItem> SpeciesList { get; private set; } = [];
    public IReadOnlyList<LookupItem> Fixations   { get; private set; } = [];
    public IReadOnlyList<LookupItem> UserAreas   { get; private set; } = [];
    public IReadOnlyList<User>       AllUsers    { get; private set; } = [];

    /// <summary>Histology type options, filtered for TSE/NonTSE (mirrors former EditBatchTestsModel).</summary>
    public IReadOnlyList<LookupItem> HistologyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> AntibodyOptions  { get; private set; } = [];
    public IReadOnlyList<LookupItem> StainOptions     { get; private set; } = [];

    /// <summary>True when the current histology selection contains IHC-PrP (TSE) or IHC-Other (NonTSE).</summary>
    public bool ShowAntibodies => SelectedHistologyCodes.Contains(HistologyCode.IhcPrp)
                                || SelectedHistologyCodes.Contains(HistologyCode.IhcOther);

    /// <summary>True when the current histology selection contains Special Stain.</summary>
    public bool ShowStains => SelectedHistologyCodes.Contains(HistologyCode.SpecialStain);

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

        RestoreDraft(); // unsaved edits made before a detour to pick list management

        Session.BatchType = Batch.BatchType;
        await LoadLookupsAsync();
        await LoadDisplayFieldsAsync();
        await LoadTestTypeOptionsAsync(Batch.BatchType);

        // Pre-select the checkboxes from the existing batch-level test selections, unless a
        // draft (from a pick-list detour) already restored them above.
        if (TempData[DraftKey] is null)
        {
            var current = await _batches.GetBatchTestSelectionsAsync(Batch.ID);
            SelectedHistologyCodes = current.Histology.Select(r => r.Code).ToList();
            SelectedAntibodyCodes  = current.Antibodies.Select(r => r.Code).ToList();
            SelectedStainCodes     = current.Stains.Select(r => r.Code).ToList();
        }

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
        List<string> SelectedHistologyCodes,
        List<string> SelectedAntibodyCodes,
        List<string> SelectedStainCodes);

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
            OtherSubmittedArea, SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes));

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
        SelectedHistologyCodes = draft.SelectedHistologyCodes;
        SelectedAntibodyCodes  = draft.SelectedAntibodyCodes;
        SelectedStainCodes     = draft.SelectedStainCodes;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"]     = "Edit submission";
        ViewData["PageTitle"] = "Edit submission";

        try { Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0); }
        catch (Exception ex) { SaveError = "Failed to load the submission. Please go back and try again."; _ = ex; await LoadLookupsAsync(); return Page(); }
        if (Batch?.RowStamp is null) return RedirectToPage("/Index");

        await LoadLookupsAsync();
        await LoadDisplayFieldsAsync();
        await LoadTestTypeOptionsAsync(Batch.BatchType);

        // ---- Test-type validation (mirrors former EditBatchTestsModel) ----
        if (SelectedHistologyCodes.Count == 0)
        {
            SaveError = "Select at least one histology type.";
            return Page();
        }
        if (SelectedHistologyCodes.Contains(HistologyCode.Archive) && SelectedHistologyCodes.Count > 1)
        {
            SaveError = "Archive cannot be combined with other histology types.";
            return Page();
        }
        if (SelectedHistologyCodes.Contains(HistologyCode.SpecialStain) && SelectedStainCodes.Count == 0)
        {
            SaveError = "Special Stain is selected — you must also select at least one special stain.";
            return Page();
        }
        var ihcSelected = SelectedHistologyCodes.Contains(HistologyCode.IhcPrp)
                       || SelectedHistologyCodes.Contains(HistologyCode.IhcOther);
        if (ihcSelected && SelectedAntibodyCodes.Count == 0)
        {
            SaveError = "IHC is selected — you must also select at least one antibody.";
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

        var updated = new Batch
        {
            ID                  = Batch.ID,
            Status              = Batch.Status,
            Comments            = Comments,
            StatusComments      = Batch.StatusComments,
            BatchDate           = batchDate,
            ReceivedDate        = Batch.ReceivedDate,
            CompletedDate       = Batch.CompletedDate,
            SubmittedByUserID   = Batch.SubmittedByUserID,
            UserAreaCode        = Batch.UserAreaCode,
            IsPreCassetted      = IsPreCassetted,
            ByPassSort          = Batch.ByPassSort,
            RowStamp            = Batch.RowStamp,
            // Submission category is fixed at creation (legacy's Cassetted.aspx type-selection
            // step is never re-shown on Edit) — always persist the original value regardless of
            // what was posted, so a crafted request can't change it even though the UI disables it.
            BatchType           = Batch.BatchType,
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

        // Clear stain/antibody selections if their triggering histology codes aren't selected.
        var cleanedStainCodes    = ShowStains     ? SelectedStainCodes    : (IReadOnlyList<string>)[];
        var cleanedAntibodyCodes = ihcSelected     ? SelectedAntibodyCodes : (IReadOnlyList<string>)[];
        if (!await _batches.SaveBatchTestSelectionsAsync(Batch.ID, SelectedHistologyCodes, cleanedAntibodyCodes, cleanedStainCodes, Session.UserID))
        {
            SaveError = "Submission saved, but failed to save test types. Please try again.";
            return Page();
        }

        return RedirectToPage(ReturnPage, ParseReturnQuery());
    }

    /// <summary>Parses <see cref="ISessionService.ReturnPageQuery"/> into route values so a
    /// post-save redirect restores the list's sort/page state, not just its bare page name.</summary>
    private Microsoft.AspNetCore.Routing.RouteValueDictionary ParseReturnQuery()
    {
        var result = new Microsoft.AspNetCore.Routing.RouteValueDictionary();
        var query = Session.ReturnPageQuery;
        if (string.IsNullOrEmpty(query)) return result;

        foreach (var pair in Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(query))
            result[pair.Key] = pair.Value.ToString();
        return result;
    }

    private async Task LoadLookupsAsync()
    {
        // includeInactive: true — an existing submission's saved Project/Pathologist may since have
        // been deactivated; an active-only list would silently drop it from the <select>, causing
        // the browser to default-select the first option instead (looks like "the wrong value is
        // populated"). Matches the established pattern in EditLookupItem.cshtml.cs.
        var projectsTask  = _lookups.GetLookupDataAsync(LookupProjects, includeInactive: true);
        var contactsTask  = _lookups.GetLookupDataAsync(LookupContacts, includeInactive: true);
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

    /// <summary>Resolves the read-only Entered By/Entered Area/Submitted As fields — never editable, so never bound from the form.</summary>
    private async Task LoadDisplayFieldsAsync()
    {
        if (Batch is null) return;

        EnteredByName = AllUsers.FirstOrDefault(u => u.UserID == Batch.SubmittedBy)?.Name;
        EnteredAreaName = UserAreas.FirstOrDefault(a => a.ID.ToString() == Batch.SubmittedArea)?.Name;

        var submittedAsCode = await _batches.GetSubmittedAsCodeAsync(Batch.ID);
        if (!string.IsNullOrEmpty(submittedAsCode))
        {
            var submittedAsOptions = await _lookups.GetLookupDataAsync(LookupSubmittedAs);
            SubmittedAsDescription = submittedAsOptions.FirstOrDefault(o => o.Code == submittedAsCode)?.Name;
        }
    }

    /// <summary>Loads histology/antibody/stain options filtered for TSE/NonTSE (mirrors former EditBatchTestsModel).</summary>
    private async Task LoadTestTypeOptionsAsync(int batchType)
    {
        var antibodyTableId = batchType == BatchTypeConstants.NonTse ? LookupNonTseAntibodies : LookupTseAntibodies;

        var histologyTask = _lookups.GetHistologyTypesAsync();
        var antibodyTask  = _lookups.GetLookupDataAsync(antibodyTableId);
        var stainTask     = _lookups.GetLookupDataAsync(LookupSpecialStain);
        await Task.WhenAll(histologyTask, antibodyTask, stainTask);

        // TSE: hide IHC-Other. NonTSE: hide IHC-PrP and H&E (BSE). Legacy: BatchDetails.aspx.vb::HideOptions()
        HistologyOptions = batchType == BatchTypeConstants.NonTse
            ? histologyTask.Result.Where(i => i.Code != HistologyCode.IhcPrp && i.Code != HistologyCode.HeBse).ToList()
            : histologyTask.Result.Where(i => i.Code != HistologyCode.IhcOther).ToList();
        AntibodyOptions = antibodyTask.Result;
        StainOptions    = stainTask.Result;
    }
}
