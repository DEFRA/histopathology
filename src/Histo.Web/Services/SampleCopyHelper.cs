using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;

namespace Histo.Web.Services;

/// <summary>Groups the three services <see cref="SampleCopyHelper.CopyBlocksToAnimalAsync"/> needs so the method stays under the 7-parameter limit.</summary>
public sealed record SampleCopyServices(IBlockService Blocks, ISubmissionService Submissions, IBlockTestService BlockTests);

/// <summary>
/// Shared "copy a sample's blocks" steps used by Copy blocks and the Copy sample journey, so both
/// reproduce a block identically: the block row, its tissues, and its test-type selections.
/// The Copy submission journey performs the same work inside its own DB transaction
/// (<c>ISubmissionRepository.CreateBatchWithCopiedSamplesAsync</c>).
/// </summary>
public static class SampleCopyHelper
{
    /// <summary>
    /// Copies <paramref name="sourceBlocks"/> onto <paramref name="target"/>, computing each new
    /// block's reference and order in sequence so several copies onto the same sample don't collide.
    /// Advances <see cref="Animal.NextBlockRef"/> in memory (legacy <c>clsAnimal.UpdateAnimalNextBlock</c>,
    /// called at the end of <c>clsBlock.CopyBlock</c>) — the caller persists the animal.
    /// Throws <see cref="InvalidOperationException"/> if a block can't be created, so the caller
    /// reports the failure rather than leaving the sample short of blocks, tissues and test ticks.
    /// </summary>
    public static async Task CopyBlocksToAnimalAsync(
        SampleCopyServices services,
        IReadOnlyList<Block> sourceBlocks,
        IReadOnlyList<Block> allBatchBlocks,
        IReadOnlyList<BlockTest> allBatchTests,
        int batchId,
        Animal target,
        int userId)
    {
        var animalBlocks = allBatchBlocks.Where(b => b.AnimalID == target.ID).ToList();
        var refs = animalBlocks.Select(b => b.BlockRef).ToList();
        var orders = allBatchBlocks.Select(b => b.Order).ToList();

        foreach (var sourceBlock in sourceBlocks)
        {
            var newBlockId = await services.Blocks.CopyBlockAsync(sourceBlock, batchId, target.ID, refs, orders, userId);
            if (newBlockId <= 0)
                throw new InvalidOperationException(
                    $"Could not copy block '{sourceBlock.BlockRef}' to sample '{target.SenderRef}'. The copy is incomplete — check that sample's blocks before continuing.");

            refs.Add(BlockHelpers.ComputeNextBlockRef(refs));
            orders.Add(BlockHelpers.ComputeNextOrder(orders));

            var tissues = await services.Submissions.GetTissuesByBlockAsync(sourceBlock.BatchID, sourceBlock.ID);
            foreach (var tissue in tissues)
                await services.Submissions.CopyTissueAsync(tissue, newBlockId, userId);

            await CopyBlockTestsAsync(services.BlockTests, allBatchTests, sourceBlock.ID, batchId, newBlockId, userId);

            target.NextBlockRef = BlockHelpers.ComputeNextBlockRef(refs);
        }
    }

    /// <summary>Reproduces the source block's test-type ticks on the copy — without this the copy's Archive/EO/H&amp;E/IHC/Special stain boxes all come back empty.</summary>
    private static async Task CopyBlockTestsAsync(
        IBlockTestService blockTests, IReadOnlyList<BlockTest> allBatchTests,
        int sourceBlockId, int batchId, int newBlockId, int userId)
    {
        var sourceTests = allBatchTests.Where(t => t.BlockID == sourceBlockId).ToList();
        if (sourceTests.Count == 0) return;

        await blockTests.SaveTestSelectionsAsync(
            batchId,
            newBlockId,
            [.. sourceTests.Where(t => t.TestType == BlockTestType.Histology).Select(t => t.Code)],
            [.. sourceTests.Where(t => t.TestType == BlockTestType.Antibodies).Select(t => t.Code)],
            [.. sourceTests.Where(t => t.TestType == BlockTestType.Stain).Select(t => t.Code)],
            userId);
    }

    /// <summary>
    /// Draws the next histology reference of the same type as <paramref name="sourceHistologyRef"/>,
    /// i.e. the "Get next ref" step the user would otherwise perform by hand. Returns
    /// <see langword="null"/> when the source has no reference to classify, or none remain.
    /// </summary>
    public static async Task<string?> DrawRefMatchingAsync(IHistologyRefService histologyRefs, string? sourceHistologyRef)
    {
        var histologyType = HistologyRefTypeCode.FromExistingRef(sourceHistologyRef);
        return histologyType is null ? null : await histologyRefs.GetNextAvailableRefAsync(histologyType.Value);
    }
}
