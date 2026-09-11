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

    // Distinguishes "user clicked Search with nothing filled in" from a first, bare page visit —
    // both look identical otherwise, since a GET form with empty fields submits no query string.
    [BindProperty(SupportsGet = true)] public bool Submitted { get; set; }

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

        // Nothing supplied yet. On a genuine Search click, show the validation error; on a bare
        // first visit (no Submitted marker), just show the empty form without one.
        if (!hasSenderRef && !hasHistologyRef)
        {
            if (Submitted)
                Errors[nameof(SenderRef)] = "Enter the Sender Ref or the Histology Ref.";
            PopulateGridViewData(TotalCount);
            return;
        }

        // GetBlocksForHistoRef/GetBlocksForSenderRef tolerate both being supplied (Sender ref
        // takes precedence) — only reject when NEITHER is given, there's nothing to search on.

        // Default to Used block refs descending until the user explicitly picks a column.
        if (string.IsNullOrEmpty(SortColumn))
        {
            SortColumn = "UsedBlockRefs";
            SortDesc = true;
        }

        Results = await SearchAsync(byHistologyRef: hasHistologyRef && !hasSenderRef);
        Searched = true;
        PopulateGridViewData(TotalCount);
    }

    /// <summary>Replaces the legacy <c>hlExcelExport</c> link. Exports every range row, not just the current page.</summary>
    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        var hasSenderRef = !string.IsNullOrWhiteSpace(SenderRef);
        var hasHistologyRef = !string.IsNullOrWhiteSpace(HistologyRef);
        if (!hasSenderRef && !hasHistologyRef)
            return RedirectToPage("/Search/SearchBlockRefs");

        var results = await SearchAsync(byHistologyRef: hasHistologyRef && !hasSenderRef);
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
