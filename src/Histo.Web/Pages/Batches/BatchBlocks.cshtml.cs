using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces <c>BatchBlocks.aspx</c> — the dedicated "Assign Tissues to Blocks" batch-wide
/// overview (Add/Edit/Delete sample, Copy From Prev. Submission, Done). Reached from
/// <c>BatchesReceived.cshtml</c> (via <see cref="BatchDetailsModel"/>'s "Assign blocks" button).
///
/// Split out from <see cref="Histo.Web.Pages.Submissions.SubmissionDetailsBlockModel"/> (which
/// now only handles the per-animal "Sample blocks" view used by <c>SampleSummary</c>'s "Manage
/// sample"/"Edit sample" across the Create/Edit/View Submission journeys) — the two legacy pages
/// (<c>BatchBlocks.aspx</c> vs <c>BatchBlockSummary.aspx</c>/<c>SubmissionDetailsBlock.aspx</c>)
/// serve different journeys and were never the same screen in legacy.
/// </summary>
public class BatchBlocksModel : HistoPageModel
{
    private const int LookupTissueCode = 9;

    private readonly ISubmissionService _submissions;
    private readonly IBlockService _blocks;
    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;
    private readonly IBlockTestService _blockTests;
    private readonly ILogger<BatchBlocksModel> _logger;

    public BatchBlocksModel(ISessionService session, ISubmissionService submissions, IBlockService blocks,
        IBatchService batches, ILookupService lookups, IBlockTestService blockTests, ILogger<BatchBlocksModel> logger)
        : base(session)
    {
        _submissions = submissions;
        _blocks = blocks;
        _batches = batches;
        _lookups = lookups;
        _blockTests = blockTests;
        _logger = logger;
    }

    /// <summary>Batch ID from the URL (route/query). Falls back to <see cref="ISessionService.BatchID"/> for links not yet migrated.</summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>Sample (animal) pending delete confirmation — drives the inline GOV.UK confirmation panel (replaces browser confirm()).</summary>
    [BindProperty(SupportsGet = true)] public int? ConfirmDeleteAnimalId { get; set; }

    public Batch? Batch { get; private set; }
    public IReadOnlyList<Block> Blocks { get; private set; } = [];

    /// <summary>True for TSE submissions — shows H&amp;E (BSE)/IHC Prp grid columns instead of IHC Other, matching legacy HideColumns.</summary>
    public bool IsTse => Batch?.BatchType != BatchTypeConstants.NonTse;

    public IReadOnlyDictionary<int, IReadOnlyList<Tissue>> TissuesByBlockId { get; private set; } =
        new Dictionary<int, IReadOnlyList<Tissue>>();

    /// <summary>
    /// Legacy source: clsBatchSummary.vb::CreateBlockSummaryData — ALL 7 of the grid's boolean
    /// indicator columns (EO/H&amp;E/Special Stain/IHC-PrP/H&amp;E(BSE)/IHC-Other/Archive) are read
    /// from BLOCK_HISTOLOGY child rows (luHistology codes 1-7).
    /// </summary>
    public IReadOnlyList<LookupItem> HistologyOptions { get; private set; } = [];
    public IReadOnlyDictionary<int, IReadOnlyList<string>> HistologyCodesByBlockId { get; private set; } = new Dictionary<int, IReadOnlyList<string>>();

    /// <summary>Tissue-code → description lookup, used to render Tissue Details the same way as legacy's LookupDescription().</summary>
    public IReadOnlyList<LookupItem> TissueOptions { get; private set; } = [];

    /// <summary>Resolves a tissue code to its description, matching legacy's LookupDescription(dtTissuesList, TissueCode).</summary>
    public string TissueName(string code) => TissueOptions.FirstOrDefault(o => o.Code == code)?.Name ?? code;

    /// <summary>Sender ref keyed by AnimalID, used to label each grid row.</summary>
    public IReadOnlyDictionary<int, string> SenderRefsByAnimalId { get; private set; } = new Dictionary<int, string>();

    /// <summary>Histology ref keyed by AnimalID, matching legacy's grdBlockSummary "Histology Ref" column.</summary>
    public IReadOnlyDictionary<int, string?> HistologyRefsByAnimalId { get; private set; } = new Dictionary<int, string?>();

    public string? ErrorMessage { get; private set; }

    /// <summary>Mirrors SampleSummaryModel — hides all mutation actions (Add/Delete/Copy/Done) in the View Submission journey.
    /// Legacy source: BatchBlocks.aspx.vb::DisableEnableControls (SV_ViewSubmission branch).</summary>
    public bool IsViewMode => Session.IsViewSubmissionMode;

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Blocks";
        ViewData["PageTitle"] = "Blocks";

        var redirect = await ResolveBatchAsync();
        if (redirect is not null) return redirect;

        await LoadGridAsync();
        return Page();
    }

    /// <summary>
    /// "Edit Sample" — legacy source: <c>BatchBlocks.aspx.vb::btnEditSample_Click</c>. Requires
    /// exactly one selected row, resolves the sample it belongs to, and hands off to the
    /// per-sample blocks page (<see cref="Histo.Web.Pages.Submissions.SubmissionDetailsBlockModel"/>).
    /// </summary>
    public async Task<IActionResult> OnPostEditSampleSelectAsync(List<int>? blockIds)
    {
        var redirect = await ResolveBatchAsync();
        if (redirect is not null) return redirect;

        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        var selected = blockIds is { Count: 1 } ? allBlocks.FirstOrDefault(b => b.ID == blockIds[0]) : null;
        if (selected is null)
        {
            ErrorMessage = "Select exactly one sample to edit.";
            await LoadGridAsync();
            return Page();
        }

        return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId = BatchId, animalId = selected.AnimalID });
    }

    /// <summary>
    /// "Delete Sample" step 1 — legacy source: <c>BatchBlocks.aspx.vb::btnDeleteSample_Click</c>.
    /// Requires exactly one selected row and routes to an inline confirmation before the whole
    /// sample (animal, blocks and tissues) is removed.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteSampleSelectAsync(List<int>? blockIds)
    {
        var redirect = await ResolveBatchAsync();
        if (redirect is not null) return redirect;

        var allBlocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        var selected = blockIds is { Count: 1 } ? allBlocks.FirstOrDefault(b => b.ID == blockIds[0]) : null;
        if (selected is null)
        {
            ErrorMessage = "Select exactly one sample to delete.";
            await LoadGridAsync();
            return Page();
        }

        return RedirectToPage(new { batchId = BatchId, confirmDeleteAnimalId = selected.AnimalID });
    }

    /// <summary>"Delete Sample" step 2 — removes the sample (animal) and all of its blocks/tissues.</summary>
    public async Task<IActionResult> OnPostDeleteSampleAsync(int animalId)
    {
        await _submissions.DeleteAnimalAsync(animalId, Session.UserID);
        return RedirectToPage(new { batchId = BatchId });
    }

    /// <summary>
    /// Stores the selected block IDs and redirects to the "Copy blocks" workflow.
    /// Legacy source: <c>BatchBlocks.aspx.vb</c> grid's per-row copy action → <c>CopyBlocks.aspx</c>.
    /// </summary>
    public IActionResult OnPostCopyAsync(List<int>? blockIds)
    {
        if (blockIds is null || blockIds.Count == 0)
            return RedirectToPage(new { batchId = BatchId });

        TempData["CopyBlockIds"] = string.Join(",", blockIds);
        return RedirectToPage("/Blocks/CopyBlocks", new { batchId = BatchId });
    }

    /// <summary>
    /// "Done" — legacy source: <c>BatchBlocks.aspx.vb::btSubmit_Click</c>. Marks the batch blocked,
    /// transitions status to In progress, and records whether every sample has at least one block
    /// (a simplified stand-in for legacy's per-tissue "green star" indicator, which tracked at
    /// individual tissue-piece granularity). Legacy then redirects to <c>FinalPrintBatch.aspx</c>;
    /// that report page is not yet migrated (deferred to the Reporting phase per
    /// <c>docs/Migration-Plan.md</c>), so this redirects to Batches received instead, matching
    /// legacy's own eventual post-print destination (<c>SV_RedirectAfterPrint</c>).
    /// </summary>
    public async Task<IActionResult> OnPostDoneAsync()
    {
        var redirect = await ResolveBatchAsync();
        if (redirect is not null) return redirect;

        var blocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        var animals = await _submissions.GetAnimalsByBatchAsync(BatchId ?? 0);
        var allTissuesAssigned = animals.Count > 0 && animals.All(a => blocks.Any(b => b.AnimalID == a.ID));

        await _batches.CompleteBlockAssignmentAsync(BatchId ?? 0, allTissuesAssigned, Session.UserID);
        return RedirectToPage("/Batches/BatchesReceived");
    }

    /// <summary>Loads the grid (blocks + sender/histology refs) and supporting lookup data — shared by <see cref="OnGetAsync"/> and the select-handlers' validation-error fallback.</summary>
    private async Task LoadGridAsync()
    {
        Blocks = await _blocks.GetByBatchAsync(BatchId ?? 0);
        var animals = await _submissions.GetAnimalsByBatchAsync(BatchId ?? 0);
        SenderRefsByAnimalId = animals.ToDictionary(a => a.ID, a => a.SenderRef);
        HistologyRefsByAnimalId = animals.ToDictionary(a => a.ID, a => a.HistologyRef);
        await LoadSupportingDataAsync();
    }

    /// <summary>Resolves/validates <see cref="BatchId"/>, applying the same area-access guard as every other batch page.</summary>
    private async Task<IActionResult?> ResolveBatchAsync()
    {
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage("/Index");

        var forbidden = await CheckBatchAccessAsync(_batches, batchId.Value);
        if (forbidden is not null) return forbidden;

        Session.BatchID = batchId;
        BatchId = batchId;
        return null;
    }

    /// <summary>Loads the batch (for the TSE/Non-TSE flag), per-block tissues, the tissue-code lookup, and per-block Histology test-selection indicators.</summary>
    private async Task LoadSupportingDataAsync()
    {
        Batch = await _batches.GetByIdAsync(BatchId ?? 0);
        TissueOptions = await _lookups.GetLookupDataAsync(LookupTissueCode);

        var allTissues = await _submissions.GetTissuesByBatchAsync(BatchId ?? 0);
        var tissuesByBlockId = new Dictionary<int, IReadOnlyList<Tissue>>();
        foreach (var block in Blocks)
            tissuesByBlockId[block.ID] = allTissues.Where(t => t.OwnerID == block.ID).ToList();
        TissuesByBlockId = tissuesByBlockId;

        // Wrapped because this reads a 10-result-set SP and a shape mismatch must not take the
        // whole page down — see SubmissionDetailsBlockModel's identical precedent.
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
            _logger.LogError(ex, "BatchBlocks: failed to load per-block test selections for batch {BatchId}.", BatchId);
        }
    }
}
