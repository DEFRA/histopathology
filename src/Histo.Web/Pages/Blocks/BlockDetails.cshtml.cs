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

    public BlockDetailsModel(ISessionService session, ISubmissionService submissions, IBlockService blocks,
        IBatchService batches, ILookupService lookups, IBlockTestService blockTests)
        : base(session)
    {
        _submissions = submissions;
        _blocks = blocks;
        _batches = batches;
        _lookups = lookups;
        _blockTests = blockTests;
    }

    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }
    [BindProperty(SupportsGet = true)] public int? AnimalId { get; set; }

    /// <summary>Block being edited. Null/0 means Add mode.</summary>
    [BindProperty(SupportsGet = true)] public int? BlockId { get; set; }

    public bool IsEditMode => BlockId is > 0;

    [BindProperty] public string? NewBlockRef { get; set; }
    [BindProperty] public string? NewCustomerRef { get; set; }
    [BindProperty] public bool NewRepeatBlock { get; set; }
    [BindProperty] public string? NewComment { get; set; }

    /// <summary>Add-mode only — creates this many identical blocks with auto-incrementing refs.</summary>
    [BindProperty] public int NewNumberOfBlocks { get; set; } = 1;

    [BindProperty] public string NewTissueCode { get; set; } = string.Empty;
    [BindProperty] public short NewTissueNoPieces { get; set; } = 1;
    [BindProperty] public string? NewTissueComment { get; set; }

    /// <summary>Legacy: chkUseWholeTissueList. Unchecked (default) filters the tissue dropdown to
    /// codes already used on this submission; checked shows the full lookup list.</summary>
    [BindProperty(SupportsGet = true)] public bool UseWholeTissueList { get; set; }

    [BindProperty] public List<string> SelectedHistologyCodes { get; set; } = [];
    [BindProperty] public List<string> SelectedAntibodyCodes { get; set; } = [];
    [BindProperty] public List<string> SelectedStainCodes { get; set; } = [];

    /// <summary>Legacy: chkbCarryTests — "Use these tests for the next block?", read by the Next Block handler.</summary>
    [BindProperty] public bool CarryTestsToNextBlock { get; set; }

    public Animal? Animal { get; private set; }
    public Batch? Batch { get; private set; }
    public Block? Block { get; private set; }
    public bool IsPreCassetted => Batch?.IsPreCassetted == true;
    public IReadOnlyList<Block> PreBookedBlockRefs { get; private set; } = [];

    public IReadOnlyList<Tissue> Tissues { get; private set; } = [];
    public IReadOnlyList<LookupItem> TissueOptions { get; private set; } = [];

    public IReadOnlyList<LookupItem> HistologyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> AntibodyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> StainOptions { get; private set; } = [];
    public IReadOnlyList<string> ExistingHistologyCodes { get; private set; } = [];
    public IReadOnlyList<string> ExistingAntibodyCodes { get; private set; } = [];
    public IReadOnlyList<string> ExistingStainCodes { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

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

        await LoadSupportingDataAsync();

        if (IsEditMode)
        {
            if (!await LoadEditModeDataAsync()) return Page();
        }
        else
        {
            // Default the Block ref the same way legacy's CreateNewRecord does: the next pre-booked
            // ref for pre-cassetted submissions, otherwise the next free two-digit ref for this
            // animal. Always a free-text default — legacy never renders Block Ref as a dropdown.
            NewBlockRef = IsPreCassetted
                ? PreBookedBlockRefs.FirstOrDefault()?.BlockRef
                : BlockHelpers.ComputeNextBlockRef(
                    (await _blocks.GetByBatchAsync(BatchId ?? 0)).Where(b => b.AnimalID == Animal.ID).Select(b => b.BlockRef));
        }

        return Page();
    }

    /// <summary>Creates a new block, or saves ref/customer ref/repeat changes to an existing one.</summary>
    public async Task<IActionResult> OnPostSaveAsync()
    {
        var redirect = await LoadAnimalAsync();
        if (redirect is not null) return redirect;
        if (Animal is null) return RedirectToPage("/Submissions/SampleSummary", new { batchId = BatchId });

        await LoadSupportingDataAsync();
        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);

        if (IsEditMode)
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
            return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });
        }

        if (IsPreCassetted)
        {
            if (!PreBookedBlockRefs.Any(b => b.BlockRef == NewBlockRef))
            {
                ErrorMessage = "Select one of the pre-booked block references for this pre-cassetted submission.";
                return Page();
            }
            NewNumberOfBlocks = 1;
        }

        if (string.IsNullOrWhiteSpace(NewBlockRef)) return Page();

        if (!CanUseAdditionalRequest)
        {
            NewRepeatBlock = false;
            // Legacy: ValidateRequiredData "You can only enter a used Block Ref if you tick the
            // additional request box" — collapses to a plain duplicate check for submission types
            // where Additional Request is never offered.
            if (allBlocks.Any(b => b.AnimalID == Animal.ID && b.BlockRef == NewBlockRef))
            {
                ErrorMessage = "This block reference has already been used for this sample.";
                return Page();
            }
        }

        var existingOrders = allBlocks.Select(b => b.Order).ToList();
        var existingRefs = allBlocks.Select(b => b.BlockRef).ToList();
        var count = Math.Max(1, NewNumberOfBlocks);
        var blockRef = NewBlockRef;
        var firstNewBlockId = 0;

        for (var i = 0; i < count; i++)
        {
            var newId = await _blocks.AddBlockAsync(
                BatchId ?? 0, Animal.ID, blockRef, existingOrders, Session.UserID,
                NewCustomerRef, comment: NewComment, repeatBlock: NewRepeatBlock);
            if (i == 0) firstNewBlockId = newId;

            existingRefs.Add(blockRef);
            existingOrders.Add(BlockHelpers.ComputeNextOrder(existingOrders));
            blockRef = BlockHelpers.ComputeNextBlockRef(existingRefs);
        }

        // Bulk-created blocks (count > 1) have no single block to continue editing — return to
        // the grid. A single new block continues into edit mode so tissues/tests can be added.
        return count > 1 || firstNewBlockId <= 0
            ? RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId = BatchId, animalId = AnimalId })
            : RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = firstNewBlockId });
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

        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });
    }

    public async Task<IActionResult> OnPostDeleteTissueAsync(int tissueId)
    {
        await _submissions.DeleteTissueAsync(tissueId, TissueOwner.Block, Session.UserID);
        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });
    }

    /// <summary>Delta-saves this block's Histology/Antibodies/Stain test-type selections.</summary>
    public async Task<IActionResult> OnPostSaveTestsAsync()
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
            await LoadSupportingDataAsync();
            Block = (await _blocks.GetByBatchAsync(BatchId ?? 0)).FirstOrDefault(b => b.ID == BlockId);
            Tissues = Block is null ? [] : await _submissions.GetTissuesByBlockAsync(Block.BatchID, Block.ID);
            return Page();
        }

        await _blockTests.SaveTestSelectionsAsync(
            BatchId ?? 0, BlockId.Value, SelectedHistologyCodes, SelectedAntibodyCodes, SelectedStainCodes, Session.UserID);

        return RedirectToPage(new { batchId = BatchId, animalId = AnimalId, blockId = BlockId });
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
        if (UseWholeTissueList || Animal is null)
        {
            TissueOptions = fullTissueList;
        }
        else
        {
            // Legacy: LoadLookupTypeList default (chkUseWholeTissueList unchecked) — only tissue
            // types already used on this submission, via GetBatchAnimalTissues(batchId, animalId).
            var submissionTissues = await _submissions.GetTissuesBySubmissionAsync(BatchId ?? 0, Animal.BatchSubmissionID);
            var usedCodes = submissionTissues.Select(t => t.TissueCode).ToHashSet();
            TissueOptions = fullTissueList.Where(o => o.Code is not null && usedCodes.Contains(o.Code)).ToList();
        }

        if (IsPreCassetted && !IsEditMode)
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

        Tissues = await _submissions.GetTissuesByBlockAsync(Block.BatchID, Block.ID);

        var allTests = await _blockTests.GetByBatchAsync(BatchId ?? 0);
        ExistingHistologyCodes = allTests.Where(t => t.BlockID == Block.ID && t.TestType == BlockTestType.Histology).Select(t => t.Code).ToList();
        ExistingAntibodyCodes = allTests.Where(t => t.BlockID == Block.ID && t.TestType == BlockTestType.Antibodies).Select(t => t.Code).ToList();
        ExistingStainCodes = allTests.Where(t => t.BlockID == Block.ID && t.TestType == BlockTestType.Stain).Select(t => t.Code).ToList();
        return true;
    }

    private async Task LoadTestOptionsAsync()
    {
        var antibodyTableId = Batch?.BatchType == BatchTypeConstants.NonTse ? LookupNonTseAntibodies : LookupTseAntibodies;
        HistologyOptions = await _lookups.GetHistologyTypesAsync();
        AntibodyOptions = await _lookups.GetLookupDataAsync(antibodyTableId);
        StainOptions = await _lookups.GetLookupDataAsync(LookupSpecialStain);
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
}
