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
/// The migrated entry point is <c>Pages/Batches/BatchBlocks.cshtml</c>, the replacement for
/// <c>BatchBlocks.aspx</c>.
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
/// <c>Pages/Batches/BatchBlocks.cshtml</c>.
/// </summary>
public class CopySamplesModel : HistoPageModel
{
    private readonly IBatchService _batches;
    private readonly ISubmissionService _submissions;
    private readonly IBlockService _blocks;
    private readonly IBlockTestService _blockTests;

    public CopySamplesModel(ISessionService session, IBatchService batches, ISubmissionService submissions, IBlockService blocks, IBlockTestService blockTests)
        : base(session)
    {
        _batches = batches;
        _submissions = submissions;
        _blocks = blocks;
        _blockTests = blockTests;
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
        var forbidden = await CheckBatchAccessAsync(_batches, Session.BatchID.Value);
        if (forbidden is not null) return forbidden;
        TargetAnimals = await _submissions.GetAnimalsByBatchAsync(Session.BatchID ?? 0);
        return Page();
    }

    public async Task<IActionResult> OnPostFindAsync()
    {
        SetTitle();
        if (Session.BatchID is null) return RedirectToPage("/Index");
        var forbidden = await CheckBatchAccessAsync(_batches, Session.BatchID.Value);
        if (forbidden is not null) return forbidden;
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
        var forbidden = await CheckBatchAccessAsync(_batches, Session.BatchID.Value);
        if (forbidden is not null) return forbidden;
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
        // Source and target are different batches here (unlike same-batch CopyBlocksModel), so the
        // test selections to copy must be looked up from the source batch, not the target batch.
        var sourceTests = await _blockTests.GetAllSelectionsByBatchAsync(SourceBatchId);
        var targetAnimals = await _submissions.GetAnimalsByBatchAsync(currentBatchId);
        var blocksCopied = 0;

        foreach (var targetAnimalId in TargetAnimalIds)
        {
            var target = targetAnimals.FirstOrDefault(a => a.ID == targetAnimalId);
            if (target is null) continue;

            try
            {
                await SampleCopyHelper.CopyBlocksToAnimalAsync(
                    new SampleCopyServices(_blocks, _submissions, _blockTests), sourceBlocks, allTargetBlocks, sourceTests, currentBatchId, target, userId);
            }
            catch (InvalidOperationException ex)
            {
                await _submissions.UpdateAnimalAsync(target, userId);
                Error = blocksCopied == 0
                    ? ex.Message
                    : $"{ex.Message} {blocksCopied} block(s) were copied successfully before this failure.";
                return Page();
            }

            await _submissions.UpdateAnimalAsync(target, userId);
            blocksCopied += sourceBlocks.Count;
        }

        return RedirectToPage("/Blocks/CopySamplesSummary", new
        {
            sourceBatchId = SourceBatchId,
            sourceAnimalId = SourceAnimalId,
            targetAnimalIds = string.Join(",", TargetAnimalIds),
            blocksCopiedCount = blocksCopied,
        });
    }

    public IActionResult OnPostCancel() => RedirectToPage("/Batches/BatchBlocks");

    private void SetTitle()
    {
        ViewData["Title"] = "Copy Samples";
        ViewData["PageTitle"] = "Copy Samples";
    }
}
