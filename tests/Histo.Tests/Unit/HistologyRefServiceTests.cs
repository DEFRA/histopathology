using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Verifies <see cref="HistologyRefService.GetNextAvailableRefAsync"/> draws both the pool and the
/// counter atomically rather than via a racy read-then-write pair:
/// <see cref="IHistologyRepository.ClaimUnusedRefAsync"/> (not a plain
/// <see cref="IHistologyRepository.GetUnusedRefsAsync"/> read) for the pool, and
/// <see cref="IHistologyRepository.ClaimNextCounterAsync"/> (not <see cref="IHistologyRepository.GetCountersAsync"/>
/// + <see cref="IHistologyRepository.UpdateCounterAsync"/>) for the counter fallback. Either
/// two-step read-then-write path let two concurrent calls both read the same value and both
/// successfully claim it, returning duplicate refs.
/// </summary>
public class HistologyRefServiceTests
{
    private readonly Mock<IHistologyRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private HistologyRefService CreateSut() => new(_repo.Object, _logger.Object);

    [Fact]
    public async Task GetNextAvailableRefAsync_PoolHasARef_ReturnsItWithoutClaimingCounter()
    {
        _repo.Setup(r => r.ClaimUnusedRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync("26/60010");

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects);

        Assert.Equal("26/60010", result);
        _repo.Verify(r => r.ClaimNextCounterAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_NoPoolRef_ClaimsCounterAtomically()
    {
        _repo.Setup(r => r.ClaimUnusedRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _repo.Setup(r => r.ClaimNextCounterAsync(HistologyRefTypeCode.MouseProjects, 90000, It.IsAny<CancellationToken>()))
            .ReturnsAsync("60002");

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects);

        Assert.Equal($"{DateTime.Now.Year % 100:D2}/60002", result);
        _repo.Verify(r => r.GetUnusedRefsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.GetCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.UpdateCounterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_UnknownType_ReturnsNull()
    {
        _repo.Setup(r => r.ClaimUnusedRefAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await CreateSut().GetNextAvailableRefAsync(999);

        Assert.Null(result);
    }
}
