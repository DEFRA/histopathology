using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

public class BlockServiceCoverageTests
{
    private readonly Mock<IBlockRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static InvalidOperationException Boom => new("boom");

    private BlockService Sut() => new(_repo.Object, _logger.Object);

    private void VerifyLogged() =>
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception?>(), It.IsAny<object[]>()), Times.AtLeastOnce);

    [Fact]
    public async Task GetByBatchAsync_ReturnsBlocksOrEmptyOnFailure()
    {
        IReadOnlyList<Block> blocks = [new Block { ID = 1 }];
        _repo.Setup(r => r.GetByBatchAsync(5, Ct)).ReturnsAsync(blocks);
        Assert.Same(blocks, await Sut().GetByBatchAsync(5, Ct));

        _repo.Setup(r => r.GetByBatchAsync(5, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetByBatchAsync(5, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetBlockRefsOnOtherBatchesAsync_ReturnsRefs_AndPropagatesFailure()
    {
        IReadOnlyList<string> refs = ["01"];
        _repo.Setup(r => r.GetBlockRefsOnOtherBatchesAsync(2, 5, Ct)).ReturnsAsync(refs);
        Assert.Same(refs, await Sut().GetBlockRefsOnOtherBatchesAsync(2, 5, Ct));

        _repo.Setup(r => r.GetBlockRefsOnOtherBatchesAsync(2, 5, Ct)).ThrowsAsync(Boom);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().GetBlockRefsOnOtherBatchesAsync(2, 5, Ct));
    }

    [Fact]
    public async Task GetPreBookedByAnimalAsync_ReturnsBlocksOrEmptyOnFailure()
    {
        IReadOnlyList<Block> blocks = [new Block { ID = 1 }];
        _repo.Setup(r => r.GetPreBookedByAnimalAsync(2, Ct)).ReturnsAsync(blocks);
        Assert.Same(blocks, await Sut().GetPreBookedByAnimalAsync(2, Ct));

        _repo.Setup(r => r.GetPreBookedByAnimalAsync(2, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetPreBookedByAnimalAsync(2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task AddBlockAsync_NoOptions_DefaultsToNoCustomerRefCommentOrRepeat()
    {
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, false, Ct))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ReturnsAsync(3);

        Assert.Equal(3, await Sut().AddBlockAsync(5, 2, "01", [], 7, ct: Ct));

        Assert.Null(saved!.CustomerRef);
        Assert.Null(saved.Comment);
        Assert.False(saved.RepeatBlock);
        Assert.Equal(BlockHelpers.ComputeNextOrder([]), saved.Order);
    }

    [Fact]
    public async Task AddBlockAsync_RepositoryThrows_ReturnsZeroAndLogs()
    {
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, false, Ct)).ThrowsAsync(Boom);

        Assert.Equal(0, await Sut().AddBlockAsync(5, 2, "01", [], 7, ct: Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task AddBlockKeepingRefAsync_NoOptions_UsesDefaults()
    {
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, true, Ct))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ReturnsAsync(4);

        Assert.Equal(4, await Sut().AddBlockKeepingRefAsync(5, 2, "02", [1], 7, ct: Ct));

        Assert.False(saved!.RepeatBlock);
        Assert.Null(saved.CustomerRef);
    }

    [Fact]
    public async Task UpdateBlockAsync_SuccessAndFailure()
    {
        var block = new Block { ID = 8 };
        Assert.True(await Sut().UpdateBlockAsync(block, 7, Ct));
        _repo.Verify(r => r.SaveAsync(block, 7, false, Ct), Times.Once);

        _repo.Setup(r => r.SaveAsync(block, 7, false, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().UpdateBlockAsync(block, 7, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task CreatePreBookedBlockAsync_SuccessAndFailure()
    {
        Assert.True(await Sut().CreatePreBookedBlockAsync(2, "03", Ct));
        _repo.Verify(r => r.CreatePreBookedBlockAsync(2, "03", Ct), Times.Once);

        _repo.Setup(r => r.CreatePreBookedBlockAsync(2, "03", Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().CreatePreBookedBlockAsync(2, "03", Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task BookPreBookedBlocksAsync_ReturnsResultsOrNullOnFailure()
    {
        IReadOnlyList<int> refs = [1, 2];
        IReadOnlyList<PreBookedBlockResult> results = [new PreBookedBlockResult(1, PreBookedBlockOutcome.Booked)];
        _repo.Setup(r => r.BookPreBookedBlocksAsync("S1", refs, Ct)).ReturnsAsync(results);
        Assert.Same(results, await Sut().BookPreBookedBlocksAsync("S1", refs, Ct));

        _repo.Setup(r => r.BookPreBookedBlocksAsync("S1", refs, Ct)).ThrowsAsync(Boom);
        Assert.Null(await Sut().BookPreBookedBlocksAsync("S1", refs, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task ClaimPreBookedBlockAsync_UpdatesPlaceholderAsPreBookedUsed()
    {
        var preBooked = new Block { ID = 9, AnimalID = 2, BlockRef = "05", Status = BlockStatus.PreBooked };
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, false, Ct))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ReturnsAsync(9);

        var ok = await Sut().ClaimPreBookedBlockAsync(preBooked, 5, [1, 2], 7, new BlockOptions("C", "cm", true), Ct);

        Assert.True(ok);
        Assert.Equal((9, 5, 2, "05", BlockStatus.PreBookedUsed), (saved!.ID, saved.BatchID, saved.AnimalID, saved.BlockRef, saved.Status));
        Assert.Equal(("C", "cm", true), (saved.CustomerRef, saved.Comment, saved.RepeatBlock));
        Assert.Equal(BlockHelpers.ComputeNextOrder([1, 2]), saved.Order);
    }

    [Fact]
    public async Task ClaimPreBookedBlockAsync_NoOptions_DefaultsAndFailureReturnsFalse()
    {
        var preBooked = new Block { ID = 9, AnimalID = 2, BlockRef = "05" };
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, false, Ct))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ThrowsAsync(Boom);

        Assert.False(await Sut().ClaimPreBookedBlockAsync(preBooked, 5, [], 7, ct: Ct));

        Assert.Null(saved!.CustomerRef);
        Assert.False(saved.RepeatBlock);
        VerifyLogged();
    }

    [Fact]
    public async Task CopyBlockAsync_AssignsNextFreeRefAndCarriesOptionsFromSource()
    {
        var source = new Block { CustomerRef = "C", Comment = "cm", RepeatBlock = true };
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, true, Ct))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ReturnsAsync(12);

        var id = await Sut().CopyBlockAsync(source, 5, 2, ["01", "02"], [1, 2], 7, Ct);

        Assert.Equal(12, id);
        Assert.Equal(BlockHelpers.ComputeNextBlockRef(["01", "02"]), saved!.BlockRef);
        Assert.Equal((5, 2, "C", "cm", true), (saved.BatchID, saved.AnimalID, saved.CustomerRef, saved.Comment, saved.RepeatBlock));
    }

    [Fact]
    public async Task DeleteBlockAsync_SuccessAndFailure()
    {
        Assert.True(await Sut().DeleteBlockAsync(8, 7, Ct));
        _repo.Verify(r => r.DeleteAsync(8, 7, Ct), Times.Once);

        _repo.Setup(r => r.DeleteAsync(8, 7, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().DeleteBlockAsync(8, 7, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetUsedBlockRefsByHistologyRefAsync_ReturnsRefsOrEmptyOnFailure()
    {
        IReadOnlyList<UsedBlockRef> refs = [new UsedBlockRef { BlockRef = 1 }];
        _repo.Setup(r => r.GetUsedBlockRefsByHistologyRefAsync("H", Ct)).ReturnsAsync(refs);
        Assert.Same(refs, await Sut().GetUsedBlockRefsByHistologyRefAsync("H", Ct));

        _repo.Setup(r => r.GetUsedBlockRefsByHistologyRefAsync("H", Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetUsedBlockRefsByHistologyRefAsync("H", Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetUsedBlockRefsBySenderRefAsync_ReturnsRefsOrEmptyOnFailure()
    {
        IReadOnlyList<UsedBlockRef> refs = [new UsedBlockRef { BlockRef = 1 }];
        _repo.Setup(r => r.GetUsedBlockRefsBySenderRefAsync("S", Ct)).ReturnsAsync(refs);
        Assert.Same(refs, await Sut().GetUsedBlockRefsBySenderRefAsync("S", Ct));

        _repo.Setup(r => r.GetUsedBlockRefsBySenderRefAsync("S", Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetUsedBlockRefsBySenderRefAsync("S", Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetBlockArchiveAsync_ReturnsRowsOrEmptyOnFailure()
    {
        IReadOnlyList<BlockArchiveInfo> rows = [new BlockArchiveInfo { ID = 1 }];
        _repo.Setup(r => r.GetBlockArchiveAsync("S", "H", "B", "L", Ct)).ReturnsAsync(rows);
        Assert.Same(rows, await Sut().GetBlockArchiveAsync("S", "H", "B", "L", Ct));

        _repo.Setup(r => r.GetBlockArchiveAsync("S", "H", "B", "L", Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetBlockArchiveAsync("S", "H", "B", "L", Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetSlideArchiveAsync_ReturnsRowsOrEmptyOnFailure()
    {
        IReadOnlyList<SlideArchiveInfo> rows = [new SlideArchiveInfo { BatchID = 1 }];
        _repo.Setup(r => r.GetSlideArchiveAsync("S", "H", "L", Ct)).ReturnsAsync(rows);
        Assert.Same(rows, await Sut().GetSlideArchiveAsync("S", "H", "L", Ct));

        _repo.Setup(r => r.GetSlideArchiveAsync("S", "H", "L", Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetSlideArchiveAsync("S", "H", "L", Ct));
        VerifyLogged();
    }
}

public class BlockTestServiceCoverageTests
{
    private readonly Mock<IBlockTestRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static InvalidOperationException Boom => new("boom");

    private BlockTestService Sut() => new(_repo.Object, _logger.Object);

    private void VerifyLogged() =>
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception?>(), It.IsAny<object[]>()), Times.AtLeastOnce);

    [Fact]
    public async Task GetByBatchAsync_ReturnsTestsOrEmptyOnFailure()
    {
        IReadOnlyList<BlockTest> tests = [new BlockTest { ID = 1 }];
        _repo.Setup(r => r.GetByBatchAsync(5, Ct)).ReturnsAsync(tests);
        Assert.Same(tests, await Sut().GetByBatchAsync(5, Ct));

        _repo.Setup(r => r.GetByBatchAsync(5, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetByBatchAsync(5, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetAllSelectionsByBatchAsync_ReturnsTestsOrEmptyOnFailure()
    {
        IReadOnlyList<BlockTest> tests = [new BlockTest { ID = 1 }];
        _repo.Setup(r => r.GetAllSelectionsByBatchAsync(5, Ct)).ReturnsAsync(tests);
        Assert.Same(tests, await Sut().GetAllSelectionsByBatchAsync(5, Ct));

        _repo.Setup(r => r.GetAllSelectionsByBatchAsync(5, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetAllSelectionsByBatchAsync(5, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingTestOrNull()
    {
        _repo.Setup(r => r.GetByBatchAsync(5, Ct)).ReturnsAsync([new BlockTest { ID = 1 }, new BlockTest { ID = 2, Code = "X" }]);

        Assert.Equal("X", (await Sut().GetByIdAsync(5, 2, Ct))!.Code);
        Assert.Null(await Sut().GetByIdAsync(5, 99, Ct));
    }

    [Fact]
    public async Task UpdateAsync_DelegatesAndLetsConcurrencyExceptionPropagate()
    {
        var test = new BlockTest { ID = 1 };
        await Sut().UpdateAsync(test, 7, Ct);
        _repo.Verify(r => r.UpdateAsync(test, 7, Ct), Times.Once);

        _repo.Setup(r => r.UpdateAsync(test, 7, Ct)).ThrowsAsync(new BlockTestConcurrencyException());
        await Assert.ThrowsAsync<BlockTestConcurrencyException>(() => Sut().UpdateAsync(test, 7, Ct));
    }

    [Fact]
    public async Task SaveTCCodesAsync_DelegatesAndRethrowsAfterLogging()
    {
        IReadOnlyList<TcCode> existing = [new TcCode(1, "A")];
        IReadOnlyList<string> selected = ["B"];
        await Sut().SaveTCCodesAsync(5, 6, BlockTestType.Stain, existing, selected, 7, Ct);
        _repo.Verify(r => r.SaveTCCodesAsync(5, 6, BlockTestType.Stain, existing, selected, 7, Ct), Times.Once);

        _repo.Setup(r => r.SaveTCCodesAsync(5, 6, BlockTestType.Stain, existing, selected, 7, Ct)).ThrowsAsync(Boom);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().SaveTCCodesAsync(5, 6, BlockTestType.Stain, existing, selected, 7, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task SaveTestSelectionsAsync_DelegatesAndRethrowsAfterLogging()
    {
        IReadOnlyList<string> h = ["1"];
        IReadOnlyList<string> a = ["A"];
        IReadOnlyList<string> s = ["S"];
        await Sut().SaveTestSelectionsAsync(5, 6, h, a, s, 7, Ct);
        _repo.Verify(r => r.SaveTestSelectionsAsync(5, 6, h, a, s, 7, Ct), Times.Once);

        _repo.Setup(r => r.SaveTestSelectionsAsync(5, 6, h, a, s, 7, Ct)).ThrowsAsync(Boom);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().SaveTestSelectionsAsync(5, 6, h, a, s, 7, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task AddTestsToBatchBlocksAsync_DelegatesAndRethrowsAfterLogging()
    {
        IReadOnlyList<string> h = ["1"];
        IReadOnlyList<string> a = ["A"];
        IReadOnlyList<string> s = ["S"];
        await Sut().AddTestsToBatchBlocksAsync(5, h, a, s, 7, Ct);
        _repo.Verify(r => r.AddTestsToBatchBlocksAsync(5, h, a, s, 7, Ct), Times.Once);

        _repo.Setup(r => r.AddTestsToBatchBlocksAsync(5, h, a, s, 7, Ct)).ThrowsAsync(Boom);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().AddTestsToBatchBlocksAsync(5, h, a, s, 7, Ct));
        VerifyLogged();
    }
}
