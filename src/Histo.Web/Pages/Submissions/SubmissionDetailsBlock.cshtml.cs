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
/// Per-block Histology/Antibodies/Stain test selection, block editing, and the Histology Ref
/// Type "or Pick" dropdown are now reproduced below using only existing stored procedures
/// (<c>AddBlock</c>/<c>EditBlock</c>; <c>Add/Edit/DeleteBlock{Histology,Antibodies,Stain}</c>;
/// <c>GetUnusedRefsAsync</c>) — no new stored procedures were required.
/// Per-block tissue assignment IS reproduced below — <see cref="ISubmissionService"/> already exposes
/// <c>TissueOwner.Block</c> tissue CRUD, it was simply not wired into this page's original migration.
/// </summary>
public class SubmissionDetailsBlockModel : HistoPageModel
{
    private const int LookupTissueCode = 9;
    private const int LookupTseAntibodies = 4;
    private const int LookupNonTseAntibodies = 5;
    private const int LookupSpecialStain = 6;

    /// <summary>Legacy source: Common.vb::HistologyRefType enum — fixed, not database-driven.</summary>
    public static readonly IReadOnlyList<(int Value, string Label)> HistologyRefTypeOptions =
    [
        (1, "Neuropath"),
        (2, "Abattoir survey"),
        (3, "TB diagnostics"),
        (4, "General pool"),
        (5, "Mouse projects"),
        (6, "Use PG number"),
    ];

    private readonly ISubmissionService _submissions;
    private readonly IBlockService _blocks;
    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;
    private readonly IBlockTestService _blockTests;
    private readonly IHistologyRefService _histologyRefs;
    private readonly ILogger<SubmissionDetailsBlockModel> _logger;

    public SubmissionDetailsBlockModel(ISessionService session, ISubmissionService submissions, IBlockService blocks,
        IBatchService batches, ILookupService lookups, IBlockTestService blockTests, IHistologyRefService histologyRefs,
        ILogger<SubmissionDetailsBlockModel> logger)
        : base(session)
    {
        _submissions = submissions;
        _blocks = blocks;
        _batches = batches;
        _lookups = lookups;
        _blockTests = blockTests;
        _histologyRefs = histologyRefs;
        _logger = logger;
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

    /// <summary>Block being edited — pre-fills the Add/Edit block form and switches its submit handler.</summary>
    [BindProperty(SupportsGet = true)] public int? EditBlockId { get; set; }

    public bool IsEditingBlock => EditBlockId is > 0;

    /// <summary>Histology Ref Type selected from the "or Pick" dropdown — legacy Common.vb::HistologyRefType.</summary>
    [BindProperty] public int? HistologyRefType { get; set; }

    /// <summary>Block whose test selections are being saved by the per-row "Manage tests" form.</summary>
    [BindProperty] public int TestBlockId { get; set; }
    [BindProperty] public List<string> SelectedHistologyCodes { get; set; } = [];
    [BindProperty] public List<string> SelectedAntibodyCodes { get; set; } = [];
    [BindProperty] public List<string> SelectedStainCodes { get; set; } = [];

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

    public IReadOnlyList<LookupItem> HistologyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> AntibodyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> StainOptions { get; private set; } = [];
    public IReadOnlyDictionary<int, IReadOnlyList<string>> HistologyCodesByBlockId { get; private set; } = new Dictionary<int, IReadOnlyList<string>>();
    public IReadOnlyDictionary<int, IReadOnlyList<string>> AntibodyCodesByBlockId { get; private set; } = new Dictionary<int, IReadOnlyList<string>>();
    public IReadOnlyDictionary<int, IReadOnlyList<string>> StainCodesByBlockId { get; private set; } = new Dictionary<int, IReadOnlyList<string>>();

    /// <summary>Sender ref keyed by AnimalID — used only in batch-wide mode (no <see cref="AnimalId"/>) to label each row.</summary>
    public IReadOnlyDictionary<int, string> SenderRefsByAnimalId { get; private set; } = new Dictionary<int, string>();

    public string? ErrorMessage { get; private set; }

    /// <summary>Mirrors SampleSummaryModel/SubmissionDetailsModel — hides all block mutation actions
    /// (Add/Edit/Delete/Copy block, Add/Delete tissue, Save details) in the View Submission journey.
    /// Legacy source: SubmissionDetailsBlock.aspx.vb::DisableEnableControls (SV_ViewSubmission branch).</summary>
    public bool IsViewMode => Session.IsViewSubmissionMode;

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Sample Blocks";
        ViewData["PageTitle"] = "Sample Blocks";

        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;

        // Batch-wide mode is "no AnimalId supplied" — not "animal not found", which must fall
        // through to the view's "Sample not found" branch rather than silently showing every block.
        if (AnimalId is null or <= 0)
        {
            // Batch-wide overview (former Blocks/BlockDetails.cshtml behaviour) — every block in the batch.
            Blocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
            var animals = await _submissions.GetAnimalsByBatchAsync(BatchId ?? 0);
            SenderRefsByAnimalId = animals.ToDictionary(a => a.ID, a => a.SenderRef);
            return Page();
        }

        if (Animal is null) return Page();

        PMDate = Animal.PMDate;
        HistologyRef = Animal.HistologyRef;

        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        Blocks = allBlocks.Where(b => b.AnimalID == Animal.ID).ToList();

        await LoadSupportingDataAsync();

        if (EditBlockId is > 0)
        {
            var editing = Blocks.FirstOrDefault(b => b.ID == EditBlockId);
            if (editing is not null)
            {
                NewBlockRef = editing.BlockRef;
                NewCustomerRef = editing.CustomerRef;
                NewRepeatBlock = editing.RepeatBlock;
            }
        }

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

    /// <summary>
    /// Saves changes to an existing block's ref/customer ref/repeat flag.
    /// Legacy source: SubmissionDetailsBlock.aspx.vb::btnEditBlock_Click → BlockDetails.aspx.
    /// Uses the already-existing <see cref="IBlockService.UpdateBlockAsync"/> (EditBlock SP) —
    /// no new stored procedure required.
    /// </summary>
    public async Task<IActionResult> OnPostEditBlockAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        var existing = allBlocks.FirstOrDefault(b => b.ID == EditBlockId);
        if (existing is null || string.IsNullOrWhiteSpace(NewBlockRef))
            return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });

        var updated = new Block
        {
            ID = existing.ID,
            BatchID = existing.BatchID,
            AnimalID = existing.AnimalID,
            BlockRef = NewBlockRef,
            CustomerRef = NewCustomerRef,
            Comment = existing.Comment,
            RepeatBlock = NewRepeatBlock,
            Status = existing.Status,
            Order = existing.Order,
            RowStamp = existing.RowStamp,
        };
        await _blocks.UpdateBlockAsync(updated, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>
    /// Delta-saves this block's Histology/Antibodies/Stain test-type selections.
    /// Uses only the existing Add/Edit/DeleteBlock{Histology,Antibodies,Stain} stored procedures
    /// (confirmed in legacy clsCheckBoxData.vb) — no new stored procedure required.
    /// </summary>
    public async Task<IActionResult> OnPostSaveBlockTestsAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null || TestBlockId <= 0) return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });

        await _blockTests.SaveTestSelectionsAsync(
            BatchId ?? 0, TestBlockId, SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes, Session.UserID);

        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>
    /// "Or Pick" — assigns the next unused histology ref for the selected type.
    /// Legacy source: SubmissionDetailsBlock.aspx.vb::ddlHistologyType_SelectedIndexChanged.
    /// Uses the existing <see cref="IHistologyRefService.GetUnusedRefsAsync"/> — no new stored procedure.
    /// Explicit submit rather than AutoPostBack, per WCAG 3.2.2 (On Input).
    /// </summary>
    public async Task<IActionResult> OnPostGetNextHistologyRefAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        Blocks = allBlocks.Where(b => b.AnimalID == Animal.ID).ToList();
        await LoadSupportingDataAsync();

        PMDate = Animal.PMDate;
        if (HistologyRefType is > 0)
        {
            var unused = await _histologyRefs.GetUnusedRefsAsync(HistologyRefType.Value);
            HistologyRef = unused.FirstOrDefault()?.Ref ?? HistologyRef;
        }

        return Page();
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
        if (batchId is null or <= 0)
        {
            _logger.LogWarning("SubmissionDetailsBlock: no BatchId (route={RouteBatchId}, session={SessionBatchId}) — redirecting to Index.", BatchId, Session.BatchID);
            return RedirectToPage("/Index");
        }

        var forbidden = await CheckBatchAccessAsync(_batches, batchId.Value);
        if (forbidden is not null)
        {
            _logger.LogWarning("SubmissionDetailsBlock: access denied for batch {BatchId} (group={Group}, userArea={UserArea}).", batchId, Session.GroupName, Session.UserAreaID);
            return forbidden;
        }

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

        if (Animal is null)
            _logger.LogWarning("SubmissionDetailsBlock: animal {AnimalId} not found in batch {BatchId} (blockAnimals={BlockCount}).", AnimalId, batchId, blockAnimals.Count);

        // Deliberately does NOT redirect when the animal cannot be resolved: bouncing back to
        // SampleSummary is indistinguishable from "the button did nothing". Leaving Animal null
        // renders the view's "Sample not found" branch so the failure is visible to the user.
        return null;
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

        // Per-block Histology/Antibodies/Stain test selection — mirrors BatchDetailsModel's
        // batch-level equivalent, scoped down to this sample's blocks.
        // Wrapped: this reads a 10-result-set SP, and a shape mismatch must not take the whole page down.
        try
        {
            var antibodyTableId = Batch?.BatchType == BatchTypeConstants.NonTse ? LookupNonTseAntibodies : LookupTseAntibodies;
            HistologyOptions = await _lookups.GetHistologyTypesAsync();
            AntibodyOptions = await _lookups.GetLookupDataAsync(antibodyTableId);
            StainOptions = await _lookups.GetLookupDataAsync(LookupSpecialStain);

            var allTests = await _blockTests.GetByBatchAsync(BatchId ?? 0);
            var histologyByBlock = new Dictionary<int, IReadOnlyList<string>>();
            var antibodyByBlock = new Dictionary<int, IReadOnlyList<string>>();
            var stainByBlock = new Dictionary<int, IReadOnlyList<string>>();
            foreach (var block in Blocks)
            {
                histologyByBlock[block.ID] = allTests.Where(t => t.BlockID == block.ID && t.TestType == BlockTestType.Histology).Select(t => t.Code).ToList();
                antibodyByBlock[block.ID] = allTests.Where(t => t.BlockID == block.ID && t.TestType == BlockTestType.Antibodies).Select(t => t.Code).ToList();
                stainByBlock[block.ID] = allTests.Where(t => t.BlockID == block.ID && t.TestType == BlockTestType.Stain).Select(t => t.Code).ToList();
            }
            HistologyCodesByBlockId = histologyByBlock;
            AntibodyCodesByBlockId = antibodyByBlock;
            StainCodesByBlockId = stainByBlock;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmissionDetailsBlock: failed to load per-block test selections for batch {BatchId}.", BatchId);
        }
    }
}
