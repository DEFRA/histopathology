using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Read-only batch summary page — replaces <c>BatchDetails.aspx</c> in view mode
/// (<c>SessionVars.SV_ViewSubmission = True</c>).
///
/// The legacy <c>BatchDetails.aspx</c> served four distinct modes in one page:
/// New, Edit, View, and Date-Returned. In the new application these are separated
/// into distinct GDS pages:
/// <list type="bullet">
/// <item><description><c>Cassetted.cshtml</c> + <c>AddSubmission.cshtml</c> — new submission</description></item>
/// <item><description><c>EditBatch.cshtml</c> — edit submission status / comments</description></item>
/// <item><description><c>BatchDetails.cshtml</c> — read-only view (this page)</description></item>
/// <item><description><c>DateReturned.cshtml</c> — set customer received date on completed batches</description></item>
/// </list>
///
/// Action buttons are gated on batch status, matching the legacy <c>EnableDisableControls()</c> logic.
/// The back link is context-aware: it reads <see cref="ISessionService.ReturnPage"/> which is set
/// by the list page (<c>ViewSubmissions</c>, <c>SearchSubmissions</c>) before navigating here,
/// replacing the legacy <c>SessionVars.SV_RedirectCancelPage</c> pattern.
/// </summary>
public class BatchDetailsModel : HistoPageModel
{
    private const int LookupContacts    = 18;
    private const int LookupProjects    = 19;
    private const int LookupFixation    = 10;
    private const int LookupSubmittedAs      = 11;
    private const int LookupUserArea          = 13;
    private const int LookupTseAntibodies     = 4;
    private const int LookupNonTseAntibodies  = 5;
    private const int LookupSpecialStain      = 6;

    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;
    private readonly IUserService _users;
    private readonly ISubmissionService _submissions;

    public BatchDetailsModel(ISessionService session, IBatchService batches, ILookupService lookups, IUserService users, ISubmissionService submissions)
        : base(session)
    {
        _batches = batches;
        _lookups = lookups;
        _users   = users;
        _submissions = submissions;
    }

    // ── Query param — "create" activates the new-batch form ──
    [BindProperty(SupportsGet = true)] public string? Mode { get; set; }
    public bool IsCreateMode => string.Equals(Mode, "create", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Batch ID from the URL (route/query), used in view mode only — create mode has no batch yet.
    /// Falls back to <see cref="ISessionService.BatchID"/> for links not yet migrated.
    /// </summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    // ── Create-mode form fields (map to AddBatch SP params) ──
    [BindProperty] public string? Create_ProjectContractCode { get; set; }
    [BindProperty] public string? Create_ContactName         { get; set; }
    [BindProperty] public string? Create_SpeciesId           { get; set; }
    [BindProperty] public string? Create_BatchDateStr        { get; set; }
    [BindProperty] public string? Create_Fixation            { get; set; }
    [BindProperty] public bool    Create_SafeToHandle        { get; set; }
    [BindProperty] public int?    Create_OtherSubmittedBy    { get; set; }
    [BindProperty] public string? Create_OtherSubmittedArea  { get; set; }
    [BindProperty] public string? Create_Comments            { get; set; }

    // ── Test type selections (checkbox groups) ──
    [BindProperty] public List<string> Create_SelectedHistologyCodes { get; set; } = [];
    [BindProperty] public List<string> Create_SelectedAntibodyCodes  { get; set; } = [];
    [BindProperty] public List<string> Create_SelectedStainCodes     { get; set; } = [];

    // Computed flags mirror EditBatchTests show/hide logic
    public bool Create_ShowAntibodies => Create_SelectedHistologyCodes.Contains(Histo.Submissions.Models.HistologyCode.IhcPrp)
                                      || Create_SelectedHistologyCodes.Contains(Histo.Submissions.Models.HistologyCode.IhcOther);
    public bool Create_ShowStains     => Create_SelectedHistologyCodes.Contains(Histo.Submissions.Models.HistologyCode.SpecialStain);

    // ── Create-mode dropdown lists ──
    public IReadOnlyList<LookupItem> Create_Projects   { get; private set; } = [];
    public IReadOnlyList<LookupItem> Create_Contacts   { get; private set; } = [];
    public IReadOnlyList<LookupItem> Create_SpeciesList { get; private set; } = [];
    public IReadOnlyList<LookupItem> Create_Fixations  { get; private set; } = [];
    public IReadOnlyList<LookupItem> Create_UserAreas  { get; private set; } = [];
    public IReadOnlyList<User>       Create_AllUsers   { get; private set; } = [];
    public string? Create_SubmittedAsName { get; private set; }

    // ── Test type lookup lists ──
    public IReadOnlyList<LookupItem> Create_HistologyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> Create_AntibodyOptions  { get; private set; } = [];
    public IReadOnlyList<LookupItem> Create_StainOptions     { get; private set; } = [];

    public IDictionary<string, string> Errors { get; private set; } = new Dictionary<string, string>();

    public Batch? Batch { get; private set; }

    /// <summary>Number of samples added so far — shown as a hint on the "Samples" button.</summary>
    public int SampleCount { get; private set; }

    /// <summary>Batch-level test type selections (histology, antibodies, special stains).</summary>
    public BatchTestSelections TestSelections { get; private set; } = new();

    /// <summary>
    /// Resolved species name for the batch.
    /// Legacy: tblBatch.Species stores a SpeciesID (integer); GetSpeciesLookup joins to luSpecies to
    /// return ID + Name. We look up the ID here so the view renders "Murine" not "3".
    /// Falls back to the raw value when the ID cannot be resolved (e.g. direct name from a newer SP).
    /// </summary>
    public string? SpeciesName { get; private set; }

    /// <summary>
    /// Resolved project/contract code description.
    /// Legacy: tblBatch stores a raw code in the "ProjectContractCode" column; GetCommonBatchTablesByID
    /// does not join it to a description (unlike the search-result SPs), so it is resolved here via
    /// LOOKUP_PROJECTS (table 19), matching legacy's ddlProjectCode data binding.
    /// Falls back to the raw code when it cannot be resolved.
    /// </summary>
    public string? ProjectName { get; private set; }

    /// <summary>
    /// Resolved pathologist/contact description.
    /// Legacy: tblBatch stores a raw code in the "ContactName" column (despite the name, this is the
    /// numeric Contact ID, not a display name); resolved here via LOOKUP_CONTACTS (table 18), matching
    /// legacy's ddlContactName data binding. Falls back to the raw code when it cannot be resolved.
    /// </summary>
    public string? PathologistName { get; private set; }

    /// <summary>
    /// Resolved fixation/fixative description.
    /// Legacy: tblBatch stores a raw code in the "Fixation" column; GetCommonBatchTablesByID does not
    /// join it to a description (unlike the search-result SPs), so it is resolved here via
    /// LOOKUP_FIXATION (table 10), matching legacy's ddlFixation data binding.
    /// Falls back to the raw code when it cannot be resolved.
    /// </summary>
    public string? FixationName { get; private set; }

    // ── User identity display names — resolved from raw IDs stored in tblBatch ──

    /// <summary>
    /// Display name of the VLA staff member who entered this submission.
    /// Legacy label: "Entered By". Resolved from <see cref="Batch.SubmittedBy"/> via user lookup.
    /// </summary>
    public string? EnteredByName { get; private set; }

    /// <summary>
    /// Display name of the area of the entering VLA staff member.
    /// Legacy label: "Entered Area". Resolved from <see cref="Batch.SubmittedArea"/> via user-area lookup.
    /// </summary>
    public string? EnteredAreaName { get; private set; }

    /// <summary>
    /// Display name of the external customer who submitted this batch.
    /// Legacy label: "Submitted By". Resolved from <see cref="Batch.OtherSubmittedBy"/> via user lookup.
    /// </summary>
    public string? SubmittedByName { get; private set; }

    /// <summary>
    /// Display name of the area of the external submitter.
    /// Legacy label: "Submitted Area". Resolved from <see cref="Batch.OtherSubmittedArea"/> via user-area lookup.
    /// </summary>
    public string? SubmittedAreaName { get; private set; }

    /// <summary>
    /// Human-readable description of the "Submitted As" classification.
    /// Legacy label: "Submitted As". Code read from BATCH_SUBMITTEDAS_TABLE (result-set 5 of
    /// GetCommonBatchTablesByID), then resolved via LOOKUP_SUBMITTEDAS (table 11).
    /// Null when no submitted-as record exists for this batch.
    /// </summary>
    public string? SubmittedAsDescription { get; private set; }

    /// <summary>
    /// Descriptions keyed by Code for each test type — used to translate stored codes to
    /// human-readable names in the summary list.
    /// </summary>
    public IReadOnlyDictionary<string, string> HistologyNames  { get; private set; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> AntibodyNames   { get; private set; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> StainNames      { get; private set; } = new Dictionary<string, string>();

    // ── Status-gated action availability (mirrors legacy EnableDisableControls) ──

    /// <summary>True when blocks can be assigned — Received or InProgress (lab is working on it).</summary>
    public bool CanAssignBlocks => Batch?.Status is BatchStatus.Received or BatchStatus.InProgress;

    /// <summary>True while the submission is still being built — drives the task-list-style progress summary.</summary>
    public bool CanModifySamples => Batch?.Status is BatchStatus.Submitted or BatchStatus.Rejected;

    /// <summary>True when the customer received date (date returned) can be set — Completed only.</summary>
    public bool CanDateReturned => Batch?.Status == BatchStatus.Completed;

    /// <summary>
    /// Mirrors <c>SampleSummaryModel.IsViewMode</c>: true when reached via the View Submission
    /// journey (ViewSubmissions or SearchSubmissions), as opposed to the Create Submission journey.
    /// </summary>
    public bool IsViewMode => Session.IsViewSubmissionMode;

    /// <summary>
    /// Gates the print buttons — a submission has nothing meaningful to print until it has left
    /// the in-progress Create Submission journey (per legacy, printing happens via the "Finish"
    /// step, not mid-build) or is being looked at via the View Submission journey.
    /// </summary>
    public bool CanPrint => IsViewMode || !CanModifySamples;

    /// <summary>
    /// Page path for the back link, populated from <see cref="ISessionService.ReturnPage"/>.
    /// Falls back to <c>/Index</c> if the session value is absent (e.g. direct URL access).
    /// </summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Index"
        : Session.ReturnPage;

    public string? LoadError { get; private set; }

    /// <summary>
    /// TempData slot holding the in-progress create form while the user detours to pick list
    /// management. Restores the legacy behaviour where <c>btnNewProject</c>/<c>btnNewContact</c>
    /// called <c>UpdateSessionWithBatchDetails()</c> before redirecting, so nothing was lost.
    /// </summary>
    private const string CreateDraftKey = "BatchDetails_CreateDraft";

    private sealed record CreateDraft(
        string? ProjectContractCode,
        string? ContactName,
        string? SpeciesId,
        string? BatchDateStr,
        string? Fixation,
        bool SafeToHandle,
        int? OtherSubmittedBy,
        string? OtherSubmittedArea,
        string? Comments,
        List<string> Histology,
        List<string> Antibodies,
        List<string> Stains);

    /// <summary>
    /// Replaces legacy <c>btnNewSubmittedBy</c> / <c>btnNewProject</c> / <c>btnNewContact</c> —
    /// saves the part-completed submission, then opens the maintenance page for that field's
    /// value list with a return link back to this form.
    /// </summary>
    public IActionResult OnPostManagePickList(string field)
    {
        TempData[CreateDraftKey] = System.Text.Json.JsonSerializer.Serialize(new CreateDraft(
            Create_ProjectContractCode,
            Create_ContactName,
            Create_SpeciesId,
            Create_BatchDateStr,
            Create_Fixation,
            Create_SafeToHandle,
            Create_OtherSubmittedBy,
            Create_OtherSubmittedArea,
            Create_Comments,
            Create_SelectedHistologyCodes,
            Create_SelectedAntibodyCodes,
            Create_SelectedStainCodes));

        var returnUrl = Url.Page("/Batches/BatchDetails", new { mode = "create" });

        return field switch
        {
            "submittedBy" => RedirectToPage("/Admin/UserMaintenance", new { returnUrl }),
            "project"     => RedirectToPage("/Admin/PickListUserArea", new { tableId = LookupProjects, returnUrl }),
            "pathologist" => RedirectToPage("/Admin/PickListUserArea", new { tableId = LookupContacts, returnUrl }),
            _             => RedirectToPage("/Batches/BatchDetails", new { mode = "create" }),
        };
    }

    /// <summary>Re-applies a draft saved by <see cref="OnPostManagePickList"/>, if one is pending.</summary>
    private bool RestoreCreateDraft()
    {
        if (TempData[CreateDraftKey] is not string json) return false;

        CreateDraft? draft;
        try { draft = System.Text.Json.JsonSerializer.Deserialize<CreateDraft>(json); }
        catch (System.Text.Json.JsonException) { return false; }
        if (draft is null) return false;

        Create_ProjectContractCode   = draft.ProjectContractCode;
        Create_ContactName           = draft.ContactName;
        Create_SpeciesId             = draft.SpeciesId;
        Create_BatchDateStr          = draft.BatchDateStr;
        Create_Fixation              = draft.Fixation;
        Create_SafeToHandle          = draft.SafeToHandle;
        Create_OtherSubmittedBy      = draft.OtherSubmittedBy;
        Create_OtherSubmittedArea    = draft.OtherSubmittedArea;
        Create_Comments              = draft.Comments;
        Create_SelectedHistologyCodes = draft.Histology ?? [];
        Create_SelectedAntibodyCodes  = draft.Antibodies ?? [];
        Create_SelectedStainCodes     = draft.Stains ?? [];
        return true;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"]     = IsCreateMode ? "New submission" : "Submission details";
        ViewData["PageTitle"] = IsCreateMode ? "New submission" : "Submission details";

        if (IsCreateMode)
        {
            await LoadCreateLookupsAsync();
            if (!RestoreCreateDraft())
                Create_BatchDateStr = DateTime.Today.ToString("yyyy-MM-dd");
            // Resolve SubmittedAs name from TempData for display
            if (TempData.TryGetValue("CreateSubmittedAsId", out var saId))
            {
                var saLookup = await _lookups.GetLookupDataAsync(LookupSubmittedAs);
                Create_SubmittedAsName = saLookup.FirstOrDefault(x => x.ID.ToString() == saId?.ToString())?.Name;
                TempData.Keep("CreateSubmittedAsId");
                TempData.Keep("CreateSubmittedAsCode");
                TempData.Keep("CreateIsPreCassetted");
            }
            return Page();
        }

        if (Session.BatchID is null or <= 0) return RedirectToPage("/Index");
        var effectiveBatchId = BatchId ?? Session.BatchID;
        if (effectiveBatchId is null or <= 0) return RedirectToPage("/Index");

        var forbidden = await CheckBatchAccessAsync(_batches, effectiveBatchId.Value);
        if (forbidden is not null) return forbidden;

        Session.BatchID = effectiveBatchId; // keep session in sync as a fallback for links not yet migrated
        BatchId = effectiveBatchId;
        try
        {
            Batch = await _batches.GetByIdAsync(effectiveBatchId.Value);
        }
        catch (Exception ex)
        {
            LoadError = "Failed to load the submission details. Please go back and try again.";
            return Page();
        }
        if (Batch is not null)
        {
            Session.BatchType = Batch.BatchType;  // ISS-023: restore from DB for downstream lookup selection

            // Load batch-level test selections and translate codes to descriptions.
            var batchId = effectiveBatchId.Value;
            var antibodyTableId = Batch.BatchType == BatchTypeConstants.NonTse ? 5 : 4;

            var selectionsTask     = _batches.GetBatchTestSelectionsAsync(batchId);
            var histologyTask      = _lookups.GetHistologyTypesAsync();
            var antibodyTask       = _lookups.GetLookupDataAsync(antibodyTableId);
            var stainTask          = _lookups.GetLookupDataAsync(6);   // LOOKUP_SPECIAL_STAIN = 6
            var speciesTask        = _lookups.GetSpeciesLookupAsync();
            var usersTask          = _users.GetAllUsersAsync();
            var userAreasTask      = _lookups.GetUserAreasAsync();
            var submittedAsTask    = _batches.GetSubmittedAsCodeAsync(batchId);
            var submittedAsLookup  = _lookups.GetLookupDataAsync(11); // LOOKUP_SUBMITTEDAS = 11
            var projectsLookup     = _lookups.GetLookupDataAsync(LookupProjects);
            var contactsLookup     = _lookups.GetLookupDataAsync(LookupContacts);
            var fixationsLookup    = _lookups.GetLookupDataAsync(LookupFixation);
            var animalsTask        = _submissions.GetAnimalsByBatchAsync(batchId);

            await Task.WhenAll(selectionsTask, histologyTask, antibodyTask, stainTask,
                               speciesTask, usersTask, userAreasTask, submittedAsTask, submittedAsLookup,
                               projectsLookup, contactsLookup, fixationsLookup, animalsTask);

            SampleCount     = animalsTask.Result.Count;

            TestSelections  = selectionsTask.Result;
            HistologyNames  = ToDictionary(histologyTask.Result);
            AntibodyNames   = ToDictionary(antibodyTask.Result);
            StainNames      = ToDictionary(stainTask.Result);

            // Resolve SpeciesID → species name.  Legacy stores an integer SpeciesID in tblBatch.Species.
            var speciesById = speciesTask.Result
                .ToDictionary(s => s.ID.ToString(), s => s.Name, StringComparer.OrdinalIgnoreCase);
            SpeciesName = !string.IsNullOrWhiteSpace(Batch.Species)
                && speciesById.TryGetValue(Batch.Species, out var sn)
                    ? sn
                    : Batch.Species;

            // Resolve Entered By / Submitted By user IDs → names.
            var userById = usersTask.Result.ToDictionary(u => u.UserID, u => u.Name);
            EnteredByName    = Batch.SubmittedBy.HasValue     && userById.TryGetValue(Batch.SubmittedBy.Value,     out var eb) ? eb : null;
            SubmittedByName  = Batch.OtherSubmittedBy.HasValue && userById.TryGetValue(Batch.OtherSubmittedBy.Value, out var sb) ? sb : null;

            // Resolve Entered Area / Submitted Area codes → descriptions.
            // GetUserAreasAsync uses MapCodeDescription: Code column → LookupItem.ID (int), Description → Name.
            // SubmittedArea/OtherSubmittedArea are varchar codes in DB; match them as strings.
            var areaByCode = userAreasTask.Result.ToDictionary(a => a.ID.ToString(), a => a.Name, StringComparer.OrdinalIgnoreCase);
            EnteredAreaName   = !string.IsNullOrEmpty(Batch.SubmittedArea)     && areaByCode.TryGetValue(Batch.SubmittedArea,      out var ea) ? ea : null;
            SubmittedAreaName = !string.IsNullOrEmpty(Batch.OtherSubmittedArea) && areaByCode.TryGetValue(Batch.OtherSubmittedArea, out var sa) ? sa : null;

            // Resolve Submitted As code → description via LOOKUP_SUBMITTEDAS (table 11).
            var code = submittedAsTask.Result;
            if (!string.IsNullOrWhiteSpace(code))
            {
                var saLookup = submittedAsLookup.Result
                    .FirstOrDefault(x => string.Equals(x.Code ?? x.ID.ToString(), code, StringComparison.OrdinalIgnoreCase));
                SubmittedAsDescription = saLookup?.Name ?? code;
            }

            // Resolve ProjectContractCode / ContactName raw codes → descriptions.
            // Both LOOKUP_PROJECTS (19) and LOOKUP_CONTACTS (18) are ID-keyed tables (LookupItem.Code is
            // null for these — see LookupItem.cs), so the raw code is matched against LookupItem.ID.
            var projectsById = projectsLookup.Result
                .ToDictionary(p => p.ID.ToString(), p => p.Name, StringComparer.OrdinalIgnoreCase);
            ProjectName = !string.IsNullOrWhiteSpace(Batch.ProjectContractCode)
                && projectsById.TryGetValue(Batch.ProjectContractCode, out var pn)
                    ? pn
                    : Batch.ProjectContractCode;

            var contactsById = contactsLookup.Result
                .ToDictionary(c => c.ID.ToString(), c => c.Name, StringComparer.OrdinalIgnoreCase);
            PathologistName = !string.IsNullOrWhiteSpace(Batch.ContactName)
                && contactsById.TryGetValue(Batch.ContactName, out var cn)
                    ? cn
                    : Batch.ContactName;

            // Resolve Fixation raw code → description via LOOKUP_FIXATION (table 10).
            // Unlike Projects/Contacts, legacy binds ddlFixation.DataValueField = "Code" (not ID), so the
            // lookup here is Code-keyed — reuse the same Code-preferred/ID-fallback helper as the test-type
            // dictionaries above.
            var fixationsByCode = ToDictionary(fixationsLookup.Result);
            FixationName = !string.IsNullOrWhiteSpace(Batch.Fixation)
                && fixationsByCode.TryGetValue(Batch.Fixation, out var fn)
                    ? fn
                    : Batch.Fixation;
        }
        return Page();
    }

    /// <summary>
    /// Builds a Code→Name dictionary from a lookup item list.
    /// Prefers <see cref="LookupItem.Code"/> as key; falls back to
    /// <see cref="LookupItem.ID"/> as string when Code is absent.
    /// </summary>
    private static IReadOnlyDictionary<string, string> ToDictionary(IReadOnlyList<LookupItem> items)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var key = !string.IsNullOrWhiteSpace(item.Code) ? item.Code : item.ID.ToString();
            dict.TryAdd(key, item.Name);
        }
        return dict;
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        ViewData["Title"]     = "New submission";
        ViewData["PageTitle"] = "New submission";
        await LoadCreateLookupsAsync();

        // Read type-selection values from TempData (set by Cassetted step)
        var submittedAsCode = TempData["CreateSubmittedAsCode"]?.ToString() ?? "";
        var isPreCassetted  = bool.TryParse(TempData["CreateIsPreCassetted"]?.ToString(), out var ipc) && ipc;

        TempData.Keep("CreateSubmittedAsId");
        TempData.Keep("CreateSubmittedAsCode");
        TempData.Keep("CreateIsPreCassetted");

        // Resolve SubmittedAs name for redisplay
        if (TempData.TryGetValue("CreateSubmittedAsId", out var saId))
        {
            var saLookup = await _lookups.GetLookupDataAsync(LookupSubmittedAs);
            Create_SubmittedAsName = saLookup.FirstOrDefault(x => x.ID.ToString() == saId?.ToString())?.Name;
        }

        var errors = new Dictionary<string, string>();

        // Required fields — mirrors legacy ValidateMandatoryFields()
        if (string.IsNullOrWhiteSpace(Create_ProjectContractCode))
            errors["Create_ProjectContractCode"] = "Select a project or contract code.";
        if (string.IsNullOrWhiteSpace(Create_ContactName))
            errors["Create_ContactName"] = "Select a pathologist.";
        if (string.IsNullOrWhiteSpace(Create_SpeciesId))
            errors["Create_SpeciesId"] = "Select a species.";
        if (string.IsNullOrWhiteSpace(Create_BatchDateStr))
            errors["Create_BatchDateStr"] = "Enter the submission date.";
        if (Create_OtherSubmittedBy is null or 0)
            errors["Create_OtherSubmittedBy"] = "Select the submitted by person.";
        if (string.IsNullOrWhiteSpace(Create_OtherSubmittedArea))
            errors["Create_OtherSubmittedArea"] = "Select the submitted area.";

        DateTime? batchDate = null;
        if (!string.IsNullOrWhiteSpace(Create_BatchDateStr))
        {
            if (!DateTime.TryParseExact(Create_BatchDateStr, new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsed))
                errors["Create_BatchDateStr"] = "Enter a valid submission date.";
            else
                batchDate = parsed;
        }

        if (Create_SelectedHistologyCodes.Count == 0)
            errors["Create_Histology"] = "Select at least one histology type.";

        // Antibody required when IHC-PrP or IHC-Other is selected
        var needsAntibodies = Create_SelectedHistologyCodes.Contains(Histo.Submissions.Models.HistologyCode.IhcPrp)
                           || Create_SelectedHistologyCodes.Contains(Histo.Submissions.Models.HistologyCode.IhcOther);
        if (needsAntibodies && Create_SelectedAntibodyCodes.Count == 0)
            errors["Create_Antibodies"] = "Select at least one antibody (required when IHC is selected).";

        // Stain required when Special Stain is selected
        var needsStains = Create_SelectedHistologyCodes.Contains(Histo.Submissions.Models.HistologyCode.SpecialStain);
        if (needsStains && Create_SelectedStainCodes.Count == 0)
            errors["Create_Stains"] = "Select at least one special stain.";

        if (errors.Count > 0)
        {
            Errors = errors;
            Mode = "create";
            return Page();
        }

        var batch = new Batch
        {
            SubmittedByUserID   = Session.UserID,
            UserAreaCode        = Session.UserAreaID,
            IsPreCassetted      = isPreCassetted,
            BatchType           = Session.BatchType,
            ProjectContractCode = Create_ProjectContractCode,
            ContactName         = Create_ContactName,
            Species             = Create_SpeciesId,
            BatchDate           = batchDate ?? DateTime.Today,
            Fixation            = Create_Fixation,
            SafeToHandle        = Create_SafeToHandle,
            OtherSubmittedBy    = Create_OtherSubmittedBy,
            OtherSubmittedArea  = Create_OtherSubmittedArea ?? string.Empty,
            Comments            = Create_Comments,
        };

        int batchId;
        try { batchId = await _batches.AddAsync(batch, Session.UserID); }
        catch
        {
            Errors = new Dictionary<string, string> { ["Create_Save"] = "Failed to create the submission. Please try again." };
            Mode = "create";
            return Page();
        }

        if (batchId <= 0)
        {
            Errors = new Dictionary<string, string> { ["Create_Save"] = "Failed to create the submission. Please try again." };
            Mode = "create";
            return Page();
        }

        Session.BatchID   = batchId;

        if (!string.IsNullOrWhiteSpace(submittedAsCode))
            await _batches.SaveSubmittedAsAsync(batchId, submittedAsCode, Session.UserID);

        // Save test type selections — mirrors clsCheckBoxData.UpdateTable for BATCH_HISTOLOGY/ANTIBODIES/STAIN
        await _batches.SaveBatchTestSelectionsAsync(
            batchId,
            Create_SelectedHistologyCodes,
            needsAntibodies ? Create_SelectedAntibodyCodes : new List<string>(),
            needsStains     ? Create_SelectedStainCodes    : new List<string>(),
            Session.UserID);

        return RedirectToPage("/Batches/BatchDetails");
    }

    private async Task LoadCreateLookupsAsync()
    {
        var projectsTask  = _lookups.GetLookupDataAsync(LookupProjects);
        var contactsTask  = _lookups.GetLookupDataAsync(LookupContacts);
        var speciesTask   = _lookups.GetSpeciesLookupAsync();
        var fixationTask  = _lookups.GetLookupDataAsync(LookupFixation);
        var areaTask      = _lookups.GetLookupDataAsync(LookupUserArea);
        var usersTask     = _users.GetAllUsersAsync();
        var antibodyId    = Session.BatchType == BatchTypeConstants.NonTse ? LookupNonTseAntibodies : LookupTseAntibodies;
        var histologyTask = _lookups.GetHistologyTypesAsync();
        var antibodyTask  = _lookups.GetLookupDataAsync(antibodyId);
        var stainTask     = _lookups.GetLookupDataAsync(LookupSpecialStain);
        await Task.WhenAll(projectsTask, contactsTask, speciesTask, fixationTask, areaTask, usersTask,
                           histologyTask, antibodyTask, stainTask);
        Create_Projects    = projectsTask.Result;
        Create_Contacts    = contactsTask.Result;
        Create_SpeciesList = speciesTask.Result;
        Create_Fixations   = fixationTask.Result;
        Create_UserAreas   = areaTask.Result;
        Create_AllUsers    = [.. usersTask.Result];

        // Filter histology options by batch type — mirrors EditBatchTests.LoadLookupOptionsAsync / BatchDetails.aspx HideOptions()
        Create_HistologyOptions = Session.BatchType == BatchTypeConstants.NonTse
            ? histologyTask.Result.Where(i => i.Code != HistologyCode.IhcPrp && i.Code != HistologyCode.HeBse).ToList()
            : histologyTask.Result.Where(i => i.Code != HistologyCode.IhcOther).ToList();
        Create_AntibodyOptions = antibodyTask.Result;
        Create_StainOptions    = stainTask.Result;
    }
}
