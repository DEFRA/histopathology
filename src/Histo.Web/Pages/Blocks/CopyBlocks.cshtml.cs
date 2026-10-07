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
/// <see cref="Histo.Web.Pages.Submissions.SubmissionDetailsBlockModel"/> (per-sample) or
/// <see cref="Histo.Web.Pages.Batches.BatchBlocksModel"/> (batch-wide) onto one
/// or more other samples in the same batch, duplicating each block's tissues.
///
/// Also reproduces legacy's <c>cbAutoGenerateHisto</c> "Auto Generate Histology Refs" option:
/// when ticked, every target sample that doesn't already have a histology ref is assigned the
/// next available ref of the same type as the source sample's own ref. The PG-number-reversal
/// fallback (<c>DefaultHistoRefPGReverse</c>) is NOT reproduced — that path was deliberately
/// removed app-wide with the Neuropath/Mouse Bioassay area (see
/// docs/Mouse-Bioassay-Neuropath-Removal-Analysis.md); only the counter-draw path applies here.
/// </summary>
public class CopyBlocksModel : HistoPageModel
{
    private readonly IBlockService _blocks;
    private readonly ISubmissionService _submissions;
    private readonly IBatchService _batches;
    private readonly IHistologyRefService _histologyRefs;
    private readonly IBlockTestService _blockTests;

    public CopyBlocksModel(ISessionService session, IBlockService blocks, ISubmissionService submissions,
        IBatchService batches, IHistologyRefService histologyRefs, IBlockTestService blockTests)
        : base(session)
    {
        _blocks = blocks;
        _submissions = submissions;
        _batches = batches;
        _histologyRefs = histologyRefs;
        _blockTests = blockTests;
    }

    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>
    /// Set only when reached from the per-animal <see cref="Histo.Web.Pages.Submissions.SubmissionDetailsBlockModel"/>
    /// (as opposed to the batch-wide <see cref="Histo.Web.Pages.Batches.BatchBlocksModel"/>) — drives
    /// <see cref="BackLinkPage"/> so Back/Cancel/Done return to whichever page actually sent the user here.
    /// </summary>
    [BindProperty(SupportsGet = true)] public int? AnimalId { get; set; }

    [BindProperty] public List<int> BlockIds { get; set; } = [];
    [BindProperty] public List<int> TargetAnimalIds { get; set; } = [];

    /// <summary>Legacy <c>cbAutoGenerateHisto</c> — "Auto Generate Histology Refs".</summary>
    [BindProperty] public bool AutoGenerateHistologyRefs { get; set; }

    public IReadOnlyList<Block> SourceBlocks { get; private set; } = [];
    public IReadOnlyList<Animal> TargetAnimals { get; private set; } = [];
    public string? Error { get; private set; }

    public string BackLinkPage => AnimalId is > 0
        ? $"/Submissions/SubmissionDetailsBlock?BatchId={BatchId}&AnimalId={AnimalId}"
        : $"/Batches/BatchBlocks?BatchId={BatchId}";

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Copy Blocks";
        ViewData["PageTitle"] = "Copy Blocks";

        var blockIdsCsv = TempData.Peek("CopyBlockIds") as string;
        var batchId = BatchId ?? Session.BatchID;
        if (string.IsNullOrEmpty(blockIdsCsv) || batchId is null)
            return RedirectToOrigin(batchId);

        var forbidden = await CheckBatchAccessAsync(_batches, batchId.Value);
        if (forbidden is not null) return forbidden;

        Session.BatchID = batchId;
        BatchId = batchId;
        BlockIds = ParseIds(blockIdsCsv);
        var loaded = await LoadDisplayDataAsync();
        return loaded ? Page() : RedirectToOrigin(batchId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Copy Blocks";
        ViewData["PageTitle"] = "Copy Blocks";

        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null || BlockIds.Count == 0)
            return RedirectToOrigin(batchId);

        var forbidden = await CheckBatchAccessAsync(_batches, batchId.Value);
        if (forbidden is not null) return forbidden;

        Session.BatchID = batchId;

        if (TargetAnimalIds.Count == 0)
        {
            Error = "Select at least one sample to copy the block(s) to.";
            await LoadDisplayDataAsync();
            return Page();
        }

        var userId = Session.UserID;
        var allBlocks = await _blocks.GetByBatchAsync(batchId.Value);
        var sourceBlocks = allBlocks.Where(b => BlockIds.Contains(b.ID)).ToList();
        var allTests = await _blockTests.GetAllSelectionsByBatchAsync(batchId.Value);

        foreach (var targetAnimalId in TargetAnimalIds)
            await CopyBlocksToAnimalAsync(sourceBlocks, allBlocks, allTests, batchId.Value, targetAnimalId, userId);

        var histoRefsAssigned = 0;
        string? histoRefProblem = null;
        if (AutoGenerateHistologyRefs && sourceBlocks.Count > 0)
            (histoRefsAssigned, histoRefProblem) = await AssignHistologyRefsAsync(batchId.Value, sourceBlocks[0].AnimalID, userId);

        TempData["StatusMessage"] = $"Copied {sourceBlocks.Count} block(s) to {TargetAnimalIds.Count} sample(s)."
            + (histoRefsAssigned > 0 ? $" Assigned a histology ref to {histoRefsAssigned} sample(s)." : string.Empty)
            + (histoRefProblem is null ? string.Empty : $" {histoRefProblem}");
        return RedirectToOrigin(batchId.Value);
    }

    /// <summary>
    /// Legacy <c>cbAutoGenerateHisto</c> path: runs the same "get next ref" draw as
    /// <c>SubmissionDetailsBlock</c>'s button for every target sample that doesn't already have a
    /// histology ref, using the ref type of the source sample's own ref. Returns how many samples
    /// were assigned and, when none were, why. Legacy source: CopyBlocks.aspx.vb::GetNextHistoNumber.
    /// </summary>
    private async Task<(int Assigned, string? Problem)> AssignHistologyRefsAsync(int batchId, int sourceAnimalId, int userId)
    {
        var animals = await GetAllAnimalsAsync(batchId);
        var sourceAnimal = animals.FirstOrDefault(a => a.ID == sourceAnimalId);
        var histologyType = HistologyRefTypeCode.FromExistingRef(sourceAnimal?.HistologyRef);
        if (histologyType is null)
            return (0, $"No histology refs were generated because the reference type could not be determined from sample {sourceAnimal?.SenderRef ?? "the source sample"}. Give that sample a histology reference first.");

        var assigned = 0;
        var skippedExisting = 0;
        var exhausted = false;
        foreach (var targetAnimalId in TargetAnimalIds)
        {
            var target = animals.FirstOrDefault(a => a.ID == targetAnimalId);
            if (target is null) continue;
            if (target.HistoRefSet)
            {
                skippedExisting++;
                continue;
            }

            var nextRef = await _histologyRefs.GetNextAvailableRefAsync(histologyType.Value);
            if (nextRef is null)
            {
                exhausted = true;
                break;
            }

            target.HistologyRef = nextRef;
            target.HistoRefSet = true;
            if (await _submissions.UpdateAnimalAsync(target, userId))
                assigned++;
        }

        if (exhausted)
            return (assigned, "No more histology references are available for that reference type.");
        if (assigned == 0 && skippedExisting > 0)
            return (0, "No histology refs were generated because the selected samples already have one.");

        return (assigned, null);
    }

    private IActionResult RedirectToOrigin(int? batchId) => AnimalId is > 0
        ? RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId, animalId = AnimalId })
        : RedirectToPage("/Batches/BatchBlocks", new { batchId });

    /// <summary>
    /// Copies each source block (its tissues and its Histology/Antibody/Stain test selections)
    /// onto the target animal, computing each new block's reference and order in sequence so
    /// multiple copies onto the same animal do not collide.
    /// </summary>
    private async Task CopyBlocksToAnimalAsync(
        IReadOnlyList<Block> sourceBlocks, IReadOnlyList<Block> allBlocks, IReadOnlyList<BlockTest> allTests,
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

            var tissues = await _submissions.GetTissuesByBlockAsync(sourceBlock.BatchID, sourceBlock.ID);
            foreach (var tissue in tissues)
                await _submissions.CopyTissueAsync(tissue, newBlockId, userId);

            await CopyBlockTestsAsync(allTests, sourceBlock.ID, batchId, newBlockId, userId);
        }
    }

    /// <summary>Reproduces the source block's test-type ticks on the copy — without this the copied block's Archive/EO/H&amp;E/IHC/Special stain boxes all come back empty.</summary>
    private async Task CopyBlockTestsAsync(IReadOnlyList<BlockTest> allTests, int sourceBlockId, int batchId, int newBlockId, int userId)
    {
        var sourceTests = allTests.Where(t => t.BlockID == sourceBlockId).ToList();
        if (sourceTests.Count == 0) return;

        await _blockTests.SaveTestSelectionsAsync(
            batchId,
            newBlockId,
            [.. sourceTests.Where(t => t.TestType == BlockTestType.Histology).Select(t => t.Code)],
            [.. sourceTests.Where(t => t.TestType == BlockTestType.Antibodies).Select(t => t.Code)],
            [.. sourceTests.Where(t => t.TestType == BlockTestType.Stain).Select(t => t.Code)],
            userId);
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
        var animals = await GetAllAnimalsAsync(batchId);
        TargetAnimals = animals.Where(a => a.ID != sourceAnimalId).OrderBy(a => a.SenderRef).ToList();
        return true;
    }

    /// <summary>
    /// Loads every animal in the batch, merging <c>GetBlockAnimalsByBatchAsync</c> (the only
    /// source that knows about samples already assigned to a block in a cassetted batch) with
    /// the plain animal list — mirrors <see cref="Histo.Web.Pages.Batches.BatchBlocksModel"/>'s
    /// identical merge. Without this, the source sample of a cassetted-batch block copy isn't
    /// found by <see cref="AssignHistologyRefsAsync"/>, so "Auto generate histology refs" silently
    /// assigned nothing.
    /// </summary>
    private async Task<IReadOnlyList<Animal>> GetAllAnimalsAsync(int batchId)
    {
        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(batchId);
        var allAnimals = await _submissions.GetAnimalsByBatchAsync(batchId);
        if (blockAnimals.Count == 0) return allAnimals;

        var seenIds = blockAnimals.Select(a => a.ID).ToHashSet();
        var missing = allAnimals.Where(a => !seenIds.Contains(a.ID));
        var merged = (IReadOnlyList<Animal>)[.. blockAnimals, .. missing];

        // GetBlockAnimalsByBatchAsync doesn't carry RowStamp, and EditAnimal's concurrency check
        // then matches no rows — the histology ref save succeeded silently without saving anything.
        var plainById = allAnimals.ToDictionary(a => a.ID, a => a);
        foreach (var animal in merged)
            if (plainById.TryGetValue(animal.ID, out var plain))
                animal.RowStamp = plain.RowStamp;

        return merged;
    }

    private static List<int> ParseIds(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
           .Select(s => int.TryParse(s, out var n) ? n : 0)
           .Where(n => n > 0)
           .ToList();
}
