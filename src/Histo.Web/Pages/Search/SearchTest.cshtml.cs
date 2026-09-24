using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>
/// Replaces <c>SearchTest.aspx</c>.
///
/// Restores the legacy histology/antibody/special-stain checkbox-driven premium-charge
/// cross-tab ("Analyse results" / <c>btnCount_Click</c>) and the submissions-by-premium
/// drill-down grid ("Analyse submissions" / <c>bntBatch_Click</c>). The 6 legacy stored
/// procedures behind these (<c>CountHistologysTestItems</c>, <c>CountAntibodesTestItems</c>,
/// <c>CountStainTestItems</c>, <c>CountHistologysTestBatch</c>, <c>CountAntibodesTestBatch</c>,
/// <c>CountStainTestBatch</c>) were confirmed live (2026-09-22) not to exist in this database —
/// the counting logic they implemented is reproduced directly in
/// <see cref="Histo.Submissions.Interfaces.IBatchRepository.GetTestPremiumChargeCountsAsync"/>/
/// <see cref="Histo.Submissions.Interfaces.IBatchRepository.GetTestPremiumChargeBatchesAsync"/>
/// against the underlying <c>BlockHistology</c>/<c>BlockAntibodies</c>/<c>BlockStain</c> and
/// <c>HistologyTCCodes</c>/<c>AntibodiesTCCodes</c>/<c>SpecialStainTCCodes</c> tables instead.
///
/// No date filter is offered, matching the prior scope decision: the legacy StartDate/EndDate
/// calendar controls were never wired to a query parameter here either.
///
/// GET-based (all filters/checkboxes bound via <c>SupportsGet</c>): results live in the query
/// string, so they're restored correctly by the browser Back button and are bookmarkable/
/// shareable — matching the established pattern already used by
/// <see cref="Histo.Web.Pages.Search.SearchSubmissionsModel"/>. Previously this page used a
/// POST form, which loses all of this on Back.
/// </summary>
public class SearchTestModel : HistoPageModel
{
    private const int LookupProjects = 19;         // Common.vb — LOOKUP_PROJECTS
    private const int LookupTseAntibodies = 4;     // LOOKUP_TSE_ANTIBODIES
    private const int LookupNonTseAntibodies = 5;  // LOOKUP_NONTSE_ANTIBODIES
    private const int LookupSpecialStain = 6;      // LOOKUP_SPECIAL_STAIN

    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;

    public SearchTestModel(ISessionService session, IBatchService batches, ILookupService lookups)
        : base(session)
    {
        _batches = batches;
        _lookups = lookups;
    }

    [BindProperty(SupportsGet = true)] public string? ProjectDescription { get; set; }
    [BindProperty(SupportsGet = true)] public int SubmissionType { get; set; }

    /// <summary>Legacy <c>StartDate</c>/<c>EndDate</c> CalendarDate controls — filters dispatched tests by <c>DispatchedDate</c>.</summary>
    [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }

    /// <summary>Selected Histology test codes (legacy <c>chkblHistology</c>).</summary>
    [BindProperty(SupportsGet = true)] public List<string> SelectedHistology { get; set; } = [];

    /// <summary>Selected Antibody test codes (legacy <c>chkblAntibodies</c>) — TSE or NonTSE list depending on <see cref="SubmissionType"/>.</summary>
    [BindProperty(SupportsGet = true)] public List<string> SelectedAntibodies { get; set; } = [];

    /// <summary>Selected Special Stain test codes (legacy <c>chkblSpecialStain</c>).</summary>
    [BindProperty(SupportsGet = true)] public List<string> SelectedSpecialStain { get; set; } = [];

    // Distinguishes "user clicked Analyse results with nothing selected" from a first, bare
    // page visit — both would otherwise look identical (no query string at all).
    [BindProperty(SupportsGet = true)] public bool Submitted { get; set; }

    public IReadOnlyList<LookupItem> Projects { get; private set; } = [];
    public IReadOnlyList<LookupItem> HistologyTests { get; private set; } = [];
    public IReadOnlyList<LookupItem> AntibodyTests { get; private set; } = [];
    public IReadOnlyList<LookupItem> SpecialStainTests { get; private set; } = [];
    public IReadOnlyList<LookupItem> PremiumCharges { get; private set; } = [];

    public IReadOnlyList<ProjectPremiumTotals> CrossTabRows { get; private set; } = [];
    public IReadOnlyDictionary<string, int> CrossTabColumnTotals { get; private set; } = new Dictionary<string, int>();
    public int CrossTabGrandTotal { get; private set; }
    public bool Searched { get; private set; }

    public IReadOnlyList<PremiumSubmissionGroup> SubmissionGroups { get; private set; } = [];
    public bool SubmissionsSearched { get; private set; }

    [BindProperty(SupportsGet = true)] public string? ReturnPage { get; set; }

    /// <summary>Only ever redirect to a path inside this application — blocks open-redirect abuse.</summary>
    public string BackLinkPage =>
        !string.IsNullOrWhiteSpace(ReturnPage) && Url.IsLocalUrl(ReturnPage) ? ReturnPage : "/Search/SearchMenu";

    public async Task OnGetAsync()
    {
        SetTitles();
        await LoadLookupsAsync();

        // A bare first visit (no query string) shows the empty form only.
        if (!Submitted) return;

        var raw = await _batches.GetTestPremiumChargeCountsAsync(
            ProjectDescription, SubmissionType, SelectedHistology, SelectedAntibodies, SelectedSpecialStain, StartDate, EndDate);
        BuildCrossTab(raw);
        Searched = true;
    }

    /// <summary>Legacy <c>bntBatch_Click</c> ("Analyse Submissions").</summary>
    public async Task OnGetAnalyseSubmissionsAsync()
    {
        SetTitles();
        await LoadLookupsAsync();

        var raw = await _batches.GetTestPremiumChargeBatchesAsync(
            ProjectDescription, SubmissionType, SelectedHistology, SelectedAntibodies, SelectedSpecialStain, StartDate, EndDate);
        BuildSubmissionGroups(raw);
        SubmissionsSearched = true;
    }

    /// <summary>Replaces the legacy <c>hlbExcel</c> ("Export Outputs to Excel") link.</summary>
    public async Task<IActionResult> OnGetExportOutputsExcelAsync()
    {
        await LoadLookupsAsync();
        var raw = await _batches.GetTestPremiumChargeCountsAsync(
            ProjectDescription, SubmissionType, SelectedHistology, SelectedAntibodies, SelectedSpecialStain, StartDate, EndDate);
        BuildCrossTab(raw);

        var codes = CrossTabColumnTotals.Keys.ToList();
        var headers = new List<string> { "Description" };
        headers.AddRange(codes);
        headers.Add("Project Totals");

        var rows = CrossTabRows.Select(r =>
        {
            var cells = new List<object?> { r.ProjectDescription };
            cells.AddRange(codes.Select(c => (object?)r.CountsByPremiumCode.GetValueOrDefault(c)));
            cells.Add(r.Total);
            return (IReadOnlyList<object?>)cells;
        });

        return ExcelExportHelper.BuildXlsx("search-test-outputs.xlsx", headers, rows);
    }

    /// <summary>Replaces the legacy <c>hlbBatchExcel</c> ("Export Submissions to Excel") link.</summary>
    public async Task<IActionResult> OnGetExportSubmissionsExcelAsync()
    {
        await LoadLookupsAsync();
        var raw = await _batches.GetTestPremiumChargeBatchesAsync(
            ProjectDescription, SubmissionType, SelectedHistology, SelectedAntibodies, SelectedSpecialStain, StartDate, EndDate);
        BuildSubmissionGroups(raw);

        return ExcelExportHelper.BuildXlsx(
            "search-test-submissions.xlsx",
            ["Premium charge", "Submission numbers"],
            SubmissionGroups.Select(g => (IReadOnlyList<object?>)new object?[]
            {
                g.PremiumDescription,
                string.Join(", ", g.BatchIds),
            }));
    }

    private void SetTitles()
    {
        ViewData["Title"] = "Search Test Totals";
        ViewData["PageTitle"] = "Search Test Totals";
    }

    private async Task LoadLookupsAsync()
    {
        Projects = await _lookups.GetLookupDataAsync(LookupProjects);

        var histology = await _lookups.GetHistologyTypesAsync();
        // Legacy HideOptions(): NonTSE hides IHC-PrP(4)/H&E(BSE)(5); TSE hides IHC-Other(6).
        HistologyTests = SubmissionType == BatchTypeConstants.NonTse
            ? histology.Where(h => h.Code != "4" && h.Code != "5").ToList()
            : histology.Where(h => h.Code != "6").ToList();

        var antibodyTableId = SubmissionType == BatchTypeConstants.NonTse ? LookupNonTseAntibodies : LookupTseAntibodies;
        AntibodyTests = await _lookups.GetLookupDataAsync(antibodyTableId);

        SpecialStainTests = await _lookups.GetLookupDataAsync(LookupSpecialStain);
        PremiumCharges = await _lookups.GetPremiumChargesAsync();
    }

    private void BuildCrossTab(IReadOnlyList<TestPremiumChargeCount> raw)
    {
        var codes = PremiumCharges
            .Select(p => p.Code)
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var columnTotals = codes.ToDictionary(c => c, _ => 0, StringComparer.OrdinalIgnoreCase);
        var rows = new List<ProjectPremiumTotals>();

        foreach (var g in raw.GroupBy(r => r.ProjectDescription ?? "").OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var counts = codes.ToDictionary(c => c, _ => 0, StringComparer.OrdinalIgnoreCase);
            foreach (var r in g)
            {
                if (r.PremiumCode is not null && counts.ContainsKey(r.PremiumCode))
                    counts[r.PremiumCode] = r.Count;
            }

            foreach (var kv in counts) columnTotals[kv.Key] += kv.Value;
            rows.Add(new ProjectPremiumTotals
            {
                ProjectDescription = g.Key,
                CountsByPremiumCode = counts,
                Total = counts.Values.Sum(),
            });
        }

        CrossTabRows = rows;
        CrossTabColumnTotals = columnTotals;
        CrossTabGrandTotal = columnTotals.Values.Sum();
    }

    private void BuildSubmissionGroups(IReadOnlyList<TestPremiumChargeBatchRef> raw)
    {
        var descByCode = PremiumCharges
            .Where(p => !string.IsNullOrEmpty(p.Code))
            .ToDictionary(p => p.Code!, p => p.Name, StringComparer.OrdinalIgnoreCase);

        SubmissionGroups = raw
            .GroupBy(r => r.PremiumCode ?? "", StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PremiumSubmissionGroup
            {
                PremiumCode = g.Key,
                PremiumDescription = descByCode.TryGetValue(g.Key, out var d) ? d : g.Key,
                BatchIds = g.Select(r => r.BatchID).Distinct().OrderBy(id => id).ToList(),
            })
            .ToList();
    }
}

/// <summary>One project's dispatched-test counts pivoted across premium/TC charge codes, for the "Analyse results" grid.</summary>
public sealed class ProjectPremiumTotals
{
    public string ProjectDescription { get; init; } = "";
    public IReadOnlyDictionary<string, int> CountsByPremiumCode { get; init; } = new Dictionary<string, int>();
    public int Total { get; init; }
}

/// <summary>Distinct submission numbers grouped by premium/TC charge code, for the "Analyse submissions" grid.</summary>
public sealed class PremiumSubmissionGroup
{
    public string PremiumCode { get; init; } = "";
    public string PremiumDescription { get; init; } = "";
    public IReadOnlyList<int> BatchIds { get; init; } = [];
}

