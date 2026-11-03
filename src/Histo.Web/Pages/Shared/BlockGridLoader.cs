using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Models;

namespace Histo.Web.Pages.Shared;

/// <summary>Per-block lookups shared by the block summary grids (SubmissionDetailsBlock, BatchBlocks).</summary>
public static class BlockGridLoader
{
    public static IReadOnlyDictionary<int, IReadOnlyList<Tissue>> GroupTissuesByBlock(
        IEnumerable<Block> blocks, IEnumerable<Tissue> allTissues)
    {
        var byBlock = new Dictionary<int, IReadOnlyList<Tissue>>();
        foreach (var blockId in blocks.Select(b => b.ID))
            byBlock[blockId] = allTissues.Where(t => t.OwnerID == blockId).ToList();
        return byBlock;
    }

    // GetByBatchAsync excludes Special Stain/IHC-PrP/IHC-Other (worklist-only exclusion) —
    // the grid checkboxes need every code, so this uses GetAllSelectionsByBatchAsync.
    public static async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> LoadHistologyCodesByBlockAsync(
        IBlockTestService blockTests, int batchId, IEnumerable<Block> blocks)
    {
        var allTests = await blockTests.GetAllSelectionsByBatchAsync(batchId);
        var byBlock = new Dictionary<int, IReadOnlyList<string>>();
        foreach (var blockId in blocks.Select(b => b.ID))
            byBlock[blockId] = allTests.Where(t => t.BlockID == blockId && t.TestType == BlockTestType.Histology).Select(t => t.Code).ToList();
        return byBlock;
    }
}
