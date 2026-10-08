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

    private BatchService BuildSut() => new(_repoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task RefreshAllTissuesAssignedAsync_DelegatesToRepository()
    {
        var sut = BuildSut();

        await sut.RefreshAllTissuesAssignedAsync(12, 99);

        _repoMock.Verify(r => r.RefreshAllTissuesAssignedAsync(12, 99, default), Times.Once);
    }

    [Fact]
    public async Task GetAnimalsWithUnassignedTissuesAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.GetAnimalsWithUnassignedTissuesAsync(12, default))
            .ReturnsAsync((IReadOnlyCollection<int>)[7]);
        var sut = BuildSut();

        var result = await sut.GetAnimalsWithUnassignedTissuesAsync(12);

        Assert.Equal([7], result);
        _repoMock.Verify(r => r.GetAnimalsWithUnassignedTissuesAsync(12, default), Times.Once);
    }

    [Fact]
    public async Task GetAnimalsWithUnassignedTissuesAsync_RepositoryThrows_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetAnimalsWithUnassignedTissuesAsync(12, default))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var sut = BuildSut();

        var result = await sut.GetAnimalsWithUnassignedTissuesAsync(12);

        Assert.Empty(result);
    }
}
