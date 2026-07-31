using Histo.Administration.Models;
using Histo.Administration.Services;
using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>
/// Replaces <c>ViewImportedData.aspx</c> ("View Old ICC_Sub data") — a standalone,
/// read-only browser for legacy imported submission data. Unrelated to Crystal
/// Reports; reads <c>LookupData.GetImportedtables</c> for the table drop-down and
/// <c>clsAnimal.GetImportedData</c> for the selected table's rows.
/// </summary>
public class ViewImportedDataModel : HistoPageModel
{
    private readonly LookupService _lookups;
    private readonly SubmissionService _submissions;

    public ViewImportedDataModel(ISessionService session, LookupService lookups, SubmissionService submissions)
        : base(session)
    {
        _lookups = lookups;
        _submissions = submissions;
    }

    [BindProperty(SupportsGet = true)] public string? SelectedTable { get; set; }
    [BindProperty(SupportsGet = true)] public string? Filter { get; set; }

    public IReadOnlyList<LookupItem> Tables { get; private set; } = [];
    public IReadOnlyList<ImportedDataRow> Results { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "View Old ICC_Sub Data";
        ViewData["PageTitle"] = "View Old ICC_Sub Data";

        Tables = await _lookups.GetImportedTablesAsync();

        if (!string.IsNullOrEmpty(SelectedTable))
        {
            var rows = await _submissions.GetImportedDataAsync(SelectedTable);
            Results = ApplyFilter(rows, Filter);
        }
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as CSV.</summary>
    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        var rows = ApplyFilter(await _submissions.GetImportedDataAsync(SelectedTable), Filter);
        return CsvExportHelper.BuildCsv(
            "ImportedData.csv",
            ["Sender ref", "Histology ref", "Block ref", "Project", "Date submitted", "Species", "Tissue", "Comments"],
            rows.Select(r => (IReadOnlyList<string?>)new string?[]
            {
                r.SenderRef, r.HistologyRef, r.BlockRef, r.Project, r.DateSubmitted?.ToShortDateString(), r.Species, r.Tissue, r.Comments
            }));
    }

    // Mirrors the legacy CreateFilterString/SplitQuoted behaviour (ViewImportedData.aspx.vb):
    // each whitespace/quote-separated term must match at least one of the same eight
    // columns, and all terms must match (AND of ORs). Applied as an in-memory LINQ
    // predicate here rather than a SQL RowFilter string.
    private static IReadOnlyList<ImportedDataRow> ApplyFilter(IReadOnlyList<ImportedDataRow> rows, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return rows;

        var terms = SplitTerms(filter);
        return terms.Count == 0 ? rows : rows.Where(r => terms.All(t => RowMatches(r, t))).ToList();
    }

    private static bool RowMatches(ImportedDataRow r, string term) =>
        Contains(r.Project, term) || Contains(r.DateSubmitted?.ToShortDateString(), term) ||
        Contains(r.Species, term) || Contains(r.Tissue, term) || Contains(r.SenderRef, term) ||
        Contains(r.HistologyRef, term) || Contains(r.BlockRef, term) || Contains(r.Comments, term);

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> SplitTerms(string filter) =>
        System.Text.RegularExpressions.Regex.Matches(filter, "\"([^\"]*)\"|(\\S+)")
            .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
}
