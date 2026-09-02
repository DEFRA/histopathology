using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>
/// Replaces <c>SubmissionDetailsBlock.aspx</c> — block summary for a batch submission's samples
/// (cassetted workflow). Consolidates the previously separate batch-wide
/// <c>Pages/Blocks/BlockDetails.cshtml</c> into a single page: when <see cref="AnimalId"/> is
/// supplied (reached from SampleSummary's "Edit sample"), it shows the full add/edit/delete/copy
/// view for that one sample's blocks; when omitted (reached from BatchDetails' "Assign blocks"), it
/// shows a read/delete/copy overview of every block in the batch, each row linking into the
/// animal-scoped view to add/edit blocks for that specific sample.
///
/// SIMPLIFIED: the legacy page renders a hierarchical block/tissue grid with per-block
/// test-assignment checkboxes (EO, H&amp;E, H&amp;E BSE, IHC Prp, IHC Other, Special Stain).
/// The migrated <see cref="Block"/> model does not carry per-block test flags — <c>BlockTest</c>/
/// <c>IBlockTestService</c> exist but are wired only into the downstream QC review/dispatch screens
/// (<c>QC/QualityData.cshtml</c>), not into block creation; adding per-block test selection here needs
/// a separate investigation into whether the DB already populates those rows on block creation.
/// Per-block tissue assignment IS reproduced below — <see cref="ISubmissionService"/> already exposes
/// <c>TissueOwner.Block</c> tissue CRUD, it was simply not wired into this page's original migration.
/// </summary>
public class SubmissionDetailsBlockModel : HistoPageModel
{
    private const int LookupTissueCode = 9;

    private readonly ISubmissionService _submissions;
    private readonly IBlockService _blocks;
    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;

    public SubmissionDetailsBlockModel(ISessionService session, ISubmissionService submissions, IBlockService blocks, IBatchService batches, ILookupService lookups)
        : base(session)
    {
        _submissions = submissions;
        _blocks = blocks;
        _batches = batches;
        _lookups = lookups;
    }

    /// <summary>
    /// Batch/animal ID from the URL (route/query). Falls back to <see cref="ISessionService.BatchID"/>/
    /// <see cref="ISessionService.AnimalID"/> for links not yet migrated — Phase 1 of the route-based-state rollout.
    /// </summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>
    /// Optional — when omitted, the page shows a batch-wide overview of every block instead of
    /// one sample's blocks (the former <c>Blocks/BlockDetails.cshtml</c> use case).
    /// </summary>
    [BindProperty(SupportsGet = true)] public int? AnimalId { get; set; }

    [BindProperty] public string? PMDate { get; set; }
    [BindProperty] public string? HistologyRef { get; set; }

    [BindProperty] public string? NewBlockRef { get; set; }
    [BindProperty] public string? NewCustomerRef { get; set; }
    [BindProperty] public bool NewRepeatBlock { get; set; }

    /// <summary>Number of identical blocks to create in one submit — restores the legacy "Number of blocks" bulk-create field.</summary>
    [BindProperty] public int NewNumberOfBlocks { get; set; } = 1;

    [BindProperty] public int NewTissueBlockId { get; set; }
    [BindProperty] public string NewTissueCode { get; set; } = string.Empty;
    [BindProperty] public short NewTissueNoPieces { get; set; } = 1;
    [BindProperty] public string? NewTissueComment { get; set; }

    /// <summary>Block awaiting delete confirmation — drives the inline GOV.UK confirmation panel (replaces browser confirm()).</summary>
    [BindProperty(SupportsGet = true)] public int? ConfirmDeleteBlockId { get; set; }

    /// <summary>Set when the user has requested the inline "Check used block refs" lookup for this sample.</summary>
    [BindProperty(SupportsGet = true)] public bool ShowUsedRefs { get; set; }

    public Animal? Animal { get; private set; }
    public Batch? Batch { get; private set; }
    public IReadOnlyList<Block> Blocks { get; private set; } = [];
    public IReadOnlyList<BlockRefRangeHelpers.BlockRefRangeRow> UsedBlockRefResults { get; private set; } = [];

    /// <summary>Unused pre-booked block refs for this sample — populated only for pre-cassetted submissions.</summary>
    public IReadOnlyList<Block> PreBookedBlockRefs { get; private set; } = [];

    /// <summary>True for pre-cassetted submissions, where the block ref must come from the pre-booked list and histology ref is mandatory.</summary>
    public bool IsPreCassetted => Batch?.IsPreCassetted == true;

    public IReadOnlyList<LookupItem> TissueOptions { get; private set; } = [];
    public IReadOnlyDictionary<int, IReadOnlyList<Tissue>> TissuesByBlockId { get; private set; } =
        new Dictionary<int, IReadOnlyList<Tissue>>();

    /// <summary>Sender ref keyed by AnimalID — used only in batch-wide mode (no <see cref="AnimalId"/>) to label each row.</summary>
    public IReadOnlyDictionary<int, string> SenderRefsByAnimalId { get; private set; } = new Dictionary<int, string>();

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Sample Blocks";
        ViewData["PageTitle"] = "Sample Blocks";

        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;

        if (Animal is null)
        {
            // Batch-wide overview (former Blocks/BlockDetails.cshtml behaviour) — every block in the batch.
            Blocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
            var animals = await _submissions.GetAnimalsByBatchAsync(BatchId ?? 0);
            SenderRefsByAnimalId = animals.ToDictionary(a => a.ID, a => a.SenderRef);
            return Page();
        }

        PMDate = Animal.PMDate;
        HistologyRef = Animal.HistologyRef;

        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        Blocks = allBlocks.Where(b => b.AnimalID == Animal.ID).ToList();

        await LoadSupportingDataAsync();

        if (ShowUsedRefs)
        {
            var used = await _blocks.GetUsedBlockRefsBySenderRefAsync(Animal.SenderRef);
            UsedBlockRefResults = BlockRefRangeHelpers.ComputeRanges(
                used.Select(b => (b.BlockRef, b.Status)).ToList());
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveDetailsAsync()
    {
        ViewData["Title"] = "Sample Blocks";
        ViewData["PageTitle"] = "Sample Blocks";

        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        var updated = new Animal
        {
            ID = Animal!.ID,
            BatchSubmissionID = Animal.BatchSubmissionID,
            SenderRef = Animal.SenderRef,
            NextBlockRef = Animal.NextBlockRef,
            HistoRefSet = !string.IsNullOrWhiteSpace(HistologyRef),
            HistologyRef = HistologyRef,
            OnHold = Animal.OnHold,
            PMDate = PMDate,
            PMDateSet = !string.IsNullOrWhiteSpace(PMDate),
            IsPGNumber = Animal.IsPGNumber,
            BookedHistologyRef = Animal.BookedHistologyRef,
            RowStamp = Animal.RowStamp,
        };

        await _submissions.UpdateAnimalAsync(updated, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>Adds a new block for this sample -- restores the "Add block" action missing from the migrated page.</summary>
    public async Task<IActionResult> OnPostAddBlockAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });
        await LoadSupportingDataAsync();

        if (IsPreCassetted && !PreBookedBlockRefs.Any(b => b.BlockRef == NewBlockRef))
        {
            ErrorMessage = "Select one of the pre-booked block references for this pre-cassetted submission.";
            var allBlocksForError = await _blocks.GetByBatchAsync(BatchId ?? 0);
            Blocks = allBlocksForError.Where(b => b.AnimalID == Animal!.ID).ToList();
            return Page();
        }

        // Pre-cassetted block refs must each be selected individually from the pre-booked list.
        if (IsPreCassetted) NewNumberOfBlocks = 1;

        if (!string.IsNullOrWhiteSpace(NewBlockRef))
        {
            var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
            var existingOrders = allBlocks.Select(b => b.Order).ToList();
            var existingRefs = allBlocks.Select(b => b.BlockRef).ToList();
            var count = Math.Max(1, NewNumberOfBlocks);

            // First block uses the entered/selected ref; further blocks (only offered when not
            // pre-cassetted, since pre-booked refs must be selected individually) auto-increment,
            // restoring the legacy "Number of blocks" bulk-create field.
            var blockRef = NewBlockRef;
            for (var i = 0; i < count; i++)
            {
                await _blocks.AddBlockAsync(
                    BatchId ?? 0, Animal!.ID, blockRef, existingOrders, Session.UserID,
                    NewCustomerRef, comment: null, repeatBlock: NewRepeatBlock);

                existingRefs.Add(blockRef);
                existingOrders.Add(BlockHelpers.ComputeNextOrder(existingOrders));
                blockRef = BlockHelpers.ComputeNextBlockRef(existingRefs);
            }
        }

        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int blockId)
    {
        await _blocks.DeleteBlockAsync(blockId, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>Adds a tissue to a specific block — restores per-block tissue assignment missing from the migrated page.</summary>
    public async Task<IActionResult> OnPostAddTissueAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        if (NewTissueBlockId > 0 && !string.IsNullOrWhiteSpace(NewTissueCode))
        {
            var tissue = new Tissue
            {
                OwnerID = NewTissueBlockId,
                Owner = TissueOwner.Block,
                TissueCode = NewTissueCode,
                NoPieces = NewTissueNoPieces,
                Comment = NewTissueComment,
            };
            await _submissions.AddTissueAsync(tissue, Session.UserID);
        }

        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    public async Task<IActionResult> OnPostDeleteTissueAsync(int tissueId)
    {
        await _submissions.DeleteTissueAsync(tissueId, TissueOwner.Block, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>
    /// Stores the selected block IDs and redirects to the "Copy blocks" workflow.
    /// Replaces the legacy <c>SubmissionDetailsBlock.aspx.vb</c>::<c>btnCopyBlock_Click</c>
    /// handler, which stored the selection in <c>Session(SV_BlockIDs)</c> before
    /// redirecting to <c>CopyBlocks.aspx</c>.
    /// </summary>
    public async Task<IActionResult> OnPostCopyAsync(List<int>? blockIds)
    {
        if (blockIds is null || blockIds.Count == 0)
        {
            var redirect = await LoadAnimalAsync();
            return redirect ?? RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
        }

        TempData["CopyBlockIds"] = string.Join(",", blockIds);
        return RedirectToPage("/Blocks/CopyBlocks");
    }

    /// <summary>
    /// Resolves <see cref="Animal"/> from the URL's batch/animal ID. Returns a redirect if the
    /// batch is unavailable. <see cref="AnimalId"/> is optional — when the caller did not supply
    /// it explicitly (batch-wide mode), <see cref="Animal"/> is left null rather than falling back
    /// to a possibly-stale <see cref="ISessionService.AnimalID"/> from browsing a different sample.
    /// </summary>
    private async Task<IActionResult?> LoadAnimalAsync()
    {
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage("/Index");

        var forbidden = await CheckBatchAccessAsync(_batches, batchId.Value);
        if (forbidden is not null) return forbidden;

        Session.BatchID = batchId; // keep session in sync as a fallback for links not yet migrated
        BatchId = batchId;

        if (AnimalId is null or <= 0)
        {
            Animal = null;
            return null;
        }

        Session.AnimalID = AnimalId;

        // Mirrors SampleSummaryModel: GetAnimalsByBatchAsync alone is incomplete for cassetted
        // batches — the animal may only exist in the block-animal table (BATCH_BLOCK_ANIMAL).
        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(batchId.Value);
        Animal = blockAnimals.FirstOrDefault(a => a.ID == AnimalId);
        if (Animal is null)
        {
            var animals = await _submissions.GetAnimalsByBatchAsync(batchId.Value);
            Animal = animals.FirstOrDefault(a => a.ID == AnimalId);
        }
        return Animal is null ? RedirectToPage("/Submissions/SampleSummary", new { batchId }) : null;
    }

    /// <summary>Loads the batch (for the pre-cassetted flag), pre-booked block refs, tissue pick-list, and per-block tissues.</summary>
    private async Task LoadSupportingDataAsync()
    {
        Batch = await _batches.GetByIdAsync(BatchId ?? 0);
        PreBookedBlockRefs = await _blocks.GetPreBookedByAnimalAsync(Animal!.ID);
        TissueOptions = await _lookups.GetLookupDataAsync(LookupTissueCode);

        var tissuesByBlockId = new Dictionary<int, IReadOnlyList<Tissue>>();
        foreach (var block in Blocks)
            tissuesByBlockId[block.ID] = await _submissions.GetTissuesByBlockAsync(block.ID);
        TissuesByBlockId = tissuesByBlockId;
    }
}
