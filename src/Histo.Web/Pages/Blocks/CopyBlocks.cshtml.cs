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

    public CopyBlocksModel(ISessionService session, IBlockService blocks, ISubmissionService submissions,
        IBatchService batches, IHistologyRefService histologyRefs)
        : base(session)
    {
        _blocks = blocks;
        _submissions = submissions;
        _batches = batches;
        _histologyRefs = histologyRefs;
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

        foreach (var targetAnimalId in TargetAnimalIds)
            await CopyBlocksToAnimalAsync(sourceBlocks, allBlocks, batchId.Value, targetAnimalId, userId);

        var histoRefsAssigned = 0;
        if (AutoGenerateHistologyRefs && sourceBlocks.Count > 0)
            histoRefsAssigned = await AssignHistologyRefsAsync(batchId.Value, sourceBlocks[0].AnimalID, userId);

        TempData["StatusMessage"] = $"Copied {sourceBlocks.Count} block(s) to {TargetAnimalIds.Count} sample(s)."
            + (histoRefsAssigned > 0 ? $" Assigned a histology ref to {histoRefsAssigned} sample(s)." : string.Empty);
        return RedirectToOrigin(batchId.Value);
    }

    /// <summary>
    /// Legacy <c>cbAutoGenerateHisto</c> path: assigns the next available histology ref (of the
    /// same type as the source sample's own ref) to every target sample that doesn't already have
    /// one. Returns the number of samples assigned. Legacy source: CopyBlocks.aspx.vb::GetNextHistoNumber.
    /// </summary>
    private async Task<int> AssignHistologyRefsAsync(int batchId, int sourceAnimalId, int userId)
    {
        var animals = await _submissions.GetAnimalsByBatchAsync(batchId);
        var sourceAnimal = animals.FirstOrDefault(a => a.ID == sourceAnimalId);
        var histologyType = HistologyRefTypeCode.FromExistingRef(sourceAnimal?.HistologyRef);
        if (histologyType is null) return 0;

        var assigned = 0;
        foreach (var targetAnimalId in TargetAnimalIds)
        {
            var target = animals.FirstOrDefault(a => a.ID == targetAnimalId);
            if (target is null || target.HistoRefSet) continue;

            var nextRef = await _histologyRefs.GetNextAvailableRefAsync(histologyType.Value);
            if (nextRef is null) continue;

            target.HistologyRef = nextRef;
            target.HistoRefSet = true;
            if (await _submissions.UpdateAnimalAsync(target, userId))
                assigned++;
        }

        return assigned;
    }

    private IActionResult RedirectToOrigin(int? batchId) => AnimalId is > 0
        ? RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId, animalId = AnimalId })
        : RedirectToPage("/Batches/BatchBlocks", new { batchId });

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

            var tissues = await _submissions.GetTissuesByBlockAsync(sourceBlock.BatchID, sourceBlock.ID);
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
