using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SampleCopyHelper"/> — the shared "copy a sample's blocks" steps used
/// by the Copy blocks and Copy sample journeys (block row, tissues, and test-type ticks).
/// </summary>
public class SampleCopyHelperTests
{
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockTestService> _blockTests = new();
    private readonly Mock<IHistologyRefService> _histologyRefs = new();

    private SampleCopyServices Services => new(_blocks.Object, _submissions.Object, _blockTests.Object);

    // ── CopyBlocksToAnimalAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CopyBlocksToAnimalAsync_SingleBlock_CopiesBlockTissuesAndTests()
    {
        // Covers: the happy-path foreach body — CopyBlockAsync call, tissue copy loop, and
        // CopyBlockTestsAsync delegation (sourceTests.Count > 0 branch).
        var sourceBlock = new Block { ID = 10, BatchID = 1, AnimalID = 5, BlockRef = "01", Order = 1 };
        var target = new Animal { ID = 99, SenderRef = "S1-NEW", NextBlockRef = "01" };
        var tissue = new Tissue { ID = 1, OwnerID = 10, TissueCode = "LIVER" };
        var tests = new List<BlockTest>
        {
            new() { ID = 1, BlockID = 10, TestType = BlockTestType.Histology, Code = "1" },
            new() { ID = 2, BlockID = 10, TestType = BlockTestType.Antibodies, Code = "A1" },
            new() { ID = 3, BlockID = 10, TestType = BlockTestType.Stain, Code = "S1" },
        };

        _blocks.Setup(b => b.CopyBlockAsync(sourceBlock, 1, 99, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(55);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[tissue]);

        await SampleCopyHelper.CopyBlocksToAnimalAsync(Services, [sourceBlock], [sourceBlock], tests, 1, target, 7);

        _submissions.Verify(s => s.CopyTissueAsync(tissue, 55, 7, It.IsAny<CancellationToken>()), Times.Once);
        _blockTests.Verify(t => t.SaveTestSelectionsAsync(1, 55,
            It.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { "1" })),
            It.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { "A1" })),
            It.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { "S1" })),
            7, It.IsAny<CancellationToken>()), Times.Once);
        // BlockHelpers.ComputeNextBlockRef("01") -> "02", assigned to the animal in memory.
        Assert.Equal("02", target.NextBlockRef);
    }

    [Fact]
    public async Task CopyBlocksToAnimalAsync_BlockHasNoTests_DoesNotCallSaveTestSelections()
    {
        // Covers: CopyBlockTestsAsync's early-return branch (sourceTests.Count == 0).
        var sourceBlock = new Block { ID = 10, BatchID = 1, AnimalID = 5, BlockRef = "01", Order = 1 };
        var target = new Animal { ID = 99, SenderRef = "S1-NEW" };

        _blocks.Setup(b => b.CopyBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(55);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);

        await SampleCopyHelper.CopyBlocksToAnimalAsync(Services, [sourceBlock], [sourceBlock], [], 1, target, 7);

        _blockTests.Verify(t => t.SaveTestSelectionsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CopyBlocksToAnimalAsync_CopyBlockAsyncReturnsZero_ThrowsInvalidOperationException()
    {
        // Covers: the "newBlockId <= 0" failure branch.
        var sourceBlock = new Block { ID = 10, BatchID = 1, AnimalID = 5, BlockRef = "01" };
        var target = new Animal { ID = 99, SenderRef = "S1-NEW" };

        _blocks.Setup(b => b.CopyBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SampleCopyHelper.CopyBlocksToAnimalAsync(Services, [sourceBlock], [sourceBlock], [], 1, target, 7));

        Assert.Contains("01", ex.Message);
        Assert.Contains("S1-NEW", ex.Message);
        _submissions.Verify(s => s.GetTissuesByBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CopyBlocksToAnimalAsync_MultipleBlocks_AssignsSequentialRefsWithoutCollision()
    {
        // Covers: the loop advancing refs/orders in memory across iterations, and the animal
        // already owning a block (animalBlocks filter by AnimalID).
        var existingBlock = new Block { ID = 1, BatchID = 1, AnimalID = 99, BlockRef = "01", Order = 1 };
        var source1 = new Block { ID = 10, BatchID = 1, AnimalID = 5, BlockRef = "01", Order = 2 };
        var source2 = new Block { ID = 11, BatchID = 1, AnimalID = 5, BlockRef = "02", Order = 3 };
        var target = new Animal { ID = 99, SenderRef = "S1-NEW" };
        var allBlocks = new List<Block> { existingBlock, source1, source2 };

        var capturedRefs = new List<IEnumerable<string>>();
        _blocks.Setup(b => b.CopyBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<Block, int, int, IEnumerable<string>, IEnumerable<int>, int, CancellationToken>((_, _, _, refs, _, _, _) => capturedRefs.Add(refs.ToList()))
            .ReturnsAsync(55);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);

        await SampleCopyHelper.CopyBlocksToAnimalAsync(Services, [source1, source2], allBlocks, [], 1, target, 7);

        // First call sees only the animal's pre-existing block ("01"); second call sees "01" plus
        // the computed next ref ("02") added after the first copy.
        Assert.Equal(["01"], capturedRefs[0]);
        Assert.Equal(["01", "02"], capturedRefs[1]);
    }

    // ── DrawRefMatchingAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DrawRefMatchingAsync_SourceHasNoHistologyRef_ReturnsNullWithoutCallingService()
    {
        // Covers: HistologyRefTypeCode.FromExistingRef(null) -> null short-circuit branch.
        var result = await SampleCopyHelper.DrawRefMatchingAsync(_histologyRefs.Object, null);

        Assert.Null(result);
        _histologyRefs.Verify(h => h.GetNextAvailableRefAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DrawRefMatchingAsync_SourceHasHistologyRef_DelegatesToServiceWithMatchingType()
    {
        // Covers: the non-null branch — resolves the type from the source ref then delegates.
        _histologyRefs.Setup(h => h.GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync("26/60003");

        var result = await SampleCopyHelper.DrawRefMatchingAsync(_histologyRefs.Object, "26/60001");

        Assert.Equal("26/60003", result);
    }
}
