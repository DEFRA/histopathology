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
/// a project/submission-type test-item count listing — with the
/// submission type selected directly on the page instead of via a session flag.
///
/// No date filter is offered: the <c>GetTestRows</c> stored procedure accepts only
/// <c>@ProjectContractDesc</c> and <c>@BatchType</c>. Date inputs were previously
/// rendered here but never reached the query, so they were removed rather than left
/// as controls that silently did nothing. Restoring date filtering requires adding
/// date parameters to the stored procedure.
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

    /// <summary>Replaces the legacy <c>hlbExcel</c> link.</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var results = await _batches.GetTestItemRowsAsync(ProjectDescription, SubmissionType);
        return CsvExportHelper.BuildCsv(
            "search-test-totals.csv",
            ["Description", "Count"],
            results.Select(r => (IReadOnlyList<string?>)new string?[] { r.Description, r.Count.ToString() }));
    }
}
