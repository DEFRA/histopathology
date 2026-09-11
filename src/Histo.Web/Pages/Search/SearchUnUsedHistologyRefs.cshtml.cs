using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>Replaces <c>SearchUnUsedHistologyRefs.aspx</c>.</summary>
public class SearchUnUsedHistologyRefsModel : GridPageModel
{
    private readonly IHistologyRefService _histologyRefs;

    public SearchUnUsedHistologyRefsModel(ISessionService session, IHistologyRefService histologyRefs)
        : base(session) => _histologyRefs = histologyRefs;

    public IReadOnlyList<HistologyRef> Results { get; private set; } = [];

    public IReadOnlyList<HistologyRef> PagedResults =>
        (SortColumn switch
        {
            //"SenderRef" => SortDesc ? Results.OrderByDescending(r => r.SenderRef) : Results.OrderBy(r => r.SenderRef),
            _           => SortDesc ? Results.OrderByDescending(r => r.Ref)       : Results.OrderBy(r => r.Ref),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Unused Histology Refs";
        ViewData["PageTitle"] = "Unused Histology Refs";
        Results = await _histologyRefs.GetAllUnusedRefsAsync();
        PopulateGridViewData(Results.Count);
    }

    /// <summary>Replaces the legacy <c>hlExcelExport</c> link. Exports every row, not just the current page.</summary>
    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        //var results = await _histologyRefs.GetAllUnusedRefsAsync();
        //return CsvExportHelper.BuildCsv(
        //    "unused-histology-refs.csv",
        //    ["Histology ref", "Sender ref"],
        //    results.Select(r => (IReadOnlyList<string?>)new string?[] { r.Ref, r.SenderRef }));

        var results = await _histologyRefs.GetAllUnusedRefsAsync();
        return CsvExportHelper.BuildCsv(
            "unused-histology-refs.csv",
            ["Histology ref"],
            results.Select(r => (IReadOnlyList<string?>)new string?[] { r.Ref}));
    }
}
