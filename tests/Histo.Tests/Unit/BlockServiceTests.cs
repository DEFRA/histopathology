using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

public class BlockServiceTests
{
    private static readonly int[] ExistingOrders = [1, 2, 3];

    private readonly Mock<IBlockRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private BlockService CreateSut() => new(_repo.Object, _logger.Object);

    [Fact]
    public async Task AddBlockAsync_SavesUsedBlockWithNextOrder_AndReturnsNewId()
    {
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, false, It.IsAny<CancellationToken>()))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ReturnsAsync(99);

        var id = await CreateSut().AddBlockAsync(5, 2, "01", ExistingOrders, 7, "CUST", "note", true);

        Assert.Equal(99, id);
        Assert.NotNull(saved);
        Assert.Equal(5, saved.BatchID);
        Assert.Equal(2, saved.AnimalID);
        Assert.Equal("01", saved.BlockRef);
        Assert.Equal("CUST", saved.CustomerRef);
        Assert.Equal("note", saved.Comment);
        Assert.True(saved.RepeatBlock);
        Assert.Equal(BlockStatus.Used, saved.Status);
        Assert.Equal(BlockHelpers.ComputeNextOrder(ExistingOrders), saved.Order);
    }

    [Fact]
    public async Task AddBlockKeepingRefAsync_SavesWithKeepRefAndOptions()
    {
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, true, It.IsAny<CancellationToken>()))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ReturnsAsync(55);

        var id = await CreateSut().AddBlockKeepingRefAsync(5, 2, "02", ExistingOrders, 7, new BlockOptions("CUST", "note", true));

        Assert.Equal(55, id);
        Assert.NotNull(saved);
        Assert.Equal("CUST", saved.CustomerRef);
        Assert.Equal("note", saved.Comment);
        Assert.True(saved.RepeatBlock);
    }

    [Fact]
    public async Task CopyBlockAsync_SavesWithKeepRef_SoRefUsedOnOtherSubmissionIsNotBumped()
    {
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, true, It.IsAny<CancellationToken>()))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ReturnsAsync(55);

        var id = await CreateSut().CopyBlockAsync(new Block { BlockRef = "01" }, 5, 2, [], ExistingOrders, 7);

        Assert.Equal(55, id);
        Assert.Equal("01", saved?.BlockRef);
    }

    [Fact]
    public async Task CopyBlockAsync_RefAlreadyUsedOnSameBatch_UsesNextFreeRef()
    {
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, true, It.IsAny<CancellationToken>()))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ReturnsAsync(55);

        await CreateSut().CopyBlockAsync(new Block { BlockRef = "01" }, 5, 2, ["01"], ExistingOrders, 7);

        Assert.Equal("02", saved?.BlockRef);
    }

    [Fact]
    public async Task AddBlockKeepingRefAsync_NoOptions_LeavesOptionalFieldsEmpty()
    {
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, true, It.IsAny<CancellationToken>()))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ReturnsAsync(55);

        await CreateSut().AddBlockKeepingRefAsync(5, 2, "02", ExistingOrders, 7);

        Assert.NotNull(saved);
        Assert.Null(saved.CustomerRef);
        Assert.Null(saved.Comment);
        Assert.False(saved.RepeatBlock);
    }

    [Fact]
    public async Task AddBlockAsync_RepositoryThrows_ReturnsZero()
    {
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var id = await CreateSut().AddBlockAsync(5, 2, "01", ExistingOrders, 7);

        Assert.Equal(0, id);
    }

    [Fact]
    public async Task UpdateBlockAsync_Saves_AndReturnsTrue()
    {
        var block = new Block { ID = 3 };

        var ok = await CreateSut().UpdateBlockAsync(block, 7);

        Assert.True(ok);
        _repo.Verify(r => r.SaveAsync(block, 7, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBlockAsync_RepositoryThrows_ReturnsFalse()
    {
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Assert.False(await CreateSut().UpdateBlockAsync(new Block { ID = 3 }, 7));
    }

    [Fact]
    public async Task ClaimPreBookedBlockAsync_UpdatesPlaceholderInPlaceAsPreBookedUsed()
    {
        Block? saved = null;
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), 7, false, It.IsAny<CancellationToken>()))
            .Callback<Block, int, bool, CancellationToken>((b, _, _, _) => saved = b)
            .ReturnsAsync(1);
        var preBooked = new Block { ID = 40, AnimalID = 2, BlockRef = "03", Status = BlockStatus.PreBooked };

        var ok = await CreateSut().ClaimPreBookedBlockAsync(preBooked, 5, ExistingOrders, 7, new BlockOptions("CUST", "note", true));

        Assert.True(ok);
        Assert.NotNull(saved);
        Assert.Equal(40, saved.ID);
        Assert.Equal(5, saved.BatchID);
        Assert.Equal(2, saved.AnimalID);
        Assert.Equal("03", saved.BlockRef);
        Assert.Equal("CUST", saved.CustomerRef);
        Assert.True(saved.RepeatBlock);
        Assert.Equal(BlockStatus.PreBookedUsed, saved.Status);
    }

    [Fact]
    public async Task ClaimPreBookedBlockAsync_RepositoryThrows_ReturnsFalse()
    {
        _repo.Setup(r => r.SaveAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var ok = await CreateSut().ClaimPreBookedBlockAsync(new Block { ID = 40 }, 5, ExistingOrders, 7);

        Assert.False(ok);
    }
}
