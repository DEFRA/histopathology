using Histo.Administration.Models;
using Histo.Administration.Services;
using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>
/// Submission search page — replaces <c>ViewSubmissions.aspx</c>.
/// Provides the full multi-field batch search with dropdown-populated filters
/// matching the legacy page (Project, Pathologist, Species, Fixation dropdowns
/// from lookup SPs; Users dropdown from UserService; Clear Search link).
/// </summary>
public class ViewSubmissionsModel : HistoPageModel
{
    private readonly BatchService   _batches;
    private readonly UserService    _users;
    private readonly LookupService  _lookups;

    // Constants matching Common.vb
    private const int LookupFixative = 10;
    private const int LookupContacts = 18;
    private const int LookupProjects = 19;

    public ViewSubmissionsModel(ISessionService session, BatchService batches, UserService users, LookupService lookups)
        : base(session)
    {
        _batches = batches;
        _users   = users;
        _lookups = lookups;
    }

    [BindProperty] public int?      SubmissionNumber    { get; set; }
    [BindProperty] public string?   Status              { get; set; }
    [BindProperty] public string?   ProjectContractCode { get; set; }
    [BindProperty] public string?   ContactName         { get; set; }
    [BindProperty] public string?   Species             { get; set; }
    [BindProperty] public string?   Fixation            { get; set; }
    [BindProperty] public int?      SubmittedBy         { get; set; }
    [BindProperty] public int?      EnteredBy           { get; set; }
    [BindProperty] public string?   HistologyRef        { get; set; }
    [BindProperty] public string?   SenderRef           { get; set; }
    [BindProperty] public DateTime? SubmittedDateFrom   { get; set; }
    [BindProperty] public DateTime? SubmittedDateTo     { get; set; }
    [BindProperty] public DateTime? ReceivedDateFrom    { get; set; }
    [BindProperty] public DateTime? ReceivedDateTo      { get; set; }

    public IReadOnlyList<User>             Users       { get; private set; } = [];
    public IReadOnlyList<LookupItem>       Projects    { get; private set; } = [];
    public IReadOnlyList<LookupItem>       Contacts    { get; private set; } = [];
    public IReadOnlyList<LookupItem>       SpeciesList { get; private set; } = [];
    public IReadOnlyList<LookupItem>       Fixations   { get; private set; } = [];
    public IReadOnlyList<BatchSearchResult> Results    { get; private set; } = [];
    public bool Searched { get; private set; }

    private async Task LoadLookupsAsync()
    {
        Users       = await _users.GetAllUsersAsync();
        Projects    = await _lookups.GetLookupDataAsync(LookupProjects);
        Contacts    = await _lookups.GetLookupDataAsync(LookupContacts);
        SpeciesList = await _lookups.GetSpeciesLookupAsync();
        Fixations   = await _lookups.GetLookupDataAsync(LookupFixative);
    }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "View submissions";
        ViewData["PageTitle"] = "View submissions";
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        ViewData["Title"] = "View submissions";
        ViewData["PageTitle"] = "View submissions";
        await LoadLookupsAsync();

        var criteria = new BatchSearchCriteria
        {
            SubmissionNumber    = SubmissionNumber,
            Status              = Status,
            ProjectContractCode = ProjectContractCode,
            ContactName         = ContactName,
            Species             = Species,
            Fixation            = Fixation,
            SubmittedBy         = SubmittedBy,
            EnteredBy           = EnteredBy,
            HistologyRef        = HistologyRef,
            SenderRef           = SenderRef,
            SubmittedDateFrom   = SubmittedDateFrom,
            SubmittedDateTo     = SubmittedDateTo,
            ReceivedDateFrom    = ReceivedDateFrom,
            ReceivedDateTo      = ReceivedDateTo,
        };

        Results  = await _batches.SearchAsync(criteria);
        Searched = true;
        return Page();
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/BatchDetails");
    }
}
