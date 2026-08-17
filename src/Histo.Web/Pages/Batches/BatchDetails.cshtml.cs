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
    private const int LookupContacts = 18;  // Legacy source: HistopathologySystem/Common.vb — LOOKUP_CONTACTS
    private const int LookupProjects = 19;  // Legacy source: HistopathologySystem/Common.vb — LOOKUP_PROJECTS
    private const int LookupFixation = 10;  // Legacy source: HistopathologySystem/Common.vb — LOOKUP_FIXATION

    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;
    private readonly IUserService _users;

    public BatchDetailsModel(ISessionService session, IBatchService batches, ILookupService lookups, IUserService users)
        : base(session)
    {
        _batches = batches;
        _lookups = lookups;
        _users   = users;
    }

    public Batch? Batch { get; private set; }

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

    /// <summary>True when the customer received date (date returned) can be set — Completed only.</summary>
    public bool CanDateReturned => Batch?.Status == BatchStatus.Completed;

    /// <summary>
    /// Page path for the back link, populated from <see cref="ISessionService.ReturnPage"/>.
    /// Falls back to <c>/Index</c> if the session value is absent (e.g. direct URL access).
    /// </summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Index"
        : Session.ReturnPage;

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Submission details";
        ViewData["PageTitle"] = "Submission details";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");
        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch is not null)
        {
            Session.BatchType = Batch.BatchType;  // ISS-023: restore from DB for downstream lookup selection

            // Load batch-level test selections and translate codes to descriptions.
            var batchId = Session.BatchID ?? 0;
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

            await Task.WhenAll(selectionsTask, histologyTask, antibodyTask, stainTask,
                               speciesTask, usersTask, userAreasTask, submittedAsTask, submittedAsLookup,
                               projectsLookup, contactsLookup, fixationsLookup);

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
            var areaById = userAreasTask.Result.ToDictionary(a => a.ID, a => a.Name);
            EnteredAreaName   = Batch.SubmittedArea.HasValue     && areaById.TryGetValue(Batch.SubmittedArea.Value,     out var ea) ? ea : null;
            SubmittedAreaName = Batch.OtherSubmittedArea.HasValue && areaById.TryGetValue(Batch.OtherSubmittedArea.Value, out var sa) ? sa : null;

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
}
