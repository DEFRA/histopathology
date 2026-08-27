using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Blocks;

/// <summary>Replaces <c>BlockDetails.aspx</c> / <c>BatchBlocks.aspx</c> — lists blocks for the current animal.</summary>
public class BlockDetailsModel : HistoPageModel
{
    private readonly IBlockService _blocks;
    private readonly IBatchService _batches;

    public BlockDetailsModel(ISessionService session, IBlockService blocks, IBatchService batches)
        : base(session)
    {
        _blocks = blocks;
        _batches = batches;
    }

    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>Block awaiting delete confirmation -- drives the inline GOV.UK confirmation panel (replaces browser confirm()).</summary>
    [BindProperty(SupportsGet = true)] public int? ConfirmDeleteBlockId { get; set; }

    public IReadOnlyList<Block> Blocks { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Block Details";
        ViewData["PageTitle"] = "Block Details";
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage("/Index");

        var forbidden = await CheckBatchAccessAsync(_batches, batchId.Value);
        if (forbidden is not null) return forbidden;

        Session.BatchID = batchId;
        BatchId = batchId;
        Blocks = await _blocks.GetByBatchAsync(batchId.Value);
        return Page();
    }

    public IActionResult OnPostSelect(int blockId)
    {
        Session.BlockID = blockId;
        return RedirectToPage("/Blocks/BlockDetails", new { batchId = BatchId ?? Session.BatchID });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int blockId, byte[] rowStamp)
    {
        await _blocks.DeleteBlockAsync(blockId, Session.UserID);
        return RedirectToPage(new { batchId = BatchId ?? Session.BatchID });
    }
}
