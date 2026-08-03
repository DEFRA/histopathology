using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>Replaces <c>SearchPMDates.aspx</c>.</summary>
public class SearchPMDatesModel : HistoPageModel
{
    private readonly SubmissionService _submissions;

    public SearchPMDatesModel(ISessionService session, SubmissionService submissions)
        : base(session) => _submissions = submissions;

    [BindProperty] public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);
    [BindProperty] public DateTime EndDate   { get; set; } = DateTime.Today;

    public IReadOnlyList<PmDateSearchResult> Results { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Search PM Dates";
        ViewData["PageTitle"] = "Search by PM Date";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Search PM Dates";
        ViewData["PageTitle"] = "Search by PM Date";
        Results = await _submissions.GetByPmDateRangeAsync(StartDate, EndDate);
        return Page();
    }
}
