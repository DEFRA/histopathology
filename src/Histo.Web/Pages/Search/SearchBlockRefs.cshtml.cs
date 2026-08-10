using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>Replaces <c>SearchBlockRefs.aspx</c>.</summary>
public class SearchBlockRefsModel : HistoPageModel
{
    private readonly IBlockService _blocks;

    public SearchBlockRefsModel(ISessionService session, IBlockService blocks)
        : base(session) => _blocks = blocks;

    [BindProperty] public string? SenderRef { get; set; }
    [BindProperty] public string? HistologyRef { get; set; }

    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<BlockRefRangeHelpers.BlockRefRangeRow> Results { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Search block refs";
        ViewData["PageTitle"] = "Search block refs";
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
            return Page();
        }

        var usedBlocks = hasHistologyRef
            ? await _blocks.GetUsedBlockRefsByHistologyRefAsync(HistologyRef!)
            : await _blocks.GetUsedBlockRefsBySenderRefAsync(SenderRef!);

        Results = BlockRefRangeHelpers.ComputeRanges(
            usedBlocks.Select(b => (b.BlockRef, b.Status)).ToList());

        return Page();
    }
}
