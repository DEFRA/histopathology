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

    [BindProperty] public string? SenderRef { get; set; }
    [BindProperty] public string? HistologyRef { get; set; }

    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<BlockRefRangeHelpers.BlockRefRangeRow> Results { get; private set; } = [];

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

    public void OnGet()
    {
        ViewData["Title"] = "Search block refs";
        ViewData["PageTitle"] = "Search block refs";
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Search block refs";
        ViewData["PageTitle"] = "Search block refs";

        var hasSenderRef = !string.IsNullOrWhiteSpace(SenderRef);
        var hasHistologyRef = !string.IsNullOrWhiteSpace(HistologyRef);

        if (hasSenderRef == hasHistologyRef)
        {
            ErrorMessage = "Enter either the Sender Ref or the Histology Ref, not both.";
            PopulateGridViewData(TotalCount);
            return Page();
        }

        var usedBlocks = hasHistologyRef
            ? await _blocks.GetUsedBlockRefsByHistologyRefAsync(HistologyRef!)
            : await _blocks.GetUsedBlockRefsBySenderRefAsync(SenderRef!);

        Results = BlockRefRangeHelpers.ComputeRanges(
            usedBlocks.Select(b => (b.BlockRef, b.Status)).ToList());

        PopulateGridViewData(TotalCount);
        return Page();
    }
}
