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
        var animals = await GetAllAnimalsAsync(batchId.Value);
        var sourceAnimal = sourceBlocks.Count > 0
            ? animals.FirstOrDefault(a => a.ID == sourceBlocks[0].AnimalID)
            : null;

        var histoRefsAssigned = 0;
        string? histoRefProblem = null;
        var histologyType = AutoGenerateHistologyRefs && sourceBlocks.Count > 0
            ? HistologyRefTypeCode.FromExistingRef(sourceAnimal?.HistologyRef)
            : null;
        if (AutoGenerateHistologyRefs && sourceBlocks.Count > 0 && histologyType is null)
            histoRefProblem = $"No histology refs were generated because the reference type could not be determined from sample {sourceAnimal?.SenderRef ?? "the source sample"}. Give that sample a histology reference first.";

        var skippedExisting = 0;
        foreach (var targetAnimalId in TargetAnimalIds)
        {
            var target = animals.FirstOrDefault(a => a.ID == targetAnimalId);
            if (target is null) continue;

            await SampleCopyHelper.CopyBlocksToAnimalAsync(
                _blocks, _submissions, _blockTests, sourceBlocks, allBlocks, allTests, batchId.Value, target, userId);

            // Legacy CopyBlocks.aspx.vb only generates for targets with no ref of their own.
            var drawRef = histologyType is not null && !target.HistoRefSet;
            if (histologyType is not null && target.HistoRefSet) skippedExisting++;

            if (drawRef)
            {
                var nextRef = await SampleCopyHelper.DrawRefMatchingAsync(_histologyRefs, sourceAnimal?.HistologyRef);
                if (nextRef is null)
                {
                    histoRefProblem = "No more histology references are available for that reference type.";
                }
                else
                {
                    target.HistologyRef = nextRef;
                    target.HistoRefSet = true;
                    histoRefsAssigned++;
                }
            }

            // One save per sample — carries both the drawn ref and the advanced NextBlockRef.
            await _submissions.UpdateAnimalAsync(target, userId);
        }

        if (histoRefProblem is null && histoRefsAssigned == 0 && skippedExisting > 0)
            histoRefProblem = "No histology refs were generated because the selected samples already have one.";

        TempData["StatusMessage"] = $"Copied {sourceBlocks.Count} block(s) to {TargetAnimalIds.Count} sample(s)."
            + (histoRefsAssigned > 0 ? $" Assigned a histology ref to {histoRefsAssigned} sample(s)." : string.Empty)
            + (histoRefProblem is null ? string.Empty : $" {histoRefProblem}");
        return RedirectToOrigin(batchId.Value);
    }

    private IActionResult RedirectToOrigin(int? batchId) => AnimalId is > 0
        ? RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId, animalId = AnimalId })
        : RedirectToPage("/Batches/BatchBlocks", new { batchId });

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
