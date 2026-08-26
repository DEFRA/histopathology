using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces <c>Cassetted.aspx</c> + legacy <c>BatchDetails.aspx</c> new-batch mode.
/// Collects all required batch header fields in one GDS form and creates the new batch via <c>AddBatch</c> SP.
/// </summary>
public class CassettedModel : HistoPageModel
{
    private const int LookupSubmittedAs = 11; // Common.vb LOOKUP_SUBMITTEDAS
    private const int LookupContacts    = 18; // Common.vb LOOKUP_CONTACTS
    private const int LookupProjects    = 19; // Common.vb LOOKUP_PROJECTS
    private const int LookupFixation    = 10; // Common.vb LOOKUP_FIXATIVE
    private const int LookupUserArea    = 13; // Common.vb LOOKUP_USER_AREA

    private readonly ILookupService _lookups;
    private readonly IBatchService  _batches;
    private readonly IUserService   _users;

    public CassettedModel(ISessionService session, ILookupService lookups, IBatchService batches, IUserService users)
        : base(session)
    {
        _lookups = lookups;
        _batches = batches;
        _users   = users;
    }

    // ---- Required fields (map directly to AddBatch SP NOT NULL params) ----
    [BindProperty] public int? SubmittedAs          { get; set; }
    [BindProperty] public int  BatchType            { get; set; } = BatchTypeConstants.Tse;
    [BindProperty] public string? ProjectContractCode { get; set; }
    [BindProperty] public string? ContactName         { get; set; }
    [BindProperty] public string? SpeciesId           { get; set; }
    [BindProperty] public string? BatchDateStr        { get; set; }

    // ---- Optional fields ----
    [BindProperty] public string? Fixation            { get; set; }
    [BindProperty] public bool    SafeToHandle        { get; set; }
    [BindProperty] public int?    OtherSubmittedBy    { get; set; }
    [BindProperty] public string? OtherSubmittedArea  { get; set; }
    [BindProperty] public string? Comments            { get; set; }

    // ---- Lookup data for dropdowns ----
    public IReadOnlyList<LookupItem> SubmittedAsOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> Projects           { get; private set; } = [];
    public IReadOnlyList<LookupItem> Contacts           { get; private set; } = [];
    public IReadOnlyList<LookupItem> SpeciesList        { get; private set; } = [];
    public IReadOnlyList<LookupItem> Fixations          { get; private set; } = [];
    public IReadOnlyList<LookupItem> UserAreas          { get; private set; } = [];
    public IReadOnlyList<User>       AllUsers           { get; private set; } = [];

    public IDictionary<string, string> Errors { get; private set; } = new Dictionary<string, string>();

    public async Task OnGetAsync()
    {
        ViewData["Title"]     = "New submission";
        ViewData["PageTitle"] = "New submission";
        BatchDateStr = DateTime.Today.ToString("dd/MM/yyyy");
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"]     = "New submission";
        ViewData["PageTitle"] = "New submission";
        await LoadLookupsAsync();

        // ---- Validate required fields ----
        var errors = new Dictionary<string, string>();
        var selected = SubmittedAsOptions.FirstOrDefault(o => o.ID == SubmittedAs);
        if (selected is null)
            errors["SubmittedAs"] = "Select a submission type.";
        if (string.IsNullOrWhiteSpace(ProjectContractCode))
            errors["ProjectContractCode"] = "Select a project / contract code.";
        if (string.IsNullOrWhiteSpace(ContactName))
            errors["ContactName"] = "Select a pathologist.";
        if (string.IsNullOrWhiteSpace(SpeciesId))
            errors["SpeciesId"] = "Select a species.";
        if (string.IsNullOrWhiteSpace(BatchDateStr))
            errors["BatchDateStr"] = "Enter the submission date.";

        DateTime? batchDate = null;
        if (!string.IsNullOrWhiteSpace(BatchDateStr) &&
            !DateTime.TryParseExact(BatchDateStr, ["dd/MM/yyyy", "d/M/yyyy"],
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsedDate))
        {
            errors["BatchDateStr"] = "Enter a valid date in DD/MM/YYYY format.";
        }
        else if (!string.IsNullOrWhiteSpace(BatchDateStr))
        {
            DateTime.TryParseExact(BatchDateStr, ["dd/MM/yyyy", "d/M/yyyy"],
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out parsedDate);
            batchDate = parsedDate == default ? null : parsedDate;
        }

        if (errors.Count > 0)
        {
            Errors = errors;
            return Page();
        }

        var batch = new Batch
        {
            SubmittedByUserID   = Session.UserID,
            UserAreaCode        = Session.UserAreaID,
            IsPreCassetted      = ValidationHelpers.IsBatchPreCassetted(selected!.ID.ToString()),
            BatchType           = BatchType,
            ProjectContractCode = ProjectContractCode,
            ContactName         = ContactName,
            Species             = SpeciesId,
            BatchDate           = batchDate ?? DateTime.Today,
            Fixation            = Fixation,
            SafeToHandle        = SafeToHandle,
            OtherSubmittedBy    = OtherSubmittedBy,
            OtherSubmittedArea  = OtherSubmittedArea ?? "",
            Comments            = Comments,
        };

        var batchId = 0;
        try
        {
            batchId = await _batches.AddAsync(batch, Session.UserID);
        }
        catch (Exception ex)
        {
            Errors = new Dictionary<string, string> { [""] = "Failed to create the submission. Please try again." };
            return Page();
        }

        if (batchId <= 0)
        {
            Errors = new Dictionary<string, string> { [""] = "Failed to create the new submission. Please try again." };
            return Page();
        }

        Session.BatchID   = batchId;
        Session.BatchType = BatchType;

        // Persist the SubmittedAs selection via AddBatchSubmission-style update
        await _batches.SaveSubmittedAsAsync(batchId, selected!.Code ?? selected.ID.ToString(), Session.UserID);

        return RedirectToPage("/Batches/BatchDetails");
    }

    private async Task LoadLookupsAsync()
    {
        var submittedAsTask = _lookups.GetLookupDataAsync(LookupSubmittedAs);
        var projectsTask    = _lookups.GetLookupDataAsync(LookupProjects);
        var contactsTask    = _lookups.GetLookupDataAsync(LookupContacts);
        var speciesTask     = _lookups.GetSpeciesLookupAsync();
        var fixationTask    = _lookups.GetLookupDataAsync(LookupFixation);
        var areaTask        = _lookups.GetLookupDataAsync(LookupUserArea);
        var usersTask       = _users.GetAllUsersAsync();
        await Task.WhenAll(submittedAsTask, projectsTask, contactsTask, speciesTask, fixationTask, areaTask, usersTask);
        SubmittedAsOptions = submittedAsTask.Result;
        Projects           = projectsTask.Result;
        Contacts           = contactsTask.Result;
        SpeciesList        = speciesTask.Result;
        Fixations          = fixationTask.Result;
        UserAreas          = areaTask.Result;
        AllUsers           = [.. usersTask.Result];
    }
}
