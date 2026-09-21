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
    private readonly IHistologyRefService _histologyRefs;

    public BlockDetailsModel(ISessionService session, ISubmissionService submissions, IBlockService blocks,
        IBatchService batches, ILookupService lookups, IBlockTestService blockTests, IHistologyRefService histologyRefs)
        : base(session)
    {
        _submissions = submissions;
        _blocks = blocks;
        _batches = batches;
        _lookups = lookups;
        _blockTests = blockTests;
        _histologyRefs = histologyRefs;
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

    [BindProperty] public string? NewBlockRef { get; set; }
    [BindProperty] public string? NewCustomerRef { get; set; }
    [BindProperty] public bool NewRepeatBlock { get; set; }
    [BindProperty] public string? NewComment { get; set; }

    /// <summary>Add-mode only — creates this many identical blocks with auto-incrementing refs.</summary>
    [BindProperty] public int NewNumberOfBlocks { get; set; } = 1;

    [BindProperty] public string NewTissueCode { get; set; } = string.Empty;
    [BindProperty] public short NewTissueNoPieces { get; set; } = 1;
    [BindProperty] public string? NewTissueComment { get; set; }

    /// <summary>Tissue currently shown in its inline edit row.</summary>
    [BindProperty(SupportsGet = true)] public int? EditTissueId { get; set; }

    [BindProperty] public int TissueId { get; set; }
    /// <summary>Inline edit-row fields — separate from the Add-tissue fields above so editing a row doesn't pre-fill the Add form.</summary>
    [BindProperty] public string EditTissueCode { get; set; } = string.Empty;
    [BindProperty] public short EditNoPieces { get; set; } = 1;
    [BindProperty] public string? EditComment { get; set; }

    /// <summary>Legacy: chkUseWholeTissueList. Unchecked (default) filters the tissue dropdown to
    /// codes already used on this submission; checked shows the full lookup list.</summary>
    [BindProperty(SupportsGet = true)] public bool UseWholeTissueList { get; set; }

    [BindProperty] public List<string> SelectedHistologyCodes { get; set; } = [];
    [BindProperty] public List<string> SelectedAntibodyCodes { get; set; } = [];
    [BindProperty] public List<string> SelectedStainCodes { get; set; } = [];

    /// <summary>Legacy: chkbCarryTests — "Use these tests for the next block?", read by the Next Block handler.</summary>
    [BindProperty] public bool CarryTestsToNextBlock { get; set; }

    /// <summary>Histology Reference for the sample (NN/NNNNN format). Stored on Animal record.</summary>
    [BindProperty] public string? EditHistologyRef { get; set; }

    /// <summary>Post-mortem Date for the sample. Stored on Animal record.</summary>
    [BindProperty] public string? EditPMDate { get; set; }

    public Animal? Animal { get; private set; }
    public Batch? Batch { get; private set; }
    public Block? Block { get; private set; }
    public bool IsPreCassetted => Batch?.IsPreCassetted == true;
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

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = IsEditMode ? "Edit block" : "Add block";
        ViewData["PageTitle"] = IsEditMode ? "Edit block" : "Add block";

        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return Page();

        // Initialize Histology Reference and PM Date from Animal record
        EditHistologyRef = Animal.HistologyRef;
        EditPMDate = Animal.PMDate;

        await LoadSupportingDataAsync();

        if (IsEditMode)
        {
            if (!await LoadEditModeDataAsync()) return Page();
        }
        else
        {
            // Legacy: BlockDetails.aspx.vb Page_Load -> CreateNewRecord() provisionally creates the
            // block immediately (in an in-memory dataset there; directly in the DB here) so tissues
            // and tests can be assigned from the very first page load, without an explicit Save
            // first. isAddFlow=true carries the "still adding" state across the redirects the
            // tissue/test actions below use to return to this page. Set up-front so the Add-mode
            // labelling (title/button/Number of blocks) is still correct even on the fallback
            // render paths below (e.g. no pre-booked refs left for a pre-cassetted submission).
            IsAddFlow = true;
            NewBlockRef = IsPreCassetted
                ? PreBookedBlockRefs.FirstOrDefault()?.BlockRef
                : BlockHelpers.ComputeNextBlockRef(
                    (await _blocks.GetByBatchAsync(BatchId ?? 0)).Where(b => b.AnimalID == Animal.ID).Select(b => b.BlockRef));

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

            var existingOrders = (await _blocks.GetByBatchAsync(BatchId ?? 0)).Select(b => b.Order).ToList();
            var newId = await _blocks.AddBlockAsync(BatchId ?? 0, Animal.ID, NewBlockRef, existingOrders, Session.UserID,
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

            return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = newId, isAddFlow = true });
        }

        return Page();
    }

    /// <summary>Saves the block (ref/customer ref/repeat/comment) and its test selections together — replaces the former separate Save block/Save tests actions.</summary>
    public async Task<IActionResult> OnPostDoneAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        // Validate Histology Reference before proceeding
        var histoRefError = await ValidateHistologyRefAsync(EditHistologyRef);
        if (histoRefError is not null)
        {
            ErrorMessage = histoRefError;
            await LoadSupportingDataAsync();
            await LoadEditModeDataAsync();
            return Page();
        }

        // Validate test selections up front too, so nothing is saved at all if either half is invalid.
        var testsError = ValidateTestSelections(SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes);
        if (testsError is not null)
        {
            ErrorMessage = testsError;
            ExistingHistologyCodes = SelectedHistologyCodes;
            ExistingAntibodyCodes = SelectedAntibodyCodes;
            ExistingStainCodes = SelectedStainCodes;
            await LoadSupportingDataAsync();
            await LoadEditModeDataAsync();
            return Page();
        }

        // Save Histology Reference and PM Date to Animal record if they've changed
        if (Animal is not null && (Animal.HistologyRef != EditHistologyRef || Animal.PMDate != EditPMDate))
        {
            var updatedAnimal = new Animal
            {
                ID = Animal.ID,
                BatchSubmissionID = Animal.BatchSubmissionID,
                SenderRef = Animal.SenderRef,
                NextBlockRef = Animal.NextBlockRef,
                HistoRefSet = !string.IsNullOrWhiteSpace(EditHistologyRef),
                HistologyRef = EditHistologyRef,
                OnHold = Animal.OnHold,
                PMDate = EditPMDate,
                PMDateSet = !string.IsNullOrWhiteSpace(EditPMDate),
                IsPGNumber = Animal.IsPGNumber,
                BookedHistologyRef = Animal.BookedHistologyRef,
                RowStamp = Animal.RowStamp,
            };
            await _submissions.UpdateAnimalAsync(updatedAnimal, Session.UserID);
            Animal = updatedAnimal; // Update the local reference
        }

        await LoadSupportingDataAsync();
        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);

        // Reached via a genuine Edit link (not the auto-provisioned add-flow) — plain update, no bulk-duplicate.
        if (!IsAddFlow)
        {
            var existing = allBlocks.FirstOrDefault(b => b.ID == BlockId);
            if (existing is null || string.IsNullOrWhiteSpace(NewBlockRef))
                return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });

            if (!CanUseAdditionalRequest) NewRepeatBlock = false;

            var updated = new Block
            {
                ID = existing.ID,
                BatchID = existing.BatchID,
                AnimalID = existing.AnimalID,
                BlockRef = NewBlockRef,
                CustomerRef = NewCustomerRef,
                Comment = NewComment,
                RepeatBlock = NewRepeatBlock,
                Status = existing.Status,
                Order = existing.Order,
                RowStamp = existing.RowStamp,
            };
            await _blocks.UpdateBlockAsync(updated, Session.UserID);
            await _blockTests.SaveTestSelectionsAsync(
                BatchId ?? 0, existing.ID, SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes, Session.UserID);
            return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });
        }

        // Add flow — legacy: UpdateBlockDetails() + CreateMultiBlocks(). Block #1 already exists
        // (auto-created on first load so tissues/tests could be assigned immediately); Save updates
        // that record, then optionally creates NewNumberOfBlocks-1 more sibling blocks.
        var current = allBlocks.FirstOrDefault(b => b.ID == BlockId);
        if (current is null || string.IsNullOrWhiteSpace(NewBlockRef)) return Page();

        var preBookedIndex = -1;
        if (IsPreCassetted)
        {
            var preBookedRefs = PreBookedBlockRefs.Select(b => b.BlockRef).ToList();
            preBookedIndex = preBookedRefs.FindIndex(r => r == NewBlockRef);
            if (preBookedIndex < 0)
            {
                ErrorMessage = "Select one of the pre-booked block references for this pre-cassetted submission.";
                return Page();
            }
            if (preBookedIndex + NewNumberOfBlocks > preBookedRefs.Count)
            {
                ErrorMessage = $"Only {preBookedRefs.Count - preBookedIndex} pre-booked block reference(s) are available from this starting point.";
                return Page();
            }
        }

        if (!CanUseAdditionalRequest)
        {
            NewRepeatBlock = false;
            // Legacy: ValidateRequiredData "You can only enter a used Block Ref if you tick the
            // additional request box" — collapses to a plain duplicate check for submission types
            // where Additional Request is never offered. Excludes this block's own (unchanged) row.
            if (allBlocks.Any(b => b.ID != BlockId && b.AnimalID == Animal.ID && b.BlockRef == NewBlockRef))
            {
                ErrorMessage = "This block reference has already been used for this sample.";
                return Page();
            }
        }

        await _blocks.UpdateBlockAsync(new Block
        {
            ID = current.ID,
            BatchID = current.BatchID,
            AnimalID = current.AnimalID,
            BlockRef = NewBlockRef,
            CustomerRef = NewCustomerRef,
            Comment = NewComment,
            RepeatBlock = NewRepeatBlock,
            Status = current.Status,
            Order = current.Order,
            RowStamp = current.RowStamp,
        }, Session.UserID);
        await _blockTests.SaveTestSelectionsAsync(
            BatchId ?? 0, current.ID, SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes, Session.UserID);

        var count = Math.Max(1, NewNumberOfBlocks);
        if (count > 1)
        {
            var existingOrders = allBlocks.Select(b => b.Order).ToList();
            var existingRefs = allBlocks.Where(b => b.ID != current.ID).Select(b => b.BlockRef).ToList();
            existingRefs.Add(NewBlockRef);
            var preBookedRefsForCreate = IsPreCassetted ? PreBookedBlockRefs.Select(b => b.BlockRef).ToList() : null;
            var blockRef = NewBlockRef;

            for (var i = 1; i < count; i++)
            {
                // Pre-cassetted blocks must use the next pre-booked ref, not the free-text auto-increment scheme.
                blockRef = IsPreCassetted
                    ? preBookedRefsForCreate![preBookedIndex + i]
                    : BlockHelpers.ComputeNextBlockRef(existingRefs);

                await _blocks.AddBlockAsync(
                    BatchId ?? 0, Animal.ID, blockRef, existingOrders, Session.UserID,
                    customerRef: null, comment: null, repeatBlock: false);

                existingRefs.Add(blockRef);
                existingOrders.Add(BlockHelpers.ComputeNextOrder(existingOrders));
            }

            // Bulk-created siblings have no single block to continue editing — return to the grid.
            return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId = BatchId, animalId = AnimalId });
        }

        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });
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
                Comment = NewTissueComment,
            };
            await _submissions.AddTissueAsync(tissue, Session.UserID);
        }

        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId, isAddFlow = IsAddFlow });
    }

    public async Task<IActionResult> OnPostDeleteTissueAsync(int tissueId)
    {
        await _submissions.DeleteTissueAsync(tissueId, TissueOwner.Block, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId, isAddFlow = IsAddFlow });
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
            Comment = EditComment,
            RowStamp = existing.RowStamp,
        };
        await _submissions.UpdateTissueAsync(updated, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId, isAddFlow = IsAddFlow });
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

        var error = ValidateTestSelections(SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes);
        if (error is not null)
        {
            ErrorMessage = error;
            ExistingHistologyCodes = SelectedHistologyCodes;
            ExistingAntibodyCodes = SelectedAntibodyCodes;
            ExistingStainCodes = SelectedStainCodes;
            await LoadEditModeDataAsync();
            return Page();
        }

        await _blockTests.SaveTestSelectionsAsync(
            BatchId ?? 0, BlockId.Value, SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes, Session.UserID);

        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        string nextRef;
        if (IsPreCassetted)
        {
            var preBooked = await _blocks.GetPreBookedByAnimalAsync(Animal.ID);
            if (preBooked.Count == 0)
            {
                ErrorMessage = "There are no more pre-booked block references available for this sample.";
                ExistingHistologyCodes = SelectedHistologyCodes;
                ExistingAntibodyCodes = SelectedAntibodyCodes;
                ExistingStainCodes = SelectedStainCodes;
                await LoadEditModeDataAsync();
                return Page();
            }
            nextRef = preBooked[0].BlockRef;
        }
        else
        {
            nextRef = BlockHelpers.ComputeNextBlockRef(allBlocks.Where(b => b.AnimalID == Animal.ID).Select(b => b.BlockRef));
        }

        var existingOrders = allBlocks.Select(b => b.Order).ToList();
        var newBlockId = await _blocks.AddBlockAsync(BatchId ?? 0, Animal.ID, nextRef, existingOrders, Session.UserID,
            customerRef: null, comment: null, repeatBlock: false);

        if (CarryTestsToNextBlock && newBlockId > 0)
            await _blockTests.SaveTestSelectionsAsync(
                BatchId ?? 0, newBlockId, SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes, Session.UserID);

        return newBlockId > 0
            ? RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = newBlockId })
            : RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });
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

    private async Task LoadSupportingDataAsync()
    {
        Batch = await _batches.GetByIdAsync(BatchId ?? 0);

        var submittedAsCode = await _batches.GetSubmittedAsCodeAsync(BatchId ?? 0);
        // Legacy: EnableDisableAdditionalRequest — disabled for Wet Tissue(1)/Stained Section(3)/Pre Cassetted(5).
        CanUseAdditionalRequest = submittedAsCode is not ("1" or "3" or "5");

        var fullTissueList = await _lookups.GetLookupDataAsync(LookupTissueCode);
        EditTissueOptions = fullTissueList;
        if (UseWholeTissueList || Animal is null)
        {
            TissueOptions = fullTissueList;
        }
        else
        {
            // Legacy: LoadLookupTypeList default (chkUseWholeTissueList unchecked) — only tissue
            // types already used across this animal's OWN blocks. Legacy source:
            // BlockDetails.aspx.vb::LoadLookupTypeList -> clsTissue.GetBatchAnimalTissues(BatchID,
            // AnimalID) -> SP GetBatchSampleTissues. Block-owned tissues have no BatchSubmissionID
            // of their own, so this cross-references this animal's block IDs against the batch's
            // block tissues rather than filtering by submission (which was the earlier, wrong fix).
            var animalBlockIds = (await _blocks.GetByBatchAsync(BatchId ?? 0))
                .Where(b => b.AnimalID == Animal.ID)
                .Select(b => b.ID)
                .ToHashSet();
            var allBlockTissues = await _submissions.GetTissuesByBatchAsync(BatchId ?? 0);
            var usedCodes = allBlockTissues.Where(t => animalBlockIds.Contains(t.OwnerID)).Select(t => t.TissueCode).ToHashSet();
            TissueOptions = fullTissueList.Where(o => o.Code is not null && usedCodes.Contains(o.Code)).ToList();
        }

        // Loaded for the initial provisioning request (BlockId not yet assigned) and for every
        // re-render while still mid add-flow — true edits of an already-established block never need it.
        if (IsPreCassetted && (!IsEditMode || IsAddFlow))
            PreBookedBlockRefs = await _blocks.GetPreBookedByAnimalAsync(Animal?.ID ?? AnimalId ?? 0);
        if (IsEditMode)
            await LoadTestOptionsAsync();
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
                EditComment = editing.Comment;
            }
        }

        var allTests = await _blockTests.GetByBatchAsync(BatchId ?? 0);
        ExistingHistologyCodes = allTests.Where(t => t.BlockID == Block.ID && t.TestType == BlockTestType.Histology).Select(t => t.Code).ToList();
        ExistingAntibodyCodes = allTests.Where(t => t.BlockID == Block.ID && t.TestType == BlockTestType.Antibodies).Select(t => t.Code).ToList();
        ExistingStainCodes = allTests.Where(t => t.BlockID == Block.ID && t.TestType == BlockTestType.Stain).Select(t => t.Code).ToList();
        HasSavedTests = ExistingHistologyCodes.Count > 0 || ExistingAntibodyCodes.Count > 0 || ExistingStainCodes.Count > 0;

        // Legacy: DisplayBatchLevelTests (Page_Load, new-block branch) — a brand-new block with no
        // test selections of its own yet defaults to the batch-level Histology/Antibody/Stain
        // choices made when the submission was created, instead of forcing a re-pick per block.
        // Not gated on IsAddFlow — a block reached via "Edit block" that has never had its own
        // tests saved is just as untested as one reached via the auto-provisioned add flow.
        if (ExistingHistologyCodes.Count == 0 && ExistingAntibodyCodes.Count == 0 && ExistingStainCodes.Count == 0)
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
        HistologyOptions = await _lookups.GetHistologyTypesAsync();
        // includeInactive:true surfaces a legacy "Others" row that would otherwise be hidden, but can
        // also surface blank placeholder rows with no name — filter those out rather than render an
        // unlabelled checkbox.
        AntibodyOptions = (await _lookups.GetLookupDataAsync(antibodyTableId, includeInactive: true))
            .Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();
        StainOptions = (await _lookups.GetLookupDataAsync(LookupSpecialStain, includeInactive: true))
            .Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();
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
        if (Animal is null)
        {
            var animals = await _submissions.GetAnimalsByBatchAsync(batchId.Value);
            Animal = animals.FirstOrDefault(a => a.ID == AnimalId);
        }

        return null;
    }

    /// <summary>
    /// Validates Histology Reference format (NN/NNNNN) and business rules.
    /// Returns error message if invalid, null if valid.
    /// </summary>
    private async Task<string?> ValidateHistologyRefAsync(string? histologyRef)
    {
        if (string.IsNullOrWhiteSpace(histologyRef))
            return null; // Empty is allowed (not recorded)

        // Check format: NN/NNNNN
        if (!System.Text.RegularExpressions.Regex.IsMatch(histologyRef, @"^\d{2}/\d{5}$"))
            return "Histology Reference must be in NN/NNNNN format (e.g., 26/40004).";

        // Extract year (first 2 digits)
        var yearStr = histologyRef.Substring(0, 2);
        if (!int.TryParse(yearStr, out var year))
            return "Invalid year in Histology Reference.";

        var currentYear = DateTime.Now.Year % 100; // Get last 2 digits of current year
        if (year > currentYear)
            return $"Histology Reference year ({year}) cannot be greater than current year ({currentYear}).";

        // Extract the numeric part (after /)
        var numStr = histologyRef.Substring(3);
        if (!int.TryParse(numStr, out var refNumber))
            return "Invalid Histology Reference number.";

        // Determine histology type from the numeric range
        int histologyType = DetermineHistologyTypeFromRef(refNumber);

        // Get the next available ref for this type
        var counters = await _histologyRefs.GetCountersAsync();
        var counter = counters.FirstOrDefault(c => c.Type == histologyType);
        if (counter is not null)
        {
            // Check that entered ref is less than the next available ref
            if (string.Compare(histologyRef, counter.NextHistologyRef, StringComparison.Ordinal) >= 0)
                return $"Histology Reference entered ({histologyRef}) must be less than the next available reference ({counter.NextHistologyRef}) for this type.";
        }

        return null;
    }

    /// <summary>
    /// Determines histology type code based on the numeric range of the reference.
    /// Ranges: Neuropath <20000, AbattoirSurvey <30000, TBDiag <40000, GeneralPool <60000, MouseProjects <90000.
    /// </summary>
    private static int DetermineHistologyTypeFromRef(int refNumber)
    {
        // These type codes come from HistologyRefTypeCode
        if (refNumber < 20000) return 1; // Neuropath
        if (refNumber < 30000) return 2; // AbattoirSurvey
        if (refNumber < 40000) return 3; // TBDiag
        if (refNumber < 60000) return 4; // GeneralPool
        if (refNumber < 90000) return 5; // MouseProjects
        return 4; // Default to GeneralPool
    }
}
