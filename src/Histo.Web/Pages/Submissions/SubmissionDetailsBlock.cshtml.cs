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
/// sample (cassetted workflow), reached from SampleSummary's "Edit sample"/"Manage sample".
/// Shows the header (Sender ref/PM date/Histology ref) and the block grid for that one sample.
///
/// The batch-wide "every block in this batch" overview (legacy <c>BatchBlocks.aspx</c>, reached
/// only from BatchDetails' "Assign blocks") lives on the dedicated
/// <see cref="Histo.Web.Pages.Batches.BatchBlocksModel"/> page — the two legacy pages served
/// different journeys and were never the same screen.
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
    /// When omitted, the page redirects to the batch-wide <see cref="Histo.Web.Pages.Batches.BatchBlocksModel"/> overview.
    /// </summary>
    [BindProperty(SupportsGet = true)] public int? AnimalId { get; set; }

    /// <summary>Blocks awaiting delete confirmation — drives the inline GOV.UK confirmation panel (replaces browser confirm()).</summary>
    [BindProperty(SupportsGet = true)] public List<int> ConfirmDeleteBlockIds { get; set; } = [];

    /// <summary>Set when the user has requested the inline "Check used block refs" lookup for this sample.</summary>
    [BindProperty(SupportsGet = true)] public bool ShowUsedRefs { get; set; }

    /// <summary>Histology Ref Type selected from the "or Pick" dropdown — legacy Common.vb::HistologyRefType.</summary>
    [BindProperty] public int? HistologyRefType { get; set; }

    /// <summary>Histology Reference for the sample (NN/NNNNN format). Only postable while unset — see <see cref="HistologyRefLocked"/>.</summary>
    [BindProperty] public string? EditHistologyRef { get; set; }

    /// <summary>Post-mortem date for the sample. Only postable while unset — see <see cref="PMDateLocked"/>.</summary>
    [BindProperty] public string? EditPMDate { get; set; }

    public Animal? Animal { get; private set; }
    public Batch? Batch { get; private set; }
    public IReadOnlyList<Block> Blocks { get; private set; } = [];
    public IReadOnlyList<BlockRefRangeHelpers.BlockRefRangeRow> UsedBlockRefResults { get; private set; } = [];
    public BatchTestSelections? BatchTestSelections { get; private set; }

    /// <summary>True for pre-cassetted submissions, where the block ref must come from the pre-booked list and histology ref is mandatory.</summary>
    public bool IsPreCassetted => Batch?.IsPreCassetted == true;

    /// <summary>True for TSE submissions — shows H&amp;E (BSE)/IHC Prp grid columns instead of IHC Other, matching legacy HideColumns.</summary>
    public bool IsTse => Batch?.BatchType != BatchTypeConstants.NonTse;

    /// <summary>True when at least one batch-level test type (histology, antibodies, or stains) has been selected.</summary>
    public bool ShowTestDetails => BatchTestSelections?.HasAny == true;

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

    public string? ErrorMessage { get; private set; }

    /// <summary>Mirrors SampleSummaryModel/SubmissionDetailsModel — hides all block mutation actions
    /// (Add/Edit/Delete/Copy block, Save details) in the View Submission journey.
    /// Legacy source: SubmissionDetailsBlock.aspx.vb::DisableEnableControls (SV_ViewSubmission branch).</summary>
    public bool IsViewMode => Session.IsViewSubmissionMode;

    /// <summary>Once a Histology Ref has been assigned to this sample it becomes read-only here — matches legacy, which only allows entry while unset.</summary>
    public bool HistologyRefLocked => Animal?.HistoRefSet == true;

    /// <summary>Once a PM Date has been assigned to this sample it becomes read-only here — matches legacy, which only allows entry while unset.</summary>
    public bool PMDateLocked => Animal?.PMDateSet == true;

    // Set by whichever page navigated here (SampleSummary/BatchBlocks/AddSubmission) right before
    // redirecting — falls back to SampleSummary if reached without that breadcrumb (e.g. a stale/direct link).
    public string BackLinkPage => string.IsNullOrWhiteSpace(Session.SampleDetailReturnPage)
        ? $"/Submissions/SampleSummary?batchId={BatchId ?? Session.BatchID ?? 0}"
        : Session.SampleDetailReturnPage;

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Sample Blocks";
        ViewData["PageTitle"] = "Sample Blocks";
        if (TempData["SubmissionDetailsBlock_Error"] is string deleteError) ErrorMessage = deleteError;

        // No AnimalId means the caller wants the batch-wide overview, which now lives on its own page.
        if (AnimalId is null or <= 0)
            return RedirectToPage("/Batches/BatchBlocks", new { batchId = BatchId ?? Session.BatchID });

        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;

        if (Animal is null) return Page();

        EditHistologyRef = Animal.HistologyRef;
        EditPMDate = DateFormatHelpers.ToIsoDate(Animal.PMDate);

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

    /// <summary>
    /// Shows the inline "Are you sure?" confirmation panel for the checked blocks — the
    /// "Delete block" button posted straight to <see cref="OnPostDeleteAsync"/> with no
    /// confirmation step, deleting immediately; this restores the confirm step ahead of it.
    /// </summary>
    public async Task<IActionResult> OnPostConfirmDeleteAsync(List<int>? blockIds)
    {
        ConfirmDeleteBlockIds = blockIds ?? [];
        return await OnGetAsync();
    }

    /// <summary>
    /// Saves a manually-entered Histology Ref/PM Date — only possible while each field is unset
    /// (<see cref="HistologyRefLocked"/>/<see cref="PMDateLocked"/>); once assigned, either here or
    /// via "Assign Tissues to Blocks", they become permanently read-only, matching legacy.
    /// </summary>
    public async Task<IActionResult> OnPostSaveHistologyDetailsAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        // Locked fields can't be changed by a crafted POST — silently keep the existing value
        // rather than trusting the submitted one, mirroring the readonly inputs in the view.
        if (HistologyRefLocked) EditHistologyRef = Animal.HistologyRef;
        if (PMDateLocked) EditPMDate = DateFormatHelpers.ToIsoDate(Animal.PMDate);

        var histoRefError = await ValidateHistologyRefAsync(EditHistologyRef);
        if (histoRefError is not null)
        {
            ErrorMessage = histoRefError;
            var allBlocksForError = await _blocks.GetByBatchAsync(BatchId ?? 0);
            Blocks = allBlocksForError.Where(b => b.AnimalID == Animal.ID).ToList();
            await LoadSupportingDataAsync();
            return Page();
        }

        if (Animal.HistologyRef != EditHistologyRef || Animal.PMDate != DateFormatHelpers.ToLegacyDate(EditPMDate))
        {
            var updated = new Animal
            {
                ID = Animal.ID,
                BatchSubmissionID = Animal.BatchSubmissionID,
                SenderRef = Animal.SenderRef,
                NextBlockRef = Animal.NextBlockRef,
                HistoRefSet = !string.IsNullOrWhiteSpace(EditHistologyRef),
                HistologyRef = EditHistologyRef,
                OnHold = Animal.OnHold,
                PMDate = DateFormatHelpers.ToLegacyDate(EditPMDate),
                PMDateSet = !string.IsNullOrWhiteSpace(EditPMDate),
                IsPGNumber = Animal.IsPGNumber,
                BookedHistologyRef = Animal.BookedHistologyRef,
                RowStamp = Animal.RowStamp,
            };
            await _submissions.UpdateAnimalAsync(updated, Session.UserID);
        }

        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>
    /// Validates Histology Reference format (NN/NNNNN) and business rules — mirrors
    /// <c>Blocks/BlockDetails.cshtml.cs::ValidateHistologyRefAsync</c>. Returns an error message if
    /// invalid, null if valid (including when empty — clearing the field is allowed).
    /// </summary>
    private async Task<string?> ValidateHistologyRefAsync(string? histologyRef)
    {
        if (string.IsNullOrWhiteSpace(histologyRef))
            return null;

        if (!System.Text.RegularExpressions.Regex.IsMatch(histologyRef, @"^\d{2}/\d{5}$"))
            return "Histology Reference must be in NN/NNNNN format (e.g., 26/40004).";

        var yearStr = histologyRef[..2];
        if (!int.TryParse(yearStr, out var year))
            return "Invalid year in Histology Reference.";

        var currentYear = DateTime.Now.Year % 100;
        if (year > currentYear)
            return $"Histology Reference year ({year}) cannot be greater than current year ({currentYear}).";

        var numStr = histologyRef[3..];
        if (!int.TryParse(numStr, out var refNumber))
            return "Invalid Histology Reference number.";

        var histologyType = DetermineHistologyTypeFromRef(refNumber);
        if (histologyType != 0 && !IsPreviousYearHistoRef(year))
        {
            var counters = await _histologyRefs.GetCountersAsync();
            var counter = counters.FirstOrDefault(c => c.Type == histologyType);
            if (counter is not null && int.TryParse(counter.NextHistologyRef, out var nextRef) && refNumber >= nextRef)
                return $"Histology Reference entered ({histologyRef}) must be less than the next available reference number ({counter.NextHistologyRef}) for this type.";
        }

        return null;
    }

    /// <summary>
    /// Determines histology type code from the numeric range of the reference — mirrors
    /// <c>Common.vb::CheckRange</c> (via <c>Blocks/BlockDetails.cshtml.cs::DetermineHistologyTypeFromRef</c>).
    /// Ranges: Neuropath 10000-19999, AbattoirSurvey 20000-29999, TBDiag 30000-39999,
    /// GeneralPool 40000-59999, MouseProjects 60000-89999. Out-of-range (e.g. &lt;10000 or &gt;=90000)
    /// returns 0 — legacy leaves the type unset and skips the next-ref check entirely for these.
    /// </summary>
    private static int DetermineHistologyTypeFromRef(int refNumber)
    {
        if (refNumber is >= 10000 and < 20000) return 1; // Neuropath
        if (refNumber is >= 20000 and < 30000) return 2; // AbattoirSurvey
        if (refNumber is >= 30000 and < 40000) return 3; // TBDiag
        if (refNumber is >= 40000 and < 60000) return 4; // GeneralPool
        if (refNumber is >= 60000 and < 90000) return 5; // MouseProjects
        return 0; // out of range — no counter to check against
    }

    /// <summary>
    /// Legacy source: Common.vb::IsPreviousYearHistoRef (plus IsPre00's Y2K rollover pivot, which
    /// only matters when the current year is '00' or '01' — practically dormant until 2100).
    /// When true, the ref's year is before the current year, so the "must be less than next ref"
    /// check is skipped entirely.
    /// </summary>
    private static bool IsPreviousYearHistoRef(int histoRefYear)
    {
        var currentYear = DateTime.Now.Year % 100;
        if (histoRefYear < currentYear) return true;
        return currentYear is 0 or 1 && histoRefYear is >= 70 and <= 99;
    }

    /// <summary>
    /// Deletes the checked blocks. Legacy source: <c>SubmissionDetailsBlock.aspx.vb</c>::
    /// <c>btnDeleteBlock_Click</c> — reads every checked <c>cbSelected</c> row and deletes each.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(List<int>? blockIds)
    {
        var failedIds = new List<int>();
        if (blockIds is not null)
            foreach (var id in blockIds)
                if (!await _blocks.DeleteBlockAsync(id, Session.UserID))
                    failedIds.Add(id);

        if (failedIds.Count > 0)
            TempData["SubmissionDetailsBlock_Error"] =
                $"Could not delete block(s) {string.Join(", ", failedIds)}. They may still have tissues or test results recorded against them.";

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
            return Page();
        }

        return RedirectToPage("/Blocks/BlockDetails", new { batchId = BatchId, animalId = AnimalId, blockId = blockIds[0] });
    }

    /// <summary>
    /// "Or Pick" — assigns the next unused histology ref for the selected type and saves it
    /// immediately (PM date/Histology ref are read-only display elsewhere on this page, so this
    /// is now the only way to change the Histology ref here — there is no separate Save action).
    /// Legacy source: SubmissionDetailsBlock.aspx.vb::ddlHistologyType_SelectedIndexChanged.
    /// Uses the existing <see cref="IHistologyRefService.GetUnusedRefsAsync"/> — no new stored procedure.
    /// Explicit submit rather than AutoPostBack, per WCAG 3.2.2 (On Input).
    /// </summary>
    public async Task<IActionResult> OnPostGetNextHistologyRefAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        if (HistologyRefType is > 0)
        {
            var unused = await _histologyRefs.GetUnusedRefsAsync(HistologyRefType.Value);
            var nextRef = unused.FirstOrDefault()?.Ref;
            if (nextRef is not null)
            {
                var updated = new Animal
                {
                    ID = Animal.ID,
                    BatchSubmissionID = Animal.BatchSubmissionID,
                    SenderRef = Animal.SenderRef,
                    NextBlockRef = Animal.NextBlockRef,
                    HistoRefSet = true,
                    HistologyRef = nextRef,
                    OnHold = Animal.OnHold,
                    PMDate = Animal.PMDate,
                    PMDateSet = Animal.PMDateSet,
                    IsPGNumber = Animal.IsPGNumber,
                    BookedHistologyRef = Animal.BookedHistologyRef,
                    RowStamp = Animal.RowStamp,
                };
                await _submissions.UpdateAnimalAsync(updated, Session.UserID);
                Animal.HistologyRef = nextRef;
            }
        }

        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        Blocks = allBlocks.Where(b => b.AnimalID == Animal.ID).ToList();
        await LoadSupportingDataAsync();

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

    /// <summary>Loads the batch (for the pre-cassetted flag), per-block tissues, the tissue-code lookup, per-block Histology test-selection indicators, and batch-level test selections.</summary>
    private async Task LoadSupportingDataAsync()
    {
        Batch = await _batches.GetByIdAsync(BatchId ?? 0);
        BatchTestSelections = await _batches.GetBatchTestSelectionsAsync(BatchId ?? 0);
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
