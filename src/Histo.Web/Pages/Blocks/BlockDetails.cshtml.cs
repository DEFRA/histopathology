using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Blocks;

/// <summary>
/// Replaces legacy <c>BlockDetails.aspx</c> — the dedicated Add/Edit block page reached from
/// <c>SubmissionDetailsBlock</c>'s "Add block"/"Edit block" actions. Restores the legacy split
/// (block creation/editing on its own page) after the earlier consolidation onto
/// <see cref="Submissions.SubmissionDetailsBlockModel"/> made that page too cluttered.
///
/// A new block is created immediately on Save (rather than staged in memory as legacy's
/// DataSet did), then the page redirects into edit mode for that block so tissues and test
/// selections can be added — matching legacy's flow of creating the block row before the
/// user assigns tissues/tests to it.
/// </summary>
public class BlockDetailsModel : HistoPageModel
{
    private const int LookupTissueCode = 9;
    private const int LookupTseAntibodies = 4;
    private const int LookupNonTseAntibodies = 5;
    private const int LookupSpecialStain = 6;

    private readonly ISubmissionService _submissions;
    private readonly IBlockService _blocks;
    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;
    private readonly IBlockTestService _blockTests;
    private readonly ILogger<BlockDetailsModel> _logger;

    public BlockDetailsModel(ISessionService session, ISubmissionService submissions, IBlockService blocks,
        IBatchService batches, ILookupService lookups, IBlockTestService blockTests,
        ILogger<BlockDetailsModel> logger)
        : base(session)
    {
        _submissions = submissions;
        _blocks = blocks;
        _batches = batches;
        _lookups = lookups;
        _blockTests = blockTests;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }
    [BindProperty(SupportsGet = true)] public int? AnimalId { get; set; }

    /// <summary>Block being edited. Null/0 means Add mode.</summary>
    [BindProperty(SupportsGet = true)] public int? BlockId { get; set; }

    public bool IsEditMode => BlockId is > 0;

    /// <summary>
    /// True while still inside the initial "Add block" workflow. Legacy: BlockDetails.aspx.vb
    /// provisionally creates the block in an in-memory session dataset on page load, so tissues
    /// and tests can be assigned before the user explicitly saves. The migrated page persists
    /// directly to the database, so the block is created for real on first load and this flag
    /// carries the "still adding" title/button-label/Number-of-blocks state across the redirects
    /// used by the tissue/test actions, until the user clicks the final Add/Save button.
    /// </summary>
    [BindProperty(SupportsGet = true)] public bool IsAddFlow { get; set; }

    /// <summary>
    /// True only for a block just created via "Next block" (as opposed to the very first block
    /// of a sample, auto-provisioned by "Add block"). Legacy: ClearControls() resets all three
    /// test checkbox lists to unselected for a block created this way unless "Use these tests
    /// for the next block?" is ticked — this flag lets <see cref="LoadEditModeDataAsync"/> tell
    /// that apart from a genuinely-first block, which instead defaults to the batch-level tests.
    /// </summary>
    [BindProperty(SupportsGet = true)] public bool IsFromNextBlock { get; set; }

    [BindProperty] public string? NewBlockRef { get; set; }
    [BindProperty] public string? NewCustomerRef { get; set; }
    [BindProperty] public bool NewRepeatBlock { get; set; }
    [BindProperty] public string? NewComment { get; set; }

    /// <summary>Add-mode only — creates this many identical blocks with auto-incrementing refs.</summary>
    [BindProperty] public int NewNumberOfBlocks { get; set; } = 1;

    [BindProperty] public string NewTissueCode { get; set; } = string.Empty;
    [BindProperty] public short NewTissueNoPieces { get; set; } = 1;

    /// <summary>Tissue currently shown in its inline edit row.</summary>
    [BindProperty(SupportsGet = true)] public int? EditTissueId { get; set; }

    [BindProperty] public int TissueId { get; set; }
    /// <summary>Inline edit-row fields — separate from the Add-tissue fields above so editing a row doesn't pre-fill the Add form.</summary>
    [BindProperty] public string EditTissueCode { get; set; } = string.Empty;
    [BindProperty] public short EditNoPieces { get; set; } = 1;

    /// <summary>Legacy: chkUseWholeTissueList. Defaults to the full lookup list; the user opts in
    /// to filtering the dropdown down to codes already used on this submission.</summary>
    [BindProperty(SupportsGet = true)] public bool UseWholeTissueList { get; set; } = true;

    /// <summary>
    /// True when reached via the batch-wide "Assign Tissues to Blocks" journey (BatchBlocks) — same
    /// breadcrumb test as <c>SubmissionDetailsBlockModel.IsAssignTissueMode</c>. There the tissue
    /// list is always limited to the submission's own tissues, with no option to widen it.
    /// </summary>
    public bool IsAssignTissueMode => (Session.SampleDetailReturnPage ?? string.Empty)
        .Contains("/Batches/BatchBlocks", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the tissue dropdown shows the entire lookup rather than the submission's tissues.</summary>
    public bool ShowWholeTissueList => UseWholeTissueList && !IsAssignTissueMode;

    [BindProperty] public List<string> SelectedHistologyCodes { get; set; } = [];
    [BindProperty] public List<string> SelectedAntibodyCodes { get; set; } = [];
    [BindProperty] public List<string> SelectedStainCodes { get; set; } = [];

    /// <summary>Legacy: chkbCarryTests — "Use these tests for the next block?", read by the Next Block handler.</summary>
    [BindProperty] public bool CarryTestsToNextBlock { get; set; }

    /// <summary>Histology Reference for the sample (NN/NNNNN format). Stored on Animal record.</summary>
    [BindProperty] public string? EditHistologyRef { get; set; }

    /// <summary>Post-mortem Date for the sample. Stored on Animal record.</summary>
    [BindProperty] public string? EditPMDate { get; set; }

    /// <summary>Once a Histology Ref has been assigned to this sample it becomes read-only here — matches legacy, which only allows entry while unset.</summary>
    public bool HistologyRefLocked => Animal?.HistoRefSet == true;

    /// <summary>Once a PM Date has been assigned to this sample it becomes read-only here — matches legacy, which only allows entry while unset.</summary>
    public bool PMDateLocked => Animal?.PMDateSet == true;

    public Animal? Animal { get; private set; }
    public Batch? Batch { get; private set; }
    public Block? Block { get; private set; }

    /// <summary>
    /// True only for SubmittedAs code "5" (genuinely Pre-Cassetted Tissue) — NOT the same as
    /// Batch.IsPreCassetted (DB Cassetted column, true for any non-Wet-Tissue type). Legacy:
    /// Common.vb::IsBatchPreCassetted. Set in LoadSupportingDataAsync.
    /// </summary>
    public bool IsPreCassetted { get; private set; }

    /// <summary>Mirrors SubmissionDetailsBlockModel.ShowHistologyRefTypePicker's area check.</summary>
    public bool IsHistopathAreaUser =>
        string.Equals(Session.UserArea?.Trim(), "Histopath", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Legacy source: SubmissionDetailsBlock.aspx.vb::Page_Load (HistologyRef1.IsMandatory).
    /// Mirrors SubmissionDetailsBlockModel.HistologyRefRequired — mandatory for Histopath-area
    /// users OR Pre-Cassetted submissions.
    /// </summary>
    public bool HistologyRefRequired => IsHistopathAreaUser || IsPreCassetted;
    public IReadOnlyList<Block> PreBookedBlockRefs { get; private set; } = [];

    public IReadOnlyList<Tissue> Tissues { get; private set; } = [];
    public IReadOnlyList<LookupItem> TissueOptions { get; private set; } = [];

    /// <summary>Full tissue lookup list, always unfiltered — used by the inline "edit tissue" row so the
    /// user can change to any tissue type, not just ones already used on this submission.</summary>
    public IReadOnlyList<LookupItem> EditTissueOptions { get; private set; } = [];

    public IReadOnlyList<LookupItem> HistologyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> AntibodyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> StainOptions { get; private set; } = [];
    public IReadOnlyList<string> ExistingHistologyCodes { get; private set; } = [];
    public IReadOnlyList<string> ExistingAntibodyCodes { get; private set; } = [];
    public IReadOnlyList<string> ExistingStainCodes { get; private set; } = [];

    /// <summary>
    /// True when this submission has at least one batch-level antibody selected — keeps the
    /// Antibodies section visible on every block (ticked or not), matching the explicit
    /// requirement that a new/Next-Block block shows the full option list rather than hiding it
    /// until this block's own IHC-PrP/IHC-Other checkbox happens to be ticked first.
    /// </summary>
    public bool ShowAntibodies => AntibodyOptions.Count > 0;

    /// <summary>Same reasoning as <see cref="ShowAntibodies"/>, for the Special stain section.</summary>
    public bool ShowStains => StainOptions.Count > 0;

    /// <summary>
    /// Legacy: DisplayBatchLevelTests/DisplayBlockLevelTests set chkblAntibodies.Enabled = True
    /// purely from an existing Antibody row being present (independent of whether IHC-PrP/IHC-Other
    /// is also saved); chkblHistology_SelectedIndexChanged separately keeps it enabled live while
    /// IHC-PrP/IHC-Other is ticked. Checking either signal — not just the Histology trigger —
    /// avoids disabling (and, via the next save's delta sync, silently deleting) an Antibody
    /// selection that already exists without its paired trigger code still being selected.
    /// Drives the checkboxes' disabled state only — the section itself stays visible regardless
    /// (see <see cref="ShowAntibodies"/>).
    /// </summary>
    public bool AntibodiesEnabled =>
        ExistingAntibodyCodes.Count > 0 ||
        ExistingHistologyCodes.Contains(HistologyCode.IhcPrp) || ExistingHistologyCodes.Contains(HistologyCode.IhcOther);

    /// <summary>Same reasoning as <see cref="AntibodiesEnabled"/>, for Special stain (legacy: chkblSpecialStain.Enabled).</summary>
    public bool StainsEnabled => ExistingStainCodes.Count > 0 || ExistingHistologyCodes.Contains(HistologyCode.SpecialStain);


    /// <summary>
    /// True when this block already has its own saved test selections (as opposed to a brand-new
    /// block merely pre-checked with the batch-level defaults). Only then do the Tests checkboxes
    /// narrow down to the selected codes — a new/untested block still shows the full option list
    /// so additional tests can be picked for the first time.
    /// </summary>
    public bool HasSavedTests { get; private set; }

    public string? ErrorMessage { get; private set; }

    /// <summary>True when a pre-cassetted submission has no pre-booked block references left for this animal — the view offers a link to book one.</summary>
    public bool NoPreBookedRefsAvailable { get; private set; }

    /// <summary>Legacy: EnableDisableAdditionalRequest — disabled for Wet Tissue/Stained Section/Pre Cassetted submissions (SubmittedAs codes 1/3/5).</summary>
    public bool CanUseAdditionalRequest { get; private set; } = true;

    public bool IsViewMode => Session.IsViewSubmissionMode;

    private async Task SaveBatchDefaultsForNewBlockAsync(int blockId)
    {
        var defaults = await _batches.GetBatchTestSelectionsAsync(BatchId ?? 0);
        if (!defaults.HasAny) return;

        await _blockTests.SaveTestSelectionsAsync(
            BatchId ?? 0,
            blockId,
            defaults.Histology.Select(r => r.Code).ToList(),
            defaults.Antibodies.Select(r => r.Code).ToList(),
            defaults.Stains.Select(r => r.Code).ToList(),
            Session.UserID);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = IsEditMode ? "Edit block" : "Add block";
        ViewData["PageTitle"] = IsEditMode ? "Edit block" : "Add block";

        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return Page();

        // Initialize Histology Reference and PM Date from Animal record
        EditHistologyRef = Animal.HistologyRef;
        EditPMDate = DateFormatHelpers.ToIsoDate(Animal.PMDate);
        await LoadSupportingDataAsync();

        if (IsEditMode)
        {
            if (!await LoadEditModeDataAsync()) return Page();
            RestoreDraft();
        }
        else
        {
            // Legacy: SubmissionDetailsBlock.aspx.vb::ValidateMandatoryFields blocks btnAddBlock_Click
            // when Histology Ref is mandatory (Histopath-area user or Pre-Cassetted) and blank.
            // Enforced here too (not just a disabled button there) since this is a GET the user could
            // reach directly — redirect back with the same GDS error-summary convention that page
            // already uses for delete errors.
            if (HistologyRefRequired && !Animal.HistoRefSet)
            {
                TempData["SubmissionDetailsBlock_Error"] = "Enter a Histology Reference for this sample before adding a block.";
                TempData["SubmissionDetailsBlock_ErrorFieldId"] = "EditHistologyRef";
                return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId = BatchId, animalId = AnimalId });
            }

            // Legacy: BlockDetails.aspx.vb Page_Load -> CreateNewRecord() provisionally creates the
            // block immediately (in an in-memory dataset there; directly in the DB here) so tissues
            // and tests can be assigned from the very first page load, without an explicit Save
            // first. isAddFlow=true carries the "still adding" state across the redirects the
            // tissue/test actions below use to return to this page. Set up-front so the Add-mode
            // labelling (title/button/Number of blocks) is still correct even on the fallback
            // render paths below (e.g. no pre-booked refs left for a pre-cassetted submission).
            IsAddFlow = true;
            var allBatchBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
            var animalBlocks = allBatchBlocks.Where(b => b.AnimalID == Animal.ID).ToList();
            var batchTissues = await _submissions.GetTissuesByBatchAsync(BatchId ?? 0);
            // A block with no tissues was never actually used — exclude it from the "existing
            // refs" the next ref is computed from, so an abandoned/never-finished block's ref
            // becomes available again instead of being permanently skipped over.
            var usedRefs = animalBlocks.Where(b => batchTissues.Any(t => t.OwnerID == b.ID)).Select(b => b.BlockRef)
                .Concat(await GetOtherBatchRefsAsync(Animal.ID));
            if (!IsPreCassetted)
                usedRefs = usedRefs.Concat((await _blocks.GetPreBookedByAnimalAsync(Animal.ID) ?? []).Select(b => b.BlockRef));
            NewBlockRef = IsPreCassetted
                ? FirstPreBookedRef()
                : BlockHelpers.ComputeNextBlockRef(usedRefs, Animal.NextBlockRef);

            if (string.IsNullOrWhiteSpace(NewBlockRef))
            {
                // Pre-cassetted submissions can only create a block from a pre-booked reference
                // (legacy: clsBlock.NewBlock -> GetPreBookedBlock). If none remain for this animal,
                // there is nothing to auto-provision — tell the user why instead of silently
                // rendering a form with no Tissues/Add tissue section and no visible explanation.
                if (IsPreCassetted)
                {
                    ErrorMessage = "No pre-booked block references are available for this animal. Book a block reference for this sender before adding a block.";
                    NoPreBookedRefsAvailable = true;
                }
                else
                {
                    ErrorMessage = "Could not determine the next block reference. Please try again or contact support if the problem continues.";
                }
                return Page();
            }

            // Resume an existing, never-finished (zero-tissue) block with this exact ref instead
            // of creating a duplicate row for it — e.g. one the user backed out of earlier.
            var abandoned = animalBlocks.FirstOrDefault(b =>
                string.Equals(b.BlockRef, NewBlockRef, StringComparison.OrdinalIgnoreCase) && !batchTissues.Any(t => t.OwnerID == b.ID));

            int newId;
            if (abandoned is not null)
            {
                newId = abandoned.ID;
            }
            else if (IsPreCassetted)
            {
                // Claim the matching pre-booked placeholder in place instead of inserting a new
                // row, so it's retired (Status PreBooked -> PreBookedUsed) and never reappears as
                // a "next" ref for this animal's future submissions.
                var existingOrders = allBatchBlocks.Select(b => b.Order).ToList();
                var preBookedToClaim = PreBookedBlockRefs.First(b => string.Equals(b.BlockRef, NewBlockRef, StringComparison.OrdinalIgnoreCase));
                var claimed = await _blocks.ClaimPreBookedBlockAsync(preBookedToClaim, BatchId ?? 0, existingOrders, Session.UserID);
                newId = claimed ? preBookedToClaim.ID : 0;
                if (newId <= 0)
                {
                    ErrorMessage = "Could not create the block. Please try again or contact support if the problem continues.";
                    return Page();
                }
            }
            else
            {
                var existingOrders = allBatchBlocks.Select(b => b.Order).ToList();
                newId = await _blocks.AddBlockAsync(BatchId ?? 0, Animal.ID, NewBlockRef, existingOrders, Session.UserID,
                    customerRef: null, comment: null, repeatBlock: false);
                if (newId <= 0)
                {
                    // AddBlockAsync swallows its own exceptions and logs them, returning 0 on failure —
                    // without this, the page silently re-rendered the "Add block" form with no Tissues/
                    // Add tissue section and no visible error, making a genuine DB failure look like a
                    // missing feature. Surface it so the real cause shows up instead of a blank result.
                    ErrorMessage = "Could not create the block. Please try again or contact support if the problem continues.";
                    return Page();
                }

                try
                {
                    await SaveBatchDefaultsForNewBlockAsync(newId);
                }
                catch (Exception ex)
                {
                    // The block is already committed; without cleanup a retry would leave an unreachable orphan.
                    _logger.LogError(ex, "BlockDetails: failed to save batch default tests for new block {BlockId}; removing it.", newId);
                    try { await _blockTests.SaveTestSelectionsAsync(BatchId ?? 0, newId, [], [], [], Session.UserID); }
                    catch (Exception clearEx) { _logger.LogError(clearEx, "BlockDetails: failed to clear partial test selections for block {BlockId}.", newId); }
                    await _blocks.DeleteBlockAsync(newId, Session.UserID);
                    ErrorMessage = "Could not create the block. Please try again or contact support if the problem continues.";
                    return Page();
                }
            }

            return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = newId, isAddFlow = true });
        }

        return Page();
    }

    /// <summary>
    /// Legacy: btnCancel_Click — for a block still in the add flow (never reached a successful
    /// "Done"), Back discards it entirely rather than leaving an empty provisional row behind.
    /// IsAddFlow reliably means "Done has never succeeded for this exact block": OnPostDoneAsync's
    /// success redirect never carries isAddFlow forward, so it's always false again by the next
    /// load once Done has gone through. That means anything added to this block while still here —
    /// tissues, carried-over test selections from "Next block" — was never actually confirmed
    /// either, matching legacy (nothing persists until Done validates) — so it's safe to clear them
    /// first, then delete the block itself, rather than leaving an incomplete, never-"Done" block
    /// (and its tissues) behind just because the FK-guarded delete alone would otherwise no-op.
    /// </summary>
    public async Task<IActionResult> OnPostCancelAsync()
    {
        if (IsAddFlow && BlockId is > 0)
        {
            var tissues = await _submissions.GetTissuesByBlockAsync(BatchId ?? 0, BlockId.Value);
            Console.WriteLine($"[DIAG OnPostCancelAsync] Deleting {tissues.Count} tissue(s) and block {BlockId}.");
            foreach (var tissue in tissues)
                await _submissions.DeleteTissueAsync(tissue.ID, TissueOwner.Block, Session.UserID);
            try
            {
                // BlockTestService.SaveTestSelectionsAsync re-throws on failure (already logged
                // there) — unlike the other cleanup calls here, which swallow their own errors.
                // Cancel must still always complete and redirect, so catch this one explicitly.
                await _blockTests.SaveTestSelectionsAsync(BatchId ?? 0, BlockId.Value, [], [], [], Session.UserID);
            }
            catch (Exception)
            {
                // Already logged inside SaveTestSelectionsAsync — nothing more to do here.
            }
            await _blocks.DeleteBlockAsync(BlockId.Value, Session.UserID);
        }

        return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>Saves the block (ref/customer ref/repeat/comment) and its test selections together — replaces the former separate Save block/Save tests actions.</summary>
    public async Task<IActionResult> OnPostDoneAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        // Legacy txtHistologyRef/PM date are display-only here (Enabled = False) — they're set via
        // the sample screens. Never trust a posted value, and never write them back from this page.
        EditHistologyRef = Animal.HistologyRef;
        EditPMDate = DateFormatHelpers.ToIsoDate(Animal.PMDate);

        // LoadEditModeDataAsync (called from every error branch below, to re-populate the rest of
        // the page for redisplay) overwrites NewBlockRef/NewCustomerRef/NewRepeatBlock/NewComment
        // from the saved Block row — capture what was actually posted first so it can be restored
        // afterwards, or a validation failure unrelated to these fields (e.g. missing tissue) would
        // silently revert the user's in-progress edits to them.
        var postedBlockRef = NewBlockRef;
        var postedCustomerRef = NewCustomerRef;
        var postedRepeatBlock = NewRepeatBlock;
        var postedComment = NewComment;

        // Validate test selections up front, so nothing is saved at all if they're invalid.
        var testsError = ValidateTestSelections(SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes);
        if (testsError is not null)
        {
            ErrorMessage = testsError;
            await LoadSupportingDataAsync();
            await LoadEditModeDataAsync();
            // Must run AFTER LoadEditModeDataAsync — it loads ExistingXxxCodes/NewBlockRef etc. from
            // the DB/batch defaults, which would otherwise silently overwrite the user's just-posted
            // (rejected) values and show stale fields instead of what they actually submitted.
            ExistingHistologyCodes = SelectedHistologyCodes;
            ExistingAntibodyCodes = SelectedAntibodyCodes;
            ExistingStainCodes = SelectedStainCodes;
            NewBlockRef = postedBlockRef;
            NewCustomerRef = postedCustomerRef;
            NewRepeatBlock = postedRepeatBlock;
            NewComment = postedComment;
            return Page();
        }

        await LoadSupportingDataAsync();
        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);

        // Legacy btnDone_Click: UpdateBlockDetails() then CreateMultiBlocks(), for a freshly added
        // block and an existing one being edited alike — "Number of blocks" applies to both. In the
        // add flow block #1 already exists (auto-created on first load so tissues/tests could be
        // assigned immediately), so this updates that record then creates count-1 more siblings.
        var current = allBlocks.FirstOrDefault(b => b.ID == BlockId);
        if (current is null || string.IsNullOrWhiteSpace(NewBlockRef))
            return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });

        // Defense-in-depth for the same rule "Add block" already enforces on OnGetAsync — only for
        // the brand-new-block creation sequence, never when editing an existing/already-completed
        // block (legacy's own ValidateMandatoryFields never checks Histology Ref at all, only Block
        // Ref/Number of blocks, so this must not become a stricter-than-legacy rule for edits).
        if (IsAddFlow && HistologyRefRequired && !Animal.HistoRefSet)
        {
            ErrorMessage = "Enter a Histology Reference for this sample before completing this block.";
            await LoadEditModeDataAsync();
            // Must run AFTER LoadEditModeDataAsync — see the testsError branch's identical comment above.
            ExistingHistologyCodes = SelectedHistologyCodes;
            ExistingAntibodyCodes = SelectedAntibodyCodes;
            ExistingStainCodes = SelectedStainCodes;
            NewBlockRef = postedBlockRef;
            NewCustomerRef = postedCustomerRef;
            NewRepeatBlock = postedRepeatBlock;
            NewComment = postedComment;
            return Page();
        }

        var saveError = await SaveCurrentBlockDetailsAsync(current, allBlocks, Animal);
        if (saveError is not null)
        {
            ErrorMessage = saveError;
            await LoadEditModeDataAsync();
            // Must run AFTER LoadEditModeDataAsync — see the testsError branch's identical comment above.
            ExistingHistologyCodes = SelectedHistologyCodes;
            ExistingAntibodyCodes = SelectedAntibodyCodes;
            ExistingStainCodes = SelectedStainCodes;
            NewBlockRef = postedBlockRef;
            NewCustomerRef = postedCustomerRef;
            NewRepeatBlock = postedRepeatBlock;
            NewComment = postedComment;
            return Page();
        }

        // Legacy btnDone_Click always returns to SubmissionDetailsBlock.aspx, regardless of
        // "Number of blocks" — a freshly-saved single block is no more "still being added" than a
        // batch of siblings, so there's no reason to stay on this page only for that case.
        return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId = BatchId, animalId = AnimalId });
    }

    /// <summary>
    /// Legacy: ValidateRequiredData() + UpdateBlockDetails() + CreateMultiBlocks() — shared by
    /// Done and Next Block, which both commit the current block's ref/customer ref/repeat/
    /// comment/tests (and "Number of blocks" duplicate siblings) before moving on to whatever
    /// they do next. Returns an error message to show the user, or null on success.
    /// </summary>
    private async Task<string?> SaveCurrentBlockDetailsAsync(Block current, IReadOnlyList<Block> allBlocks, Animal animal)
    {
        // A block with nothing assigned to it isn't a usable result — require at least one
        // tissue (unless it's a repeat block, legacy: "If chkRepeatBlock.Checked = False Then
        // ... dtView.Count = 0" exemption) before either Done or Next Block can proceed (also
        // protects the "Number of blocks > 1" duplication below, which would otherwise just
        // stamp out several equally-empty siblings).
        var currentTissues = await _submissions.GetTissuesByBlockAsync(BatchId ?? 0, current.ID);
        if (currentTissues.Count == 0 && !NewRepeatBlock)
            return "Add at least one tissue to this block before completing it.";

        var count = Math.Max(1, NewNumberOfBlocks);
        var refChanged = !string.Equals(NewBlockRef, current.BlockRef, StringComparison.OrdinalIgnoreCase);

        // Legacy only re-checks the pre-booked list when the ref actually changes (ValidateRequiredData's
        // "If Not sOriginalBlockRef = txtBlockReference.Text()" guard) — an unchanged ref is already
        // allocated to this block and so no longer appears among the free pre-booked refs.
        var preBookedIndex = -1;
        if (IsPreCassetted && (count > 1 || refChanged))
        {
            var preBookedError = ValidatePreBookedStart(count, out preBookedIndex);
            if (preBookedError is not null) return preBookedError;
        }

        if (!CanUseAdditionalRequest) NewRepeatBlock = false;

        // Legacy: ValidateRequiredData "You can only enter a used Block Ref if you tick the additional
        // request box" — checked against THIS submission's blocks only. A ref the sample holds on another
        // submission may be entered again (re-using existing blocks), exactly as legacy allowed.
        if (!NewRepeatBlock
            && allBlocks.Any(b => b.ID != current.ID && b.AnimalID == current.AnimalID && b.BlockRef == NewBlockRef))
        {
            return "This block reference has already been used for this sample on this submission.";
        }

        await _blocks.UpdateBlockAsync(new Block
        {
            ID = current.ID,
            BatchID = current.BatchID,
            AnimalID = current.AnimalID,
            BlockRef = NewBlockRef!,
            CustomerRef = NewCustomerRef,
            Comment = NewComment,
            RepeatBlock = NewRepeatBlock,
            Status = current.Status,
            Order = current.Order,
            RowStamp = current.RowStamp,
        }, Session.UserID);
        await _blockTests.SaveTestSelectionsAsync(
            BatchId ?? 0, current.ID, SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes, Session.UserID);

        if (count > 1)
            await CreateSiblingBlocksAsync(current, allBlocks, animal, currentTissues, count, preBookedIndex);

        return null;
    }

    private string? ValidatePreBookedStart(int count, out int preBookedIndex)
    {
        var preBookedRefs = PreBookedBlockRefs.Select(b => b.BlockRef).ToList();
        preBookedIndex = preBookedRefs.FindIndex(r => r == NewBlockRef);
        if (preBookedIndex < 0)
            return "Select one of the pre-booked block references for this pre-cassetted submission.";
        if (preBookedIndex + count > preBookedRefs.Count)
            return $"Only {preBookedRefs.Count - preBookedIndex} pre-booked block reference(s) are available from this starting point.";
        return null;
    }

    private async Task CreateSiblingBlocksAsync(
        Block current, IReadOnlyList<Block> allBlocks, Animal animal, IReadOnlyList<Tissue> sourceTissues, int count, int preBookedIndex)
    {
        var existingOrders = allBlocks.Select(b => b.Order).ToList();
        // Legacy CreateMultiBlocks: count on from the entered ref, skipping refs this sample already has
        // on this submission and its pre-booked refs — refs from other submissions are re-used.
        var existingRefs = allBlocks.Where(b => b.ID != current.ID && b.AnimalID == current.AnimalID).Select(b => b.BlockRef).ToList();
        existingRefs.Add(NewBlockRef!);
        if (!IsPreCassetted)
            existingRefs.AddRange((await _blocks.GetPreBookedByAnimalAsync(animal.ID) ?? []).Select(b => b.BlockRef));
        var preBookedBlocksForCreate = IsPreCassetted ? PreBookedBlockRefs.ToList() : null;
        var blockRef = NewBlockRef!;

        for (var i = 1; i < count; i++)
        {
            int siblingId;
            if (IsPreCassetted)
            {
                // Pre-cassetted blocks must claim the next pre-booked ref in place, not the
                // free-text auto-increment scheme — same reasoning as the Add-flow claim above.
                var toClaim = preBookedBlocksForCreate![preBookedIndex + i];
                blockRef = toClaim.BlockRef;
                var claimed = await _blocks.ClaimPreBookedBlockAsync(toClaim, BatchId ?? 0, existingOrders, Session.UserID,
                    new BlockOptions(NewCustomerRef, NewComment, NewRepeatBlock));
                siblingId = claimed ? toClaim.ID : 0;
            }
            else
            {
                blockRef = BlockHelpers.ComputeNextBlockRef(existingRefs, null, blockRef);
                siblingId = await _blocks.AddBlockKeepingRefAsync(
                    BatchId ?? 0, current.AnimalID, blockRef, existingOrders, Session.UserID,
                    new BlockOptions(NewCustomerRef, NewComment, NewRepeatBlock));
            }

            if (siblingId > 0)
                await CopyTissuesAndTestsToSiblingAsync(siblingId, sourceTissues);

            existingRefs.Add(blockRef);
            existingOrders.Add(BlockHelpers.ComputeNextOrder(existingOrders));
        }
    }

    // Legacy CreateMultiBlocks duplicates the source block's tissues and tests into every
    // sibling — the "blocks containing the same tissues and tests" the field describes.
    private async Task CopyTissuesAndTestsToSiblingAsync(int siblingId, IReadOnlyList<Tissue> sourceTissues)
    {
        foreach (var t in sourceTissues)
        {
            await _submissions.AddTissueAsync(new Tissue
            {
                OwnerID = siblingId,
                Owner = TissueOwner.Block,
                TissueCode = t.TissueCode,
                NoPieces = t.NoPieces,
                Comment = t.Comment,
            }, Session.UserID);
        }

        await _blockTests.SaveTestSelectionsAsync(
            BatchId ?? 0, siblingId, SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes, Session.UserID);
    }

    public async Task<IActionResult> OnPostAddTissueAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null || BlockId is not > 0) return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });

        if (!string.IsNullOrWhiteSpace(NewTissueCode))
        {
            var tissue = new Tissue
            {
                OwnerID = BlockId.Value,
                Owner = TissueOwner.Block,
                TissueCode = NewTissueCode,
                NoPieces = NewTissueNoPieces,
            };
            await _submissions.AddTissueAsync(tissue, Session.UserID);
            await _batches.RefreshAllTissuesAssignedAsync(BatchId ?? 0, Session.UserID);
        }

        StashDraft();
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId, isAddFlow = IsAddFlow, isFromNextBlock = IsFromNextBlock, useWholeTissueList = UseWholeTissueList });
    }

    public async Task<IActionResult> OnPostDeleteTissueAsync(int tissueId)
    {
        await _submissions.DeleteTissueAsync(tissueId, TissueOwner.Block, Session.UserID);
        await _batches.RefreshAllTissuesAssignedAsync(BatchId ?? 0, Session.UserID);
        StashDraft();
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId, isAddFlow = IsAddFlow, isFromNextBlock = IsFromNextBlock, useWholeTissueList = UseWholeTissueList });
    }

    private const string DraftKey = "BlockDetails_Draft";

    /// <summary>
    /// Set by the page script when it copies the typed block fields into a tissue form. Needed because an
    /// emptied Block ref posts as an empty string and binds to null, which looks the same as "not posted".
    /// </summary>
    [BindProperty] public bool BlockFieldsPosted { get; set; }

    private sealed record BlockDraft(
        int BlockId, string? BlockRef, int NumberOfBlocks, string? CustomerRef, bool RepeatBlock, string? Comment,
        List<string> Histology, List<string> Antibodies, List<string> Stains, bool CarryTests);

    /// <summary>
    /// Keeps the block fields the user has typed but not saved across a tissue action's redirect.
    /// Skipped when none were posted (no JavaScript), so the saved values are shown as before.
    /// </summary>
    private void StashDraft()
    {
        if (BlockId is not > 0 || !BlockFieldsPosted) return;

        TempData[DraftKey] = System.Text.Json.JsonSerializer.Serialize(new BlockDraft(
            BlockId.Value, NewBlockRef, NewNumberOfBlocks, NewCustomerRef, NewRepeatBlock, NewComment,
            SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes, CarryTestsToNextBlock));
    }

    private void RestoreDraft()
    {
        if (TempData[DraftKey] is not string json) return;

        BlockDraft? draft;
        try { draft = System.Text.Json.JsonSerializer.Deserialize<BlockDraft>(json); }
        catch (System.Text.Json.JsonException) { return; }
        if (draft is null || draft.BlockId != BlockId) return;

        NewBlockRef = draft.BlockRef;
        NewNumberOfBlocks = Math.Max(1, draft.NumberOfBlocks);
        NewCustomerRef = draft.CustomerRef;
        NewRepeatBlock = draft.RepeatBlock;
        NewComment = draft.Comment;
        ExistingHistologyCodes = draft.Histology;
        ExistingAntibodyCodes = draft.Antibodies;
        ExistingStainCodes = draft.Stains;
        CarryTestsToNextBlock = draft.CarryTests;
    }

    public async Task<IActionResult> OnPostUpdateTissueAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null || BlockId is not > 0 || TissueId <= 0)
            return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });

        var existing = (await _submissions.GetTissuesByBlockAsync(BatchId ?? 0, BlockId.Value))
            .FirstOrDefault(t => t.ID == TissueId);
        if (existing is null || string.IsNullOrWhiteSpace(EditTissueCode))
            return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });

        var updated = new Tissue
        {
            ID = TissueId,
            OwnerID = existing.OwnerID,
            Owner = TissueOwner.Block,
            TissueCode = EditTissueCode,
            NoPieces = EditNoPieces,
            // No tissue-level Comment field on this screen (legacy BlockDetails.aspx has none) —
            // preserve whatever value already exists rather than erasing it.
            Comment = existing.Comment,
            RowStamp = existing.RowStamp,
        };
        await _submissions.UpdateTissueAsync(updated, Session.UserID);
        await _batches.RefreshAllTissuesAssignedAsync(BatchId ?? 0, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId, isAddFlow = IsAddFlow, isFromNextBlock = IsFromNextBlock, useWholeTissueList = UseWholeTissueList });
    }

    /// <summary>
    /// Legacy: btnAddBlock_Click on BlockDetails.aspx (button text "Next Block") — saves this
    /// block's tests, then creates a new block for the same animal and continues editing it.
    /// If <see cref="CarryTestsToNextBlock"/> is checked, the same test selections are saved
    /// against the new block too (legacy's ClearControls only clears the checkbox lists when
    /// this is unchecked).
    /// </summary>
    public async Task<IActionResult> OnPostNextBlockAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null || BlockId is not > 0) return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });

        // Loads Batch (IsPreCassetted depends on it) — without this, the pre-booked-ref branch
        // below was always unreachable and every "Next block" silently fell through to the
        // free-text auto-increment scheme, even for pre-cassetted submissions.
        await LoadSupportingDataAsync();

        // LoadEditModeDataAsync (called from every error branch below) overwrites these from the
        // saved Block row — see OnPostDoneAsync's identical comment for why they must be restored.
        var postedBlockRef = NewBlockRef;
        var postedCustomerRef = NewCustomerRef;
        var postedRepeatBlock = NewRepeatBlock;
        var postedComment = NewComment;

        var error = ValidateTestSelections(SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes);
        if (error is not null)
            return await RenderEditErrorAsync(error, postedBlockRef, postedCustomerRef, postedRepeatBlock, postedComment);

        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        var current = allBlocks.FirstOrDefault(b => b.ID == BlockId);
        if (current is null || string.IsNullOrWhiteSpace(NewBlockRef))
            return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId, isAddFlow = IsAddFlow, isFromNextBlock = IsFromNextBlock });

        // Legacy btnAddBlock_Click calls the exact same ValidateRequiredData/UpdateBlockDetails/
        // CreateMultiBlocks sequence as btnDone_Click before creating the next block — this is
        // what persists Block Ref/Customer Ref/Repeat/Comment edits (and "Number of blocks"
        // siblings) made on the current block via "Next block", not just via "Done".
        var saveError = await SaveCurrentBlockDetailsAsync(current, allBlocks, Animal);
        if (saveError is not null)
            return await RenderEditErrorAsync(saveError, postedBlockRef, postedCustomerRef, postedRepeatBlock, postedComment);

        // SaveCurrentBlockDetailsAsync may have inserted sibling blocks ("Number of blocks" > 1) —
        // re-fetch so the next-ref computation and existingOrders below account for them.
        allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        var existingOrders = allBlocks.Select(b => b.Order).ToList();
        var newBlockId = await CreateNextBlockAsync(Animal, allBlocks, existingOrders);
        if (newBlockId is null)
            return await RenderEditErrorAsync("There are no more pre-booked block references available for this sample.",
                postedBlockRef, postedCustomerRef, postedRepeatBlock, postedComment);

        if (CarryTestsToNextBlock && newBlockId > 0)
            await _blockTests.SaveTestSelectionsAsync(
                BatchId ?? 0, newBlockId.Value, SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes, Session.UserID);

        // isAddFlow must be true for the new block too — otherwise Back renders as a plain nav
        // link instead of the Cancel-and-delete button, leaving this not-yet-"Done" block orphaned.
        // isFromNextBlock=true tells LoadEditModeDataAsync not to fall back to the batch-level test
        // defaults for it — it must start blank unless CarryTestsToNextBlock already saved codes.
        return newBlockId > 0
            ? RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = newBlockId, isAddFlow = true, isFromNextBlock = true })
            : RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId, isAddFlow = IsAddFlow, isFromNextBlock = IsFromNextBlock });
    }

    /// <summary>Re-renders the edit page with an error, restoring the values the user posted (LoadEditModeDataAsync overwrites them from the saved Block row).</summary>
    private async Task<IActionResult> RenderEditErrorAsync(
        string message, string? postedBlockRef, string? postedCustomerRef, bool postedRepeatBlock, string? postedComment)
    {
        ErrorMessage = message;
        await LoadEditModeDataAsync();
        // Must run AFTER LoadEditModeDataAsync — see OnPostDoneAsync's identical comment.
        ExistingHistologyCodes = SelectedHistologyCodes;
        ExistingAntibodyCodes = SelectedAntibodyCodes;
        ExistingStainCodes = SelectedStainCodes;
        NewBlockRef = postedBlockRef;
        NewCustomerRef = postedCustomerRef;
        NewRepeatBlock = postedRepeatBlock;
        NewComment = postedComment;
        return Page();
    }

    /// <summary>Claims or creates the block that "Next block" moves on to. Returns null when a pre-cassetted sample has no pre-booked ref left.</summary>
    private async Task<int?> CreateNextBlockAsync(Animal animal, IReadOnlyList<Block> allBlocks, List<int> existingOrders)
    {
        if (IsPreCassetted)
        {
            // Status PreBookedUsed (3) refs are already claimed into a real block elsewhere —
            // only Status PreBooked (2) is genuinely available to claim here.
            var preBooked = (await _blocks.GetPreBookedByAnimalAsync(animal.ID) ?? []).Where(b => b.Status == BlockStatus.PreBooked).ToList();
            if (preBooked.Count == 0) return null;

            // Claim the placeholder in place instead of inserting a new row — see OnGetAsync.
            var claimed = await _blocks.ClaimPreBookedBlockAsync(preBooked[0], BatchId ?? 0, existingOrders, Session.UserID);
            return claimed ? preBooked[0].ID : 0;
        }

        var animalBlocks = allBlocks.Where(b => b.AnimalID == animal.ID).ToList();
        var batchTissues = await _submissions.GetTissuesByBatchAsync(BatchId ?? 0);
        // Same "never actually used" exclusion as OnGetAsync's Add-block flow — without this,
        // a Next Block the user never finished (backed out of some other way than the Cancel
        // button) permanently reserves its ref, so every later "Next block" click skips further
        // and further past it (e.g. abandoned 02/03 -> jumps straight to 04 instead of 02).
        var usedRefs = animalBlocks.Where(b => batchTissues.Any(t => t.OwnerID == b.ID)).Select(b => b.BlockRef)
            .Concat(await GetOtherBatchRefsAsync(animal.ID))
            .Concat((await _blocks.GetPreBookedByAnimalAsync(animal.ID) ?? []).Select(b => b.BlockRef));
        var nextRef = BlockHelpers.ComputeNextBlockRef(usedRefs, animal.NextBlockRef,
            allBlocks.FirstOrDefault(b => b.ID == BlockId)?.BlockRef);

        // Resume an existing, never-finished (zero-tissue) block with this exact ref instead of
        // inserting a duplicate row for it — same pattern as OnGetAsync.
        var abandoned = animalBlocks.FirstOrDefault(b =>
            string.Equals(b.BlockRef, nextRef, StringComparison.OrdinalIgnoreCase) && !batchTissues.Any(t => t.OwnerID == b.ID));
        if (abandoned is not null) return abandoned.ID;

        return await _blocks.AddBlockAsync(BatchId ?? 0, animal.ID, nextRef, existingOrders, Session.UserID,
            customerRef: null, comment: null, repeatBlock: false);
    }

    /// <summary>
    /// Legacy: chkblHistology_SelectedIndexChanged + ValidateRequiredData's static checks —
    /// EO/Archive are mutually exclusive with every other Histology code; Special Stain requires
    /// at least one stain; IHC-PrP/IHC-Other require at least one antibody.
    /// </summary>
    private static string? ValidateTestSelections(List<string> histologyCodes, List<string> antibodyCodes, List<string> stainCodes)
    {
        if (histologyCodes.Count == 0)
            return "Select at least one histology test for this block.";
        if (histologyCodes.Contains(HistologyCode.EO) && histologyCodes.Count > 1)
            return "EO selected — no other tests can be selected.";
        if (histologyCodes.Contains(HistologyCode.Archive) && histologyCodes.Count > 1)
            return "Archive selected — no other tests can be selected.";
        if (histologyCodes.Contains(HistologyCode.SpecialStain) && stainCodes.Count == 0)
            return "Special stain selected — at least one stain must be selected.";
        if ((histologyCodes.Contains(HistologyCode.IhcPrp) || histologyCodes.Contains(HistologyCode.IhcOther)) && antibodyCodes.Count == 0)
            return "IHC selected — at least one antibody test must be selected.";
        return null;
    }

    private string? FirstPreBookedRef() =>
        PreBookedBlockRefs.Count > 0 ? PreBookedBlockRefs[0].BlockRef : null;

    private async Task<IReadOnlyList<string>> GetOtherBatchRefsAsync(int animalId) =>
        await _blocks.GetBlockRefsOnOtherBatchesAsync(animalId, BatchId ?? 0) ?? [];

    private async Task LoadSupportingDataAsync()
    {
        Batch = await _batches.GetByIdAsync(BatchId ?? 0);

        var submittedAsCode = await _batches.GetSubmittedAsCodeAsync(BatchId ?? 0);
        IsPreCassetted = Histo.Core.Domain.ValidationHelpers.IsBatchPreCassetted(submittedAsCode);
        // Legacy: EnableDisableAdditionalRequest — disabled for Wet Tissue(1)/Stained Section(3)/Pre Cassetted(5).
        CanUseAdditionalRequest = submittedAsCode is not ("1" or "3" or "5");

        var fullTissueList = await _lookups.GetLookupDataAsync(LookupTissueCode);
        EditTissueOptions = fullTissueList;
        TissueOptions = ShowWholeTissueList || Animal is null
            ? fullTissueList
            : await GetSampleTissueOptionsAsync(fullTissueList, Animal);

        // Loaded for the initial provisioning request (BlockId not yet assigned) and for every
        // re-render while still mid add-flow — true edits of an already-established block never need it.
        // GetAnimalPreBookedBlocks returns Status PreBooked (2) AND PreBookedUsed (3) — the latter
        // are refs already claimed into a real block elsewhere, so they must be excluded here or a
        // already-claimed ref would be offered/re-claimed again (stealing it from its real block).
        if (IsPreCassetted && (!IsEditMode || IsAddFlow))
            PreBookedBlockRefs = (await _blocks.GetPreBookedByAnimalAsync(Animal?.ID ?? AnimalId ?? 0) ?? [])
                .Where(b => b.Status == BlockStatus.PreBooked).ToList();
        if (IsEditMode)
            await LoadTestOptionsAsync();
    }

    private async Task<IReadOnlyList<LookupItem>> GetSampleTissueOptionsAsync(IReadOnlyList<LookupItem> fullTissueList, Animal animal)
    {
        // Legacy: LoadLookupTypeList with chkUseWholeTissueList checked — the tissue types
        // recorded against this SAMPLE, i.e. the ones waiting to be assigned to its blocks.
        // Legacy source: BlockDetails.aspx.vb::LoadLookupTypeList ->
        // clsTissue.GetBatchAnimalTissues(BatchID, AnimalID) -> SP GetBatchSampleTissues.
        var submissions = await _submissions.GetSubmissionsByBatchAsync(BatchId ?? 0);

        var bySubmission = (await _submissions.GetBatchSubmissionTissuesAsync(BatchId ?? 0))
            .GroupBy(t => t.OwnerID)
            .ToDictionary(g => g.Key, g => g.Select(t => t.TissueCode).ToList());

        // Only this sample's OWN submission rows count (all of them, if it has several). The animal
        // SPs never return BatchSubmissionID (always 0) and every batch starts with an empty default
        // submission (AnimalID 0, Order 1), so there is deliberately no fallback to the batch's first
        // submission or an unkeyed group: a sample with no row of its own must offer nothing rather
        // than another sample's tissues.
        var ownSubmissionIds = submissions
            .Where(s => s.AnimalID == animal.ID || (animal.BatchSubmissionID > 0 && s.ID == animal.BatchSubmissionID))
            .Select(s => s.ID);
        var sampleCodes = ownSubmissionIds
            .SelectMany(id => bySubmission.GetValueOrDefault(id) ?? [])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Anything already assigned to this sample's blocks must stay selectable even if it
        // isn't (or is no longer) on the submission's own tissue list — otherwise a tissue
        // visible in the table above can't be picked again for another block.
        var animalBlockIds = (await _blocks.GetByBatchAsync(BatchId ?? 0))
            .Where(b => b.AnimalID == animal.ID)
            .Select(b => b.ID)
            .ToHashSet();
        sampleCodes.UnionWith((await _submissions.GetTissuesByBatchAsync(BatchId ?? 0))
            .Where(t => animalBlockIds.Contains(t.OwnerID))
            .Select(t => t.TissueCode));

        return fullTissueList.Where(o => o.Code is not null && sampleCodes.Contains(o.Code)).ToList();
    }

    /// <summary>Loads the current block, its tissues, and existing test-selection codes for edit mode. Returns false if the block no longer exists.</summary>
    private async Task<bool> LoadEditModeDataAsync()
    {
        await LoadSupportingDataAsync();
        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        Block = allBlocks.FirstOrDefault(b => b.ID == BlockId);
        if (Block is null) return false;

        NewBlockRef = Block.BlockRef;
        NewCustomerRef = Block.CustomerRef;
        NewRepeatBlock = Block.RepeatBlock;
        NewComment = Block.Comment;

        // Populate Histology Reference and PM Date from Animal record
        EditHistologyRef = Animal?.HistologyRef;
        EditPMDate = Animal?.PMDate;

        Tissues = await _submissions.GetTissuesByBlockAsync(Block.BatchID, Block.ID);

        if (EditTissueId is > 0)
        {
            var editing = Tissues.FirstOrDefault(t => t.ID == EditTissueId);
            if (editing is not null)
            {
                TissueId = editing.ID;
                EditTissueCode = editing.TissueCode;
                EditNoPieces = editing.NoPieces;
            }
        }

        // GetByBatchAsync excludes Special Stain/IHC-PrP/IHC-Other (worklist-only exclusion) — the
        // Tests checkbox pre-population needs every code, so it must use GetAllSelectionsByBatchAsync.
        var allTests = await _blockTests.GetAllSelectionsByBatchAsync(BatchId ?? 0);
        ExistingHistologyCodes = allTests.Where(t => t.BlockID == Block.ID && t.TestType == BlockTestType.Histology).Select(t => t.Code).ToList();
        ExistingAntibodyCodes = allTests.Where(t => t.BlockID == Block.ID && t.TestType == BlockTestType.Antibodies).Select(t => t.Code).ToList();
        ExistingStainCodes = allTests.Where(t => t.BlockID == Block.ID && t.TestType == BlockTestType.Stain).Select(t => t.Code).ToList();
        HasSavedTests = ExistingHistologyCodes.Count > 0 || ExistingAntibodyCodes.Count > 0 || ExistingStainCodes.Count > 0;

        // Legacy: DisplayBatchLevelTests (Page_Load, new-block branch) — a brand-new block with no
        // test selections of its own yet defaults to the batch-level Histology/Antibody/Stain
        // choices made when the submission was created, instead of forcing a re-pick per block.
        // Not gated on IsAddFlow — a block reached via "Edit block" that has never had its own
        // tests saved is just as untested as one reached via the auto-provisioned add flow.
        if (ExistingHistologyCodes.Count == 0 && ExistingAntibodyCodes.Count == 0 && ExistingStainCodes.Count == 0 && !IsFromNextBlock)
        {
            var batchDefaults = await _batches.GetBatchTestSelectionsAsync(BatchId ?? 0);
            ExistingHistologyCodes = batchDefaults.Histology.Select(r => r.Code).ToList();
            ExistingAntibodyCodes = batchDefaults.Antibodies.Select(r => r.Code).ToList();
            ExistingStainCodes = batchDefaults.Stains.Select(r => r.Code).ToList();
        }
        return true;
    }

    private async Task LoadTestOptionsAsync()
    {
        var antibodyTableId = Batch?.BatchType == BatchTypeConstants.NonTse ? LookupNonTseAntibodies : LookupTseAntibodies;
        var fullHistologyOptions = await _lookups.GetHistologyTypesAsync();
        // includeInactive:true surfaces a legacy "Others" row that would otherwise be hidden, but can
        // also surface blank placeholder rows with no name — filter those out rather than render an
        // unlabelled checkbox.
        var fullAntibodyOptions = (await _lookups.GetLookupDataAsync(antibodyTableId, includeInactive: true))
            .Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();
        var fullStainOptions = (await _lookups.GetLookupDataAsync(LookupSpecialStain, includeInactive: true))
            .Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();

        // Legacy: HideTests() — a block can only ever offer the tests chosen for the whole
        // submission at Create/Edit Submission time, never the full lookup list, for every block
        // (first or subsequent) alike.
        var batchDefaults = await _batches.GetBatchTestSelectionsAsync(BatchId ?? 0);
        var allowedHistology = batchDefaults.Histology.Select(r => r.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedAntibodies = batchDefaults.Antibodies.Select(r => r.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedStains = batchDefaults.Stains.Select(r => r.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        HistologyOptions = fullHistologyOptions.Where(o => o.Code is not null && allowedHistology.Contains(o.Code)).ToList();
        AntibodyOptions = fullAntibodyOptions.Where(o => o.Code is not null && allowedAntibodies.Contains(o.Code)).ToList();
        StainOptions = fullStainOptions.Where(o => o.Code is not null && allowedStains.Contains(o.Code)).ToList();
    }

    /// <summary>Resolves <see cref="Animal"/> from the URL's batch/animal ID.</summary>
    private async Task<IActionResult?> LoadAnimalAsync()
    {
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage("/Index");

        var forbidden = await CheckBatchAccessAsync(_batches, batchId.Value);
        if (forbidden is not null) return forbidden;

        Session.BatchID = batchId;
        BatchId = batchId;

        if (AnimalId is null or <= 0)
            return RedirectToPage("/Submissions/SampleSummary", new { batchId });

        Session.AnimalID = AnimalId;
        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(batchId.Value);
        Animal = blockAnimals.FirstOrDefault(a => a.ID == AnimalId);
        var plainAnimals = await _submissions.GetAnimalsByBatchAsync(batchId.Value);
        if (Animal is null)
        {
            Animal = plainAnimals.FirstOrDefault(a => a.ID == AnimalId);
        }
        else if (Animal.BatchSubmissionID <= 0)
        {
            // BATCH_BLOCK_ANIMAL doesn't return BatchSubmissionID, so patch it from the plain
            // animal list — the sample's tissue list is keyed on it.
            Animal.BatchSubmissionID = plainAnimals.FirstOrDefault(a => a.ID == AnimalId)?.BatchSubmissionID ?? 0;
        }

        return null;
    }
}
