using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Blocks;

/// <summary>
/// Replaces <c>CopySamples.aspx</c> / <c>CopySamplesBlocks.aspx</c> — copies the
/// block(s) (and their tissues) assigned to a sample in a different ("source")
/// submission onto one or more samples in the current submission.
///
/// Legacy source: entry point was <c>BatchBlocks.aspx</c> (<c>btnCopySamples</c>).
/// The migrated entry point is <see cref="BlockDetailsModel"/>, the replacement
/// for <c>BatchBlocks.aspx</c>.
///
/// SIMPLIFIED: the legacy 3-page wizard (<c>CopySamples.aspx</c> →
/// <c>CopySamplesBlocks.aspx</c> → <c>Finish</c>) maintained an in-memory
/// working <c>DataSet</c> (<c>SV_BatchDetails</c>/<c>SV_OldBatchDetails</c>,
/// <c>BATCH_BLOCK_ANIMAL</c> "pre-booked" plumbing) purely to drive ASPX grid
/// data-binding across postbacks. That plumbing has no equivalent here — this
/// page queries the source and current submissions directly and copies blocks
/// in a single step, consistent with <c>Pages/Blocks/CopyBlocks.cshtml</c>.
/// The legacy submission-type match check (TSE vs Non-TSE) is not reproduced —
/// the migrated <see cref="Batch"/> model does not carry a batch type. The
/// separate read-only "CopySamplesSummary.aspx" batch-wide grid (reached via
/// <c>btnSummary</c>, independent of the copy operation itself) is not
/// reproduced — equivalent detail is already available via
/// <see cref="BlockDetailsModel"/> and <c>Pages/Submissions/SubmissionDetailsBlock.cshtml</c>.
/// </summary>
public class CopySamplesModel : HistoPageModel
{
    private readonly IBatchService _batches;
    private readonly ISubmissionService _submissions;
    private readonly IBlockService _blocks;

    public CopySamplesModel(ISessionService session, IBatchService batches, ISubmissionService submissions, IBlockService blocks)
        : base(session)
    {
        _batches = batches;
        _submissions = submissions;
        _blocks = blocks;
    }

    [BindProperty] public int SourceBatchId { get; set; }
    [BindProperty] public int SourceAnimalId { get; set; }
    [BindProperty] public List<int> TargetAnimalIds { get; set; } = [];

    public Batch? SourceBatch { get; private set; }
    public IReadOnlyList<Animal> SourceAnimals { get; private set; } = [];
    public IReadOnlyList<Animal> TargetAnimals { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SetTitle();
        if (Session.BatchID is null) return RedirectToPage("/Index");
        TargetAnimals = await _submissions.GetAnimalsByBatchAsync(Session.BatchID ?? 0);
        return Page();
    }

    public async Task<IActionResult> OnPostFindAsync()
    {
        SetTitle();
        if (Session.BatchID is null) return RedirectToPage("/Index");
        TargetAnimals = await _submissions.GetAnimalsByBatchAsync(Session.BatchID ?? 0);

        var batch = await _batches.GetByIdAsync(SourceBatchId);
        if (batch is null)
        {
            Error = "The submission to copy from could not be found.";
            return Page();
        }

        var sourceBlocks = await _blocks.GetByBatchAsync(SourceBatchId);
        var animalIdsWithBlocks = sourceBlocks.Select(b => b.AnimalID).ToHashSet();
        if (animalIdsWithBlocks.Count == 0)
        {
            Error = "The selected submission has no blocks assigned to copy.";
            return Page();
        }

        var animals = await _submissions.GetAnimalsByBatchAsync(SourceBatchId);
        SourceAnimals = animals.Where(a => animalIdsWithBlocks.Contains(a.ID)).OrderBy(a => a.SenderRef).ToList();
        if (SourceAnimals.Count == 0)
        {
            Error = "The selected submission has no samples with blocks assigned.";
            return Page();
        }

        SourceBatch = batch;
        return Page();
    }

    public async Task<IActionResult> OnPostCopyAsync()
    {
        SetTitle();
        if (Session.BatchID is null) return RedirectToPage("/Index");
        var currentBatchId = Session.BatchID ?? 0;
        TargetAnimals = await _submissions.GetAnimalsByBatchAsync(currentBatchId);

        SourceBatch = await _batches.GetByIdAsync(SourceBatchId);
        var sourceBlocksAll = SourceBatch is null ? [] : await _blocks.GetByBatchAsync(SourceBatchId);
        var animalIdsWithBlocks = sourceBlocksAll.Select(b => b.AnimalID).ToHashSet();
        SourceAnimals = SourceBatch is null
            ? []
            : (await _submissions.GetAnimalsByBatchAsync(SourceBatchId))
                .Where(a => animalIdsWithBlocks.Contains(a.ID)).OrderBy(a => a.SenderRef).ToList();

        if (SourceBatch is null || SourceAnimals.Count == 0)
        {
            Error = "Find a source submission before copying samples.";
            return Page();
        }

        if (TargetAnimalIds.Count == 0)
        {
            Error = "Select at least one sample in the current submission to copy the blocks to.";
            return Page();
        }

        var sourceBlocks = sourceBlocksAll.Where(b => b.AnimalID == SourceAnimalId).ToList();
        if (sourceBlocks.Count == 0)
        {
            Error = "Select a sample to copy from.";
            return Page();
        }

        var userId = Session.UserID;
        var allTargetBlocks = await _blocks.GetByBatchAsync(currentBatchId);
        var blocksCopied = 0;

        foreach (var targetAnimalId in TargetAnimalIds)
            blocksCopied += await CopyBlocksToAnimalAsync(sourceBlocks, allTargetBlocks, currentBatchId, targetAnimalId, userId);

        return RedirectToPage("/Blocks/CopySamplesSummary", new
        {
            sourceBatchId = SourceBatchId,
            sourceAnimalId = SourceAnimalId,
            targetAnimalIds = string.Join(",", TargetAnimalIds),
            blocksCopiedCount = blocksCopied,
        });
    }

    public IActionResult OnPostCancel() => RedirectToPage("/Blocks/BlockDetails");

    /// <summary>
    /// Copies each source block (and its tissues) onto the target animal in the
    /// current batch, computing each new block's reference and order in sequence
    /// so multiple copies onto the same animal do not collide. Mirrors
    /// <c>CopyBlocksModel.CopyBlocksToAnimalAsync</c>. Returns the number of blocks copied.
    /// </summary>
    private async Task<int> CopyBlocksToAnimalAsync(
        IReadOnlyList<Block> sourceBlocks, IReadOnlyList<Block> allBlocks,
        int batchId, int targetAnimalId, int userId)
    {
        var animalBlocks = allBlocks.Where(b => b.AnimalID == targetAnimalId).ToList();
        var refs = animalBlocks.Select(b => b.BlockRef).ToList();
        var orders = animalBlocks.Select(b => b.Order).ToList();
        var copied = 0;

        foreach (var sourceBlock in sourceBlocks)
        {
            var newBlockId = await _blocks.CopyBlockAsync(sourceBlock, batchId, targetAnimalId, refs, orders, userId);
            if (newBlockId <= 0) continue;

            refs.Add(BlockHelpers.ComputeNextBlockRef(refs));
            orders.Add(BlockHelpers.ComputeNextOrder(orders));
            copied++;

            var tissues = await _submissions.GetTissuesByBlockAsync(sourceBlock.ID);
            foreach (var tissue in tissues)
                await _submissions.CopyTissueAsync(tissue, newBlockId, userId);
        }

        return copied;
    }

    private void SetTitle()
    {
        ViewData["Title"] = "Copy Samples";
        ViewData["PageTitle"] = "Copy Samples";
    }
}
