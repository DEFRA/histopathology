using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Verifies <see cref="HistologyRefService.GetNextAvailableRefAsync"/> refuses a type whose counter is
/// outside its range, then draws from the <c>GetNextHistologyRef</c> SP
/// (<see cref="IHistologyRepository.DrawNextRefAsync"/>), which reads and advances the counter in
/// one server-side step. The unused pool is never read.
/// </summary>
public class HistologyRefServiceTests
{
    private readonly Mock<IHistologyRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private static string ThisYear => $"{DateTime.Now.Year % 100:D2}";

    public HistologyRefServiceTests()
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[]);
        GivenCounters((1, "10001"), (2, "20001"), (3, "30001"), (4, "40001"), (5, "60001"));
    }

    private void GivenCounters(params (int Type, string Next)[] counters) =>
        _repo.Setup(r => r.GetCountersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[.. counters.Select(c => new HistologyRefCounter { Type = c.Type, NextHistologyRef = c.Next })]);

    private HistologyRefService CreateSut() => new(_repo.Object, _logger.Object);

    [Fact]
    public async Task GetNextAvailableRefAsync_EmptyPool_DrawsFromTheCounterSpAndPrefixesTheYear()
    {
        _repo.Setup(r => r.DrawNextRefAsync(HistologyRefTypeCode.MouseProjects, 90000, It.IsAny<CancellationToken>()))
            .ReturnsAsync("60002");

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects, ct: TestContext.Current.CancellationToken);

        Assert.Equal($"{ThisYear}/60002", result);
        _repo.Verify(r => r.UpdateCounterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_PooledRefsExist_IgnoresThePoolAndDrawsFromTheCounter()
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)
            [
                new HistologyRef { Ref = "08/60070" },
                new HistologyRef { Ref = $"{ThisYear}/60500" },
                new HistologyRef { Ref = $"{ThisYear}/60120" },
            ]);
        _repo.Setup(r => r.DrawNextRefAsync(HistologyRefTypeCode.MouseProjects, 90000, It.IsAny<CancellationToken>()))
            .ReturnsAsync("60002");

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects, ct: TestContext.Current.CancellationToken);

        Assert.Equal($"{ThisYear}/60002", result);
        _repo.Verify(r => r.GetUnusedRefsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_PoolHoldsOnlyPriorYearRefs_FallsBackToCounter()
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[new HistologyRef { Ref = "08/60070" }]);
        _repo.Setup(r => r.DrawNextRefAsync(HistologyRefTypeCode.MouseProjects, 90000, It.IsAny<CancellationToken>()))
            .ReturnsAsync("60002");

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects, ct: TestContext.Current.CancellationToken);

        Assert.Equal($"{ThisYear}/60002", result);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_NoCounterRow_ReturnsNull()
    {
        GivenCounters();

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects, ct: TestContext.Current.CancellationToken);

        Assert.Null(result);
        _repo.Verify(r => r.DrawNextRefAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_ExhaustedType_RefusesWithoutAdvancingTheCounter()
    {
        // 60000 is the first number of the next type's range: drawing here would drag the counter on into it.
        GivenCounters((4, "60000"));

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.GeneralPool, ct: TestContext.Current.CancellationToken);

        Assert.Null(result);
        _repo.Verify(r => r.DrawNextRefAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()), Times.Once);
    }

    [Theory]
    [InlineData("df")]
    [InlineData("100")]
    public async Task GetNextAvailableRefAsync_CounterRowOutsideTheRange_RefusesWithoutDrawing(string counterValue)
    {
        GivenCounters((4, counterValue));

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.GeneralPool, ct: TestContext.Current.CancellationToken);

        Assert.Null(result);
        _repo.Verify(r => r.DrawNextRefAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_UnknownType_ReturnsNullWithoutHittingTheDatabase()
    {
        var result = await CreateSut().GetNextAvailableRefAsync(999, ct: TestContext.Current.CancellationToken);

        Assert.Null(result);
        _repo.Verify(r => r.DrawNextRefAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _repo.Setup(r => r.DrawNextRefAsync(HistologyRefTypeCode.GeneralPool, 60000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counterValue);

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.GeneralPool, ct: TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetNextAvailableRefRunAsync_DrawsConsecutiveRefsFromTheCounter_IgnoringThePool()
    {
        // A pooled ref (26/60000) sits far from the counter (86236) — mixing them made a run jump.
        _repo.Setup(r => r.GetUnusedRefsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[new HistologyRef { Ref = $"{ThisYear}/60000" }]);
        _repo.Setup(r => r.DrawNextRefRunAsync(HistologyRefTypeCode.MouseProjects, 3, 60000, 90000, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)["86236", "86237", "86238"]);

        var result = await CreateSut().GetNextAvailableRefRunAsync(HistologyRefTypeCode.MouseProjects, 3, ct: TestContext.Current.CancellationToken);

        Assert.Equal([$"{ThisYear}/86236", $"{ThisYear}/86237", $"{ThisYear}/86238"], result);
        _repo.Verify(r => r.GetUnusedRefsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        // The whole run is reserved in ONE repository call — never one draw per number.
        _repo.Verify(r => r.DrawNextRefRunAsync(It.IsAny<int>(), It.IsAny<int>(), 60000, 90000, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.DrawNextRefAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefRunAsync_RunLeavesTheRange_ReturnsNothing()
    {
        _repo.Setup(r => r.DrawNextRefRunAsync(HistologyRefTypeCode.MouseProjects, 2, 60000, 90000, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)["89999", "90000"]);

        var result = await CreateSut().GetNextAvailableRefRunAsync(HistologyRefTypeCode.MouseProjects, 2, ct: TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNextAvailableRefRunAsync_RepositoryReturnsAShortRun_ReturnsNothing()
    {
        _repo.Setup(r => r.DrawNextRefRunAsync(HistologyRefTypeCode.MouseProjects, 3, 60000, 90000, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)["86236", "86237"]);

        var result = await CreateSut().GetNextAvailableRefRunAsync(HistologyRefTypeCode.MouseProjects, 3, ct: TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNextAvailableRefRunAsync_ZeroCount_DrawsNothing()
    {
        var result = await CreateSut().GetNextAvailableRefRunAsync(HistologyRefTypeCode.MouseProjects, 0, ct: TestContext.Current.CancellationToken);

        Assert.Empty(result);
        _repo.Verify(r => r.DrawNextRefAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("40000")]
    [InlineData("59999")]
    public async Task GetNextAvailableRefAsync_GeneralPoolBoundsAreInclusive(string counterValue)
    {
        _repo.Setup(r => r.DrawNextRefAsync(HistologyRefTypeCode.GeneralPool, 60000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counterValue);

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.GeneralPool, ct: TestContext.Current.CancellationToken);

        Assert.Equal($"{ThisYear}/{counterValue}", result);
    }
}
