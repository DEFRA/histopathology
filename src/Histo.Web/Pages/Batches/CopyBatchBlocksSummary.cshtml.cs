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
/// Replaces <c>CopyBatchBlocksSummary.aspx</c> — the read-only, pre-copy block/tissue/test
/// preview reached from <c>CopyBatchBlocks.aspx</c>'s "Summary" button (cassetted scenario only;
/// legacy's non-cassetted <c>CopyBatch.aspx</c> never had this button). Shows the SOURCE batch's
/// live blocks exactly as legacy did (over <c>Session(SV_OldBatchDetails)</c>) — not the
/// in-progress copy plan, so "Change" sender-ref picks made on <see cref="CopyBatchModel"/> are
/// deliberately not reflected here.
///
/// Not to be confused with <see cref="CopyBatchSummaryModel"/>, which replaces the same legacy
/// page for the unrelated *post-copy* confirmation screen.
/// </summary>
public class CopyBatchBlocksSummaryModel : HistoPageModel
{
    private const int LookupTissueCode = 9;

    private readonly IBatchService _batches;
    private readonly ISubmissionService _submissions;
    private readonly IBlockService _blocks;
    private readonly ILookupService _lookups;
    private readonly IBlockTestService _blockTests;
    private readonly ILogger<CopyBatchBlocksSummaryModel> _logger;

    public CopyBatchBlocksSummaryModel(ISessionService session, IBatchService batches, ISubmissionService submissions,
        IBlockService blocks, ILookupService lookups, IBlockTestService blockTests, ILogger<CopyBatchBlocksSummaryModel> logger)
        : base(session)
    {
        _batches = batches;
        _submissions = submissions;
        _blocks = blocks;
        _lookups = lookups;
        _blockTests = blockTests;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)] public int SourceBatchId { get; set; }

    public Batch? SourceBatch { get; private set; }
    public IReadOnlyList<Block> Blocks { get; private set; } = [];
    public IReadOnlyDictionary<int, string> SenderRefsByAnimalId { get; private set; } = new Dictionary<int, string>();
    public IReadOnlyDictionary<int, IReadOnlyList<Tissue>> TissuesByBlockId { get; private set; } = new Dictionary<int, IReadOnlyList<Tissue>>();
    public IReadOnlyDictionary<int, IReadOnlyList<string>> HistologyCodesByBlockId { get; private set; } = new Dictionary<int, IReadOnlyList<string>>();
    public IReadOnlyList<LookupItem> TissueOptions { get; private set; } = [];

    /// <summary>Resolves a tissue code to its description, matching legacy's LookupDescription(dtTissuesList, TissueCode).</summary>
    public string TissueName(string code) => TissueOptions.FirstOrDefault(o => o.Code == code)?.Name ?? code;

    public async Task<IActionResult> OnGetAsync(int sourceBatchId)
    {
        ViewData["Title"] = "Submission summary";
        ViewData["PageTitle"] = "Submission summary";

        SourceBatchId = sourceBatchId;
        var forbidden = await CheckBatchAccessAsync(_batches, sourceBatchId);
        if (forbidden is not null) return forbidden;

        SourceBatch = await _batches.GetByIdAsync(sourceBatchId);
        if (SourceBatch is null) return RedirectToPage("/Index");

        Blocks = await _blocks.GetByBatchAsync(sourceBatchId);

        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(sourceBatchId);
        var allAnimals = await _submissions.GetAnimalsByBatchAsync(sourceBatchId);
        var seenIds = blockAnimals.Select(a => a.ID).ToHashSet();
        var animals = blockAnimals.Count == 0 ? allAnimals : [.. blockAnimals, .. allAnimals.Where(a => !seenIds.Contains(a.ID))];
        SenderRefsByAnimalId = animals.ToDictionary(a => a.ID, a => a.SenderRef);

        TissueOptions = await _lookups.GetLookupDataAsync(LookupTissueCode);

        var allTissues = await _submissions.GetTissuesByBatchAsync(sourceBatchId);
        var tissuesByBlockId = new Dictionary<int, IReadOnlyList<Tissue>>();
        foreach (var blockId in Blocks.Select(b => b.ID))
            tissuesByBlockId[blockId] = allTissues.Where(t => t.OwnerID == blockId).ToList();
        TissuesByBlockId = tissuesByBlockId;

        // Wrapped so a 10-result-set SP shape mismatch doesn't take the whole page down —
        // matches BatchBlocksModel's identical precedent for the same stored procedure.
        try
        {
            var allTests = await _blockTests.GetAllSelectionsByBatchAsync(sourceBatchId);
            var histologyByBlock = new Dictionary<int, IReadOnlyList<string>>();
            foreach (var blockId in Blocks.Select(b => b.ID))
                histologyByBlock[blockId] = allTests.Where(t => t.BlockID == blockId && t.TestType == BlockTestType.Histology).Select(t => t.Code).ToList();
            HistologyCodesByBlockId = histologyByBlock;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CopyBatchBlocksSummary: failed to load per-block test selections for batch {BatchId}.", sourceBatchId);
        }

        return Page();
    }
}
