using Histo.Administration.Interfaces;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>
/// Replaces <c>SearchSubmissions.aspx</c>.
///
/// SIMPLIFIED: the legacy page drives several of its filters (Status,
/// Submitted Area, Submitted By, Project, Pathologist, Species, Fixation)
/// from lookup-populated drop-downs and hides a "SubmittedBy" grid column.
/// Status is rendered from the fixed <see cref="BatchStatus"/> constants,
/// Submitted/Entered By are populated from <see cref="UserService.GetAllUsersAsync"/>,
/// and the remaining filters are plain text fields matching the string
/// criteria already accepted by <see cref="BatchSearchCriteria"/>.
/// </summary>
public class SearchSubmissionsModel : HistoPageModel
{
    private readonly IBatchService _batches;
    private readonly IUserService _users;

    public SearchSubmissionsModel(ISessionService session, IBatchService batches, IUserService users)
        : base(session)
    {
        _batches = batches;
        _users = users;
    }

    [BindProperty] public int? SubmissionNumber { get; set; }
    [BindProperty] public string? Status { get; set; }
    [BindProperty] public string? ProjectContractCode { get; set; }
    [BindProperty] public string? ContactName { get; set; }
    [BindProperty] public string? Species { get; set; }
    [BindProperty] public string? Fixation { get; set; }
    [BindProperty] public string? SubmittedArea { get; set; }
    [BindProperty] public int? SubmittedBy { get; set; }
    [BindProperty] public int? EnteredBy { get; set; }
    [BindProperty] public string? HistologyRef { get; set; }
    [BindProperty] public string? SenderRef { get; set; }
    [BindProperty] public DateTime? SubmittedDateFrom { get; set; }
    [BindProperty] public DateTime? SubmittedDateTo { get; set; }
    [BindProperty] public DateTime? ReceivedDateFrom { get; set; }
    [BindProperty] public DateTime? ReceivedDateTo { get; set; }

    public IReadOnlyList<Administration.Models.User> Users { get; private set; } = [];
    public IReadOnlyList<BatchSearchResult> Results { get; private set; } = [];
    public bool Searched { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Search Submissions";
        ViewData["PageTitle"] = "Search Submissions";
        Users = await _users.GetAllUsersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Search Submissions";
        ViewData["PageTitle"] = "Search Submissions";
        Users = await _users.GetAllUsersAsync();

        var criteria = new BatchSearchCriteria
        {
            SubmissionNumber = SubmissionNumber,
            Status = Status,
            ProjectContractCode = ProjectContractCode,
            ContactName = ContactName,
            Species = Species,
            Fixation = Fixation,
            SubmittedArea = SubmittedArea,
            SubmittedBy = SubmittedBy,
            EnteredBy = EnteredBy,
            HistologyRef = HistologyRef,
            SenderRef = SenderRef,
            SubmittedDateFrom = SubmittedDateFrom,
            SubmittedDateTo = SubmittedDateTo,
            ReceivedDateFrom = ReceivedDateFrom,
            ReceivedDateTo = ReceivedDateTo,
        };

        Results = await _batches.SearchAsync(criteria);
        Searched = true;

        return Page();
    }
}
