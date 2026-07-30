using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Blocks;

/// <summary>Replaces <c>BlockDetails.aspx</c> / <c>BatchBlocks.aspx</c> — lists blocks for the current animal.</summary>
public class BlockDetailsModel : HistoPageModel
{
    private readonly BlockService _blocks;

    public BlockDetailsModel(ISessionService session, BlockService blocks)
        : base(session) => _blocks = blocks;

    public IReadOnlyList<Block> Blocks { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Block Details";
        ViewData["PageTitle"] = "Block Details";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");
        Blocks = await _blocks.GetByBatchAsync(Session.BatchID ?? 0);
        return Page();
    }

    public async Task<IActionResult> OnPostSelectAsync(int blockId)
    {
        Session.BlockID = blockId;
        return await Task.FromResult(RedirectToPage("/Blocks/BlockDetails"));
    }

    public async Task<IActionResult> OnPostDeleteAsync(int blockId, byte[] rowStamp)
    {
        await _blocks.DeleteBlockAsync(blockId, Session.UserID);
        return RedirectToPage();
    }
}
