using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>Replaces <c>SearchBlockRefs.aspx</c>.</summary>
public class SearchBlockRefsModel : GridPageModel
{
    private readonly IBlockService _blocks;

    public SearchBlockRefsModel(ISessionService session, IBlockService blocks)
        : base(session) => _blocks = blocks;

    // SupportsGet so the legacy deep-link form still works — SearchBlockRefs.aspx's Page_Load read
    // Request.QueryString("SenderRef"/"HistologyRef"), pre-filled the boxes and auto-ran the search.
    // It also keeps the criteria in the query string, so the GET sort/paging links preserve them.
    [BindProperty(SupportsGet = true)] public string? SenderRef { get; set; }
    [BindProperty(SupportsGet = true)] public string? HistologyRef { get; set; }

    public Dictionary<string, string> Errors { get; } = [];
    public IReadOnlyList<BlockRefRangeHelpers.BlockRefRangeRow> Results { get; private set; } = [];
    public bool Searched { get; private set; }

    public int TotalCount => Results.Count;

    public IReadOnlyList<BlockRefRangeHelpers.BlockRefRangeRow> PagedResults =>
        (SortColumn switch
        {
            "UnusedBlockRefs"    => SortDesc ? Results.OrderByDescending(r => r.UnusedBlockRefs)    : Results.OrderBy(r => r.UnusedBlockRefs),
            "PreBookedBlockRefs" => SortDesc ? Results.OrderByDescending(r => r.PreBookedBlockRefs) : Results.OrderBy(r => r.PreBookedBlockRefs),
            _                    => SortDesc ? Results.OrderByDescending(r => r.UsedBlockRefs)      : Results.OrderBy(r => r.UsedBlockRefs),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Search block refs";
        ViewData["PageTitle"] = "Search block refs";

        var hasSenderRef = !string.IsNullOrWhiteSpace(SenderRef);
        var hasHistologyRef = !string.IsNullOrWhiteSpace(HistologyRef);

        // Nothing supplied yet — first visit, so show the empty form without an error.
        if (!hasSenderRef && !hasHistologyRef)
        {
            PopulateGridViewData(TotalCount);
            return;
        }

        if (hasSenderRef && hasHistologyRef)
        {
            Errors[nameof(SenderRef)] = "Enter either the Sender Ref or the Histology Ref, not both.";
            PopulateGridViewData(TotalCount);
            return;
        }

        Results = await SearchAsync(hasHistologyRef);
        Searched = true;
        PopulateGridViewData(TotalCount);
    }

    /// <summary>Replaces the legacy <c>hlExcelExport</c> link. Exports every range row, not just the current page.</summary>
    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        var hasHistologyRef = !string.IsNullOrWhiteSpace(HistologyRef);
        if (!hasHistologyRef && string.IsNullOrWhiteSpace(SenderRef))
            return RedirectToPage("/Search/SearchBlockRefs");

        var results = await SearchAsync(hasHistologyRef);
        return CsvExportHelper.BuildCsv(
            "search-block-refs.csv",
            ["Used block refs", "Unused block refs", "Pre booked block refs"],
            results.Select(r => (IReadOnlyList<string?>)new string?[]
            {
                r.UsedBlockRefs,
                r.UnusedBlockRefs,
                r.PreBookedBlockRefs
            }));
    }

    private async Task<IReadOnlyList<BlockRefRangeHelpers.BlockRefRangeRow>> SearchAsync(bool byHistologyRef)
    {
        var usedBlocks = byHistologyRef
            ? await _blocks.GetUsedBlockRefsByHistologyRefAsync(HistologyRef!)
            : await _blocks.GetUsedBlockRefsBySenderRefAsync(SenderRef!);

        return BlockRefRangeHelpers.ComputeRanges(
            usedBlocks.Select(b => (b.BlockRef, b.Status)).ToList());
    }
}
