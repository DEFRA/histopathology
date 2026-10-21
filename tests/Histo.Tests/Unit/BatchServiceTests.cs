using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Moq;

namespace Histo.Tests.Unit;

public class BatchServiceTests
{
    private readonly Mock<IBatchRepository> _repoMock = new();
    private readonly Mock<IAppLogger> _loggerMock = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private BatchService BuildSut() => new(_repoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task RefreshAllTissuesAssignedAsync_DelegatesToRepository()
    {
        var sut = BuildSut();

        await sut.RefreshAllTissuesAssignedAsync(12, 99, Ct);

        _repoMock.Verify(r => r.RefreshAllTissuesAssignedAsync(12, 99, Ct), Times.Once);
    }

    [Fact]
    public async Task GetAnimalsWithUnassignedTissuesAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.GetAnimalsWithUnassignedTissuesAsync(12, Ct))
            .ReturnsAsync((IReadOnlyCollection<int>)[7]);
        var sut = BuildSut();

        var result = await sut.GetAnimalsWithUnassignedTissuesAsync(12, Ct);

        Assert.Equal([7], result);
        _repoMock.Verify(r => r.GetAnimalsWithUnassignedTissuesAsync(12, Ct), Times.Once);
    }

    [Fact]
    public async Task GetAnimalsWithUnassignedTissuesAsync_RepositoryThrows_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetAnimalsWithUnassignedTissuesAsync(12, Ct))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var sut = BuildSut();

        var result = await sut.GetAnimalsWithUnassignedTissuesAsync(12, Ct);

        Assert.Empty(result);
    }

    [Fact]
    public async Task TryGetBatchTestSelectionsAsync_DelegatesToRepository()
    {
        var selections = new BatchTestSelections();
        _repoMock.Setup(r => r.GetBatchTestSelectionsAsync(12, Ct)).ReturnsAsync(selections);

        var result = await BuildSut().TryGetBatchTestSelectionsAsync(12, Ct);

        Assert.Same(selections, result);
    }

    [Fact]
    public async Task TryGetBatchTestSelectionsAsync_RepositoryThrows_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetBatchTestSelectionsAsync(12, Ct)).ThrowsAsync(new InvalidOperationException("boom"));

        Assert.Null(await BuildSut().TryGetBatchTestSelectionsAsync(12, Ct));
    }

    [Fact]
    public async Task SetInProgressAsync_DelegatesToRepository_ReturnsTrue()
    {
        var result = await BuildSut().SetInProgressAsync(42, 7, Ct);

        Assert.True(result);
        _repoMock.Verify(r => r.SetInProgressAsync(42, 7, Ct), Times.Once);
    }

    [Fact]
    public async Task SetInProgressAsync_RepositoryThrows_LogsAndReturnsFalse()
    {
        _repoMock.Setup(r => r.SetInProgressAsync(42, 7, Ct)).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await BuildSut().SetInProgressAsync(42, 7, Ct);

        Assert.False(result);
        _loggerMock.Verify(l => l.LogError("Failed to mark batch {BatchId} in progress.", It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task SetCompletedAsync_DelegatesToRepository_ReturnsTrue()
    {
        var completedDate = new DateTime(2026, 10, 19);

        var result = await BuildSut().SetCompletedAsync(42, completedDate, 7, Ct);

        Assert.True(result);
        _repoMock.Verify(r => r.SetCompletedAsync(42, completedDate, 7, Ct), Times.Once);
    }

    [Fact]
    public async Task SetCompletedAsync_RepositoryThrows_LogsAndReturnsFalse()
    {
        var completedDate = new DateTime(2026, 10, 19);
        _repoMock.Setup(r => r.SetCompletedAsync(42, completedDate, 7, Ct)).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await BuildSut().SetCompletedAsync(42, completedDate, 7, Ct);

        Assert.False(result);
        _loggerMock.Verify(l => l.LogError("Failed to mark batch {BatchId} completed.", It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task GetTestPremiumChargeCountsAsync_PassesTheCodeListsToTheRepository()
    {
        var codes = new TestCodeSelection(["1"], ["A1"], ["S1"]);
        IReadOnlyList<TestPremiumChargeCount> counts = [new() { ProjectDescription = "P", PremiumCode = "TC 1", Count = 2 }];
        _repoMock.Setup(r => r.GetTestPremiumChargeCountsAsync("P", 0, codes, null, null, Ct))
            .ReturnsAsync(counts);

        var result = await BuildSut().GetTestPremiumChargeCountsAsync("P", 0, codes, ct: Ct);

        Assert.Same(counts, result);
    }

    [Fact]
    public async Task GetTestPremiumChargeBatchesAsync_PassesTheCodeListsToTheRepository()
    {
        var codes = new TestCodeSelection(["1"], [], []);
        IReadOnlyList<TestPremiumChargeBatchRef> batches = [new() { PremiumCode = "TC 1", BatchID = 7 }];
        _repoMock.Setup(r => r.GetTestPremiumChargeBatchesAsync(null, 1, codes, null, null, Ct))
            .ReturnsAsync(batches);

        var result = await BuildSut().GetTestPremiumChargeBatchesAsync(null, 1, codes, ct: Ct);

        Assert.Same(batches, result);
    }

    [Fact]
    public async Task GetTestPremiumChargeCountsAsync_RepositoryThrows_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetTestPremiumChargeCountsAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<TestCodeSelection>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Assert.Empty(await BuildSut().GetTestPremiumChargeCountsAsync(null, 0, new TestCodeSelection([], [], []), ct: Ct));
    }
}
