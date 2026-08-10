using Histo.Administration.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>
/// Replaces <c>SearchTest.aspx</c>.
///
/// SIMPLIFIED: the legacy page builds a histology/antibody/special-stain
/// checkbox-driven premium-charge cross-tab (<c>CountHistologysTestItems</c>,
/// <c>CountAntibodesTestItems</c>, <c>CountStainTestItems</c>) plus a
/// submissions-by-premium breakdown grid, and it reads the TSE/Non-TSE
/// submission type from a session flag set by the legacy Search Outputs menu.
/// This page reproduces the reduced-scope replacement already defined by
/// <see cref="Histo.Submissions.Interfaces.IBatchRepository.GetTestItemRowsAsync"/> —
/// a project/date-range/submission-type test-item count listing — with the
/// submission type selected directly on the page instead of via a session flag.
/// See the search module report for details of what analytics were not ported.
/// </summary>
public class SearchTestModel : HistoPageModel
{
    private const int LookupProjects = 19; // Legacy source: HistopathologySystem/Common.vb — LOOKUP_PROJECTS

    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;

    public SearchTestModel(ISessionService session, IBatchService batches, ILookupService lookups)
        : base(session)
    {
        _batches = batches;
        _lookups = lookups;
    }

    [BindProperty] public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);
    [BindProperty] public DateTime EndDate { get; set; } = DateTime.Today;
    [BindProperty] public string? ProjectDescription { get; set; }
    [BindProperty] public int SubmissionType { get; set; }

    public IReadOnlyList<Administration.Models.LookupItem> Projects { get; private set; } = [];
    public IReadOnlyList<TestItemRow> Results { get; private set; } = [];
    public bool Searched { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Search Test Totals";
        ViewData["PageTitle"] = "Search Test Totals";
        Projects = await _lookups.GetLookupDataAsync(LookupProjects);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Search Test Totals";
        ViewData["PageTitle"] = "Search Test Totals";
        Projects = await _lookups.GetLookupDataAsync(LookupProjects);

        Results = await _batches.GetTestItemRowsAsync(ProjectDescription, SubmissionType);
        Searched = true;

        return Page();
    }
}
