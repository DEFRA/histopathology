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
/// Replaces <c>SubmissionDetailsBlock.aspx</c> — block summary grid for a batch submission's
/// sample (cassetted workflow). When <see cref="AnimalId"/> is supplied (reached from
/// SampleSummary's "Edit sample"), shows the header (Sender ref/PM date/Histology ref) and the
/// block grid for that one sample; when omitted (reached from BatchDetails' "Assign blocks"),
/// shows a read/delete/copy overview of every block in the batch.
///
/// Block creation/editing (ref, customer ref, tissues, per-block test selection) lives on the
/// dedicated <see cref="Histo.Web.Pages.Blocks.BlockDetailsModel"/> page — restoring the legacy
/// split between this grid and <c>BlockDetails.aspx</c> after an earlier consolidation onto this
/// page made it too cluttered to use.
/// </summary>
public class SubmissionDetailsBlockModel : HistoPageModel
{
    private const int LookupTissueCode = 9;

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

    /// <summary>Blocks awaiting delete confirmation — drives the inline GOV.UK confirmation panel (replaces browser confirm()).</summary>
    [BindProperty(SupportsGet = true)] public List<int> ConfirmDeleteBlockIds { get; set; } = [];

    /// <summary>Set when the user has requested the inline "Check used block refs" lookup for this sample.</summary>
    [BindProperty(SupportsGet = true)] public bool ShowUsedRefs { get; set; }

    /// <summary>Histology Ref Type selected from the "or Pick" dropdown — legacy Common.vb::HistologyRefType.</summary>
    [BindProperty] public int? HistologyRefType { get; set; }

    public Animal? Animal { get; private set; }
    public Batch? Batch { get; private set; }
    public IReadOnlyList<Block> Blocks { get; private set; } = [];
    public IReadOnlyList<BlockRefRangeHelpers.BlockRefRangeRow> UsedBlockRefResults { get; private set; } = [];

    /// <summary>True for pre-cassetted submissions, where the block ref must come from the pre-booked list and histology ref is mandatory.</summary>
    public bool IsPreCassetted => Batch?.IsPreCassetted == true;

    /// <summary>True for TSE submissions — shows H&amp;E (BSE)/IHC Prp grid columns instead of IHC Other, matching legacy HideColumns.</summary>
    public bool IsTse => Batch?.BatchType != BatchTypeConstants.NonTse;

    public IReadOnlyDictionary<int, IReadOnlyList<Tissue>> TissuesByBlockId { get; private set; } =
        new Dictionary<int, IReadOnlyList<Tissue>>();

    /// <summary>
    /// Legacy source: clsBatchSummary.vb::CreateAnimalSummaryData — ALL 7 of the grid's boolean
    /// indicator columns (EO/H&amp;E/Special Stain/IHC-PrP/H&amp;E(BSE)/IHC-Other/Archive) are read
    /// from BLOCK_HISTOLOGY child rows (luHistology codes 1-7), confirmed against the database
    /// directly — none of them come from the Antibodies or Stain test-type tables.
    /// </summary>
    public IReadOnlyList<LookupItem> HistologyOptions { get; private set; } = [];
    public IReadOnlyDictionary<int, IReadOnlyList<string>> HistologyCodesByBlockId { get; private set; } = new Dictionary<int, IReadOnlyList<string>>();

    /// <summary>Tissue-code → description lookup, used to render Tissue Details the same way as legacy's LookupDescription().</summary>
    public IReadOnlyList<LookupItem> TissueOptions { get; private set; } = [];

    /// <summary>Resolves a tissue code to its description, matching legacy's LookupDescription(dtTissuesList, TissueCode).</summary>
    public string TissueName(string code) => TissueOptions.FirstOrDefault(o => o.Code == code)?.Name ?? code;

    /// <summary>Sender ref keyed by AnimalID — used only in batch-wide mode (no <see cref="AnimalId"/>) to label each row.</summary>
    public IReadOnlyDictionary<int, string> SenderRefsByAnimalId { get; private set; } = new Dictionary<int, string>();

    public string? ErrorMessage { get; private set; }

    /// <summary>Mirrors SampleSummaryModel/SubmissionDetailsModel — hides all block mutation actions
    /// (Add/Edit/Delete/Copy block, Save details) in the View Submission journey.
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

        PMDate = DateFormatHelpers.ToIsoDate(Animal.PMDate);
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
            PMDate = DateFormatHelpers.ToLegacyDate(PMDate),
            PMDateSet = !string.IsNullOrWhiteSpace(PMDate),
            IsPGNumber = Animal.IsPGNumber,
            BookedHistologyRef = Animal.BookedHistologyRef,
            RowStamp = Animal.RowStamp,
        };

        await _submissions.UpdateAnimalAsync(updated, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>
    /// Deletes the checked blocks. Legacy source: <c>SubmissionDetailsBlock.aspx.vb</c>::
    /// <c>btnDeleteBlock_Click</c> — reads every checked <c>cbSelected</c> row and deletes each.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(List<int>? blockIds)
    {
        if (blockIds is not null)
            foreach (var id in blockIds)
                await _blocks.DeleteBlockAsync(id, Session.UserID);

        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>
    /// Requires exactly one checked block, then redirects to the dedicated Block Details page.
    /// Legacy source: <c>SubmissionDetailsBlock.aspx.vb</c>::<c>btnEditBlock_Click</c> →
    /// <c>BlockDetails.aspx</c>.
    /// </summary>
    public async Task<IActionResult> OnPostEditBlockSelectAsync(List<int>? blockIds)
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        if (blockIds is null || blockIds.Count != 1)
        {
            ErrorMessage = "Select exactly one block to edit.";
            var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
            Blocks = allBlocks.Where(b => b.AnimalID == Animal!.ID).ToList();
            await LoadSupportingDataAsync();
            PMDate = DateFormatHelpers.ToIsoDate(Animal.PMDate);
            HistologyRef = Animal.HistologyRef;
            return Page();
        }

        return RedirectToPage("/Blocks/BlockDetails", new { batchId = BatchId, animalId = AnimalId, blockId = blockIds[0] });
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

        PMDate = DateFormatHelpers.ToIsoDate(Animal.PMDate);
        if (HistologyRefType is > 0)
        {
            var unused = await _histologyRefs.GetUnusedRefsAsync(HistologyRefType.Value);
            HistologyRef = unused.FirstOrDefault()?.Ref ?? HistologyRef;
        }

        return Page();
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
        IReadOnlyList<Animal> plainAnimals = [];
        if (Animal is null)
        {
            plainAnimals = await _submissions.GetAnimalsByBatchAsync(batchId.Value);
            Animal = plainAnimals.FirstOrDefault(a => a.ID == AnimalId);
        }

        if (Animal is null)
            _logger.LogWarning(
                "SubmissionDetailsBlock: animal {AnimalId} not found in batch {BatchId}. " +
                "blockAnimals returned {BlockCount} row(s) [{BlockIds}]; plain GetAnimalsByBatchAsync returned {PlainCount} row(s) [{PlainIds}].",
                AnimalId, batchId,
                blockAnimals.Count, string.Join(",", blockAnimals.Select(a => a.ID)),
                plainAnimals.Count, string.Join(",", plainAnimals.Select(a => a.ID)));

        // Deliberately does NOT redirect when the animal cannot be resolved: bouncing back to
        // SampleSummary is indistinguishable from "the button did nothing". Leaving Animal null
        // renders the view's "Sample not found" branch so the failure is visible to the user.
        return null;
    }

    /// <summary>Loads the batch (for the pre-cassetted flag), per-block tissues, the tissue-code lookup, and per-block Histology test-selection indicators.</summary>
    private async Task LoadSupportingDataAsync()
    {
        Batch = await _batches.GetByIdAsync(BatchId ?? 0);
        TissueOptions = await _lookups.GetLookupDataAsync(LookupTissueCode);

        var allTissues = await _submissions.GetTissuesByBatchAsync(BatchId ?? 0);
        var tissuesByBlockId = new Dictionary<int, IReadOnlyList<Tissue>>();
        foreach (var block in Blocks)
            tissuesByBlockId[block.ID] = allTissues.Where(t => t.OwnerID == block.ID).ToList();
        TissuesByBlockId = tissuesByBlockId;

        // All 7 grid indicator columns (Archive/EO/H&E/H&E-BSE/IHC-PrP/IHC-Other/Special Stain)
        // are Histology test-type codes — see the HistologyOptions doc comment. Wrapped because
        // this reads a 10-result-set SP and a shape mismatch must not take the whole page down.
        try
        {
            HistologyOptions = await _lookups.GetHistologyTypesAsync();

            var allTests = await _blockTests.GetByBatchAsync(BatchId ?? 0);
            var histologyByBlock = new Dictionary<int, IReadOnlyList<string>>();
            foreach (var block in Blocks)
                histologyByBlock[block.ID] = allTests.Where(t => t.BlockID == block.ID && t.TestType == BlockTestType.Histology).Select(t => t.Code).ToList();
            HistologyCodesByBlockId = histologyByBlock;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SubmissionDetailsBlock: failed to load per-block test selections for batch {BatchId}.", BatchId);
        }
    }
}
