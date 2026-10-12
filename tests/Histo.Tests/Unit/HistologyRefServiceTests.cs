using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Verifies <see cref="HistologyRefService.GetNextAvailableRefAsync"/> takes this calendar year's
/// lowest pre-booked-but-unused ref first, then falls back to the <c>GetNextHistologyRef</c> SP
/// (<see cref="IHistologyRepository.DrawNextRefAsync"/>), which reads and advances the counter in
/// one server-side step rather than a racy <see cref="IHistologyRepository.GetCountersAsync"/> +
/// <see cref="IHistologyRepository.UpdateCounterAsync"/> pair.
/// </summary>
public class HistologyRefServiceTests
{
    private readonly Mock<IHistologyRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private static string ThisYear => $"{DateTime.Now.Year % 100:D2}";

    public HistologyRefServiceTests() =>
        _repo.Setup(r => r.GetUnusedRefsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[]);

    private HistologyRefService CreateSut() => new(_repo.Object, _logger.Object);

    [Fact]
    public async Task GetNextAvailableRefAsync_EmptyPool_DrawsFromTheCounterSpAndPrefixesTheYear()
    {
        _repo.Setup(r => r.DrawNextRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync("60002");

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects);

        Assert.Equal($"{ThisYear}/60002", result);
        _repo.Verify(r => r.GetCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.UpdateCounterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_PrefersLowestPooledRefOfTheCurrentYear()
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)
            [
                new HistologyRef { Ref = "08/60070" },
                new HistologyRef { Ref = $"{ThisYear}/60500" },
                new HistologyRef { Ref = $"{ThisYear}/60120" },
            ]);

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects);

        Assert.Equal($"{ThisYear}/60120", result);
        _repo.Verify(r => r.DrawNextRefAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_PoolHoldsOnlyPriorYearRefs_FallsBackToCounter()
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[new HistologyRef { Ref = "08/60070" }]);
        _repo.Setup(r => r.DrawNextRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync("60002");

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects);

        Assert.Equal($"{ThisYear}/60002", result);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_NoCounterRow_ReturnsNull()
    {
        _repo.Setup(r => r.DrawNextRefAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_UnknownType_ReturnsNullWithoutHittingTheDatabase()
    {
        var result = await CreateSut().GetNextAvailableRefAsync(999);

        Assert.Null(result);
        _repo.Verify(r => r.DrawNextRefAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.GetUnusedRefsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// The counter column is an unconstrained varchar(5) — General Pool held the literal value
    /// 'df' in a real database, which would otherwise have minted "26/df" onto a sample.
    /// </summary>
    [Theory]
    [InlineData("df")]    // not a number at all
    [InlineData("100")]   // below the General Pool floor of 40000
    [InlineData("60000")] // past the General Pool ceiling (max valid is 59999)
    public async Task GetNextAvailableRefAsync_CounterOutsideTheTypeRange_ReturnsNull(string counterValue)
    {
        _repo.Setup(r => r.DrawNextRefAsync(HistologyRefTypeCode.GeneralPool, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counterValue);

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.GeneralPool);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("40000")]
    [InlineData("59999")]
    public async Task GetNextAvailableRefAsync_GeneralPoolBoundsAreInclusive(string counterValue)
    {
        _repo.Setup(r => r.DrawNextRefAsync(HistologyRefTypeCode.GeneralPool, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counterValue);

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.GeneralPool);

        Assert.Equal($"{ThisYear}/{counterValue}", result);
    }
}
