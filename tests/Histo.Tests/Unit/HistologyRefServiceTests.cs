using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Verifies <see cref="HistologyRefService.GetNextAvailableRefAsync"/> draws the counter via the
/// atomic <see cref="IHistologyRepository.ClaimNextCounterAsync"/> rather than the racy
/// read-then-write pair (<see cref="IHistologyRepository.GetCountersAsync"/> +
/// <see cref="IHistologyRepository.UpdateCounterAsync"/>) previously used — two concurrent calls
/// to the old path could both read the same counter value and both successfully write it + 1,
/// returning duplicate refs.
/// </summary>
public class HistologyRefServiceTests
{
    private readonly Mock<IHistologyRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private HistologyRefService CreateSut() => new(_repo.Object, _logger.Object);

    [Fact]
    public async Task GetNextAvailableRefAsync_PoolHasARef_ReturnsItWithoutClaimingCounter()
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[new HistologyRef { Ref = "26/60010", HistologyType = HistologyRefTypeCode.MouseProjects }]);

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects);

        Assert.Equal("26/60010", result);
        _repo.Verify(r => r.ClaimNextCounterAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_NoPoolRef_ClaimsCounterAtomically()
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[]);
        _repo.Setup(r => r.ClaimNextCounterAsync(HistologyRefTypeCode.MouseProjects, 90000, It.IsAny<CancellationToken>()))
            .ReturnsAsync("60002");

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects);

        Assert.Equal($"{DateTime.Now.Year % 100:D2}/60002", result);
        _repo.Verify(r => r.GetCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.UpdateCounterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_UnknownType_ReturnsNull()
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[]);

        var result = await CreateSut().GetNextAvailableRefAsync(999);

        Assert.Null(result);
    }
}
