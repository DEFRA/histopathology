using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Blocks;

/// <summary>
/// Replaces <c>CopyBlocks.aspx</c> — copies the block(s) selected on
/// <see cref="Histo.Web.Pages.Submissions.SubmissionDetailsBlockModel"/> onto one
/// or more other samples in the same batch, duplicating each block's tissues.
///
/// SIMPLIFIED: the legacy page also offered an "auto-generate histology ref"
/// option for the target samples (<c>cbAutoGenerateHisto</c>, PG-number reversal,
/// neuropath range lookups). That histology-ref generation is not reproduced —
/// target samples keep whatever histology reference they already have. See
/// <see cref="Histo.Submissions.Services.SubmissionService.CopyAnimalAsync"/> and
/// <c>AnimalHelpers.ComputePgAutoHistologyRef</c> for the equivalent logic used
/// elsewhere, which could be wired in here in a follow-on phase if required.
/// </summary>
public class CopyBlocksModel : HistoPageModel
{
    private readonly IBlockService _blocks;
    private readonly ISubmissionService _submissions;
    private readonly IBatchService _batches;

    public CopyBlocksModel(ISessionService session, IBlockService blocks, ISubmissionService submissions, IBatchService batches)
        : base(session)
    {
        _blocks = blocks;
        _submissions = submissions;
        _batches = batches;
    }

    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    [BindProperty] public List<int> BlockIds { get; set; } = [];
    [BindProperty] public List<int> TargetAnimalIds { get; set; } = [];

    public IReadOnlyList<Block> SourceBlocks { get; private set; } = [];
    public IReadOnlyList<Animal> TargetAnimals { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Copy Blocks";
        ViewData["PageTitle"] = "Copy Blocks";

        var blockIdsCsv = TempData.Peek("CopyBlockIds") as string;
        var batchId = BatchId ?? Session.BatchID;
        if (string.IsNullOrEmpty(blockIdsCsv) || batchId is null)
            return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId });

        var forbidden = await CheckBatchAccessAsync(_batches, batchId.Value);
        if (forbidden is not null) return forbidden;

        Session.BatchID = batchId;
        BatchId = batchId;
        BlockIds = ParseIds(blockIdsCsv);
        var loaded = await LoadDisplayDataAsync();
        return loaded ? Page() : RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Copy Blocks";
        ViewData["PageTitle"] = "Copy Blocks";

        if (Session.BatchID is null || BlockIds.Count == 0)
            return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId = BatchId ?? Session.BatchID });

        if (TargetAnimalIds.Count == 0)
        {
            Error = "Select at least one sample to copy the block(s) to.";
            await LoadDisplayDataAsync();
            return Page();
        }

        var batchId = Session.BatchID ?? 0;
        var userId = Session.UserID;
        var allBlocks = await _blocks.GetByBatchAsync(batchId);
        var sourceBlocks = allBlocks.Where(b => BlockIds.Contains(b.ID)).ToList();

        foreach (var targetAnimalId in TargetAnimalIds)
            await CopyBlocksToAnimalAsync(sourceBlocks, allBlocks, batchId, targetAnimalId, userId);

        TempData["StatusMessage"] = $"Copied {sourceBlocks.Count} block(s) to {TargetAnimalIds.Count} sample(s).";
        return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId });
    }

    /// <summary>
    /// Copies each source block (and its tissues) onto the target animal,
    /// computing each new block's reference and order in sequence so multiple
    /// copies onto the same animal do not collide.
    /// </summary>
    private async Task CopyBlocksToAnimalAsync(
        IReadOnlyList<Block> sourceBlocks, IReadOnlyList<Block> allBlocks,
        int batchId, int targetAnimalId, int userId)
    {
        var animalBlocks = allBlocks.Where(b => b.AnimalID == targetAnimalId).ToList();
        var refs = animalBlocks.Select(b => b.BlockRef).ToList();
        var orders = animalBlocks.Select(b => b.Order).ToList();

        foreach (var sourceBlock in sourceBlocks)
        {
            var newBlockId = await _blocks.CopyBlockAsync(sourceBlock, batchId, targetAnimalId, refs, orders, userId);
            if (newBlockId <= 0) continue;

            refs.Add(BlockHelpers.ComputeNextBlockRef(refs));
            orders.Add(BlockHelpers.ComputeNextOrder(orders));

            var tissues = await _submissions.GetTissuesByBlockAsync(sourceBlock.ID);
            foreach (var tissue in tissues)
                await _submissions.CopyTissueAsync(tissue, newBlockId, userId);
        }
    }

    /// <summary>Loads <see cref="SourceBlocks"/> and <see cref="TargetAnimals"/>. Returns false if nothing to copy.</summary>
    private async Task<bool> LoadDisplayDataAsync()
    {
        if (Session.BatchID is null) return false;
        var batchId = Session.BatchID ?? 0;

        var allBlocks = await _blocks.GetByBatchAsync(batchId);
        SourceBlocks = allBlocks.Where(b => BlockIds.Contains(b.ID)).ToList();
        if (SourceBlocks.Count == 0) return false;

        var sourceAnimalId = SourceBlocks[0].AnimalID;
        var animals = await _submissions.GetAnimalsByBatchAsync(batchId);
        TargetAnimals = animals.Where(a => a.ID != sourceAnimalId).OrderBy(a => a.SenderRef).ToList();
        return true;
    }

    private static List<int> ParseIds(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
           .Select(s => int.TryParse(s, out var n) ? n : 0)
           .Where(n => n > 0)
           .ToList();
}
