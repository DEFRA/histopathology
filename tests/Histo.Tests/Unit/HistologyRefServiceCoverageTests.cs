using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

public class HistologyRefServiceCoverageTests
{
    private readonly Mock<IHistologyRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static InvalidOperationException Boom => new("boom");

    private HistologyRefService Sut() => new(_repo.Object, _logger.Object);

    private void VerifyLogged() =>
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception?>(), It.IsAny<object[]>()), Times.AtLeastOnce);

    private static HistologyRefCounter Counter(int type, string next, byte[]? stamp = null) =>
        new() { Type = type, NextHistologyRef = next, RowStamp = stamp };

    // ---------- simple reads ----------

    [Fact]
    public async Task GetUnusedRefsAsync_ReturnsRefsOrEmptyOnFailure()
    {
        IReadOnlyList<HistologyRef> refs = [new HistologyRef { Ref = "26/10001" }];
        _repo.Setup(r => r.GetUnusedRefsAsync(1, Ct)).ReturnsAsync(refs);
        Assert.Same(refs, await Sut().GetUnusedRefsAsync(1, Ct));

        _repo.Setup(r => r.GetUnusedRefsAsync(1, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetUnusedRefsAsync(1, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetUsedRefsByBatchAsync_ReturnsRefsOrEmptyOnFailure()
    {
        IReadOnlyList<HistologyRef> refs = [new HistologyRef { Ref = "26/10001" }];
        _repo.Setup(r => r.GetUsedRefsByBatchAsync(3, Ct)).ReturnsAsync(refs);
        Assert.Same(refs, await Sut().GetUsedRefsByBatchAsync(3, Ct));

        _repo.Setup(r => r.GetUsedRefsByBatchAsync(3, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetUsedRefsByBatchAsync(3, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetUnusedBookedRefsAsync_ReturnsRefsOrEmptyOnFailure()
    {
        IReadOnlyList<HistologyRef> refs = [new HistologyRef { Ref = "26/10001" }];
        _repo.Setup(r => r.GetUnusedBookedRefsAsync(Ct)).ReturnsAsync(refs);
        Assert.Same(refs, await Sut().GetUnusedBookedRefsAsync(Ct));

        _repo.Setup(r => r.GetUnusedBookedRefsAsync(Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetUnusedBookedRefsAsync(Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetAllUnusedRefsAsync_ReturnsRefsOrEmptyOnFailure()
    {
        IReadOnlyList<HistologyRef> refs = [new HistologyRef { Ref = "26/10001" }];
        _repo.Setup(r => r.GetAllUnusedRefsAsync(Ct)).ReturnsAsync(refs);
        Assert.Same(refs, await Sut().GetAllUnusedRefsAsync(Ct));

        _repo.Setup(r => r.GetAllUnusedRefsAsync(Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetAllUnusedRefsAsync(Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetCountersAsync_ReturnsCountersOrEmptyOnFailure()
    {
        IReadOnlyList<HistologyRefCounter> counters = [Counter(1, "10001")];
        _repo.Setup(r => r.GetCountersAsync(Ct)).ReturnsAsync(counters);
        Assert.Same(counters, await Sut().GetCountersAsync(Ct));

        _repo.Setup(r => r.GetCountersAsync(Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetCountersAsync(Ct));
        VerifyLogged();
    }

    // ---------- SetCounter ----------

    [Fact]
    public async Task SetCounterAsync_KnownType_UpdatesWithExistingRowStamp()
    {
        var stamp = new byte[] { 9 };
        _repo.Setup(r => r.GetCountersAsync(Ct)).ReturnsAsync([Counter(1, "10001"), Counter(4, "40001", stamp)]);

        Assert.True(await Sut().SetCounterAsync(4, "40050", Ct));

        _repo.Verify(r => r.UpdateCounterAsync(4, "40050", stamp, Ct), Times.Once);
    }

    [Fact]
    public async Task SetCounterAsync_UnknownType_ReturnsFalseWithoutUpdating()
    {
        _repo.Setup(r => r.GetCountersAsync(Ct)).ReturnsAsync([Counter(1, "10001")]);

        Assert.False(await Sut().SetCounterAsync(4, "40050", Ct));

        _repo.Verify(r => r.UpdateCounterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]?>(), Ct), Times.Never);
    }

    [Fact]
    public async Task SetCounterAsync_RepositoryThrows_ReturnsFalseAndLogs()
    {
        _repo.Setup(r => r.GetCountersAsync(Ct)).ThrowsAsync(Boom);

        Assert.False(await Sut().SetCounterAsync(4, "40050", Ct));
        VerifyLogged();
    }

    // ---------- BookCounterRange ----------

    [Fact]
    public async Task BookCounterRangeAsync_WithinBound_ReservesRangeAndAdvancesCounter()
    {
        var stamp = new byte[] { 3 };
        _repo.Setup(r => r.GetCountersAsync(Ct)).ReturnsAsync([Counter(HistologyRefTypeCode.Neuropath, "10010", stamp)]);

        var result = await Sut().BookCounterRangeAsync(HistologyRefTypeCode.Neuropath, 5, Ct);

        Assert.True(result.Success);
        Assert.Equal((10010, 10014), (result.FirstBooked, result.LastBooked));
        _repo.Verify(r => r.UpdateCounterAsync(HistologyRefTypeCode.Neuropath, "10015", stamp, Ct), Times.Once);
    }

    [Theory]
    [InlineData(HistologyRefTypeCode.Neuropath, "19998", "neuropath", 19999)]
    [InlineData(HistologyRefTypeCode.AbattoirSurvey, "29998", "abattoir survey", 29999)]
    [InlineData(HistologyRefTypeCode.TBDiagnostic, "39998", "TB diagnostic", 39999)]
    [InlineData(HistologyRefTypeCode.GeneralPool, "59998", "general pool", 59999)]
    [InlineData(HistologyRefTypeCode.MouseProjects, "89998", "mouse project", 89999)]
    public async Task BookCounterRangeAsync_ExceedsTypeUpperBound_FailsWithMaxMessage(int type, string current, string name, int max)
    {
        _repo.Setup(r => r.GetCountersAsync(Ct)).ReturnsAsync([Counter(type, current)]);

        var result = await Sut().BookCounterRangeAsync(type, 5, Ct);

        Assert.False(result.Success);
        Assert.Equal($"Cannot book the required histology numbers as the maximum {name} histology number is {max}.", result.Error);
        _repo.Verify(r => r.UpdateCounterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]?>(), Ct), Times.Never);
    }

    [Fact]
    public async Task BookCounterRangeAsync_TypeWithoutUpperBound_BooksWithoutLimit()
    {
        _repo.Setup(r => r.GetCountersAsync(Ct)).ReturnsAsync([Counter(HistologyRefTypeCode.UsePgNumber, "99990")]);

        var result = await Sut().BookCounterRangeAsync(HistologyRefTypeCode.UsePgNumber, 20, Ct);

        Assert.True(result.Success);
        _repo.Verify(r => r.UpdateCounterAsync(HistologyRefTypeCode.UsePgNumber, "100010", null, Ct), Times.Once);
    }

    [Fact]
    public async Task BookCounterRangeAsync_UnknownType_Fails()
    {
        _repo.Setup(r => r.GetCountersAsync(Ct)).ReturnsAsync([Counter(1, "10001")]);

        var result = await Sut().BookCounterRangeAsync(5, 2, Ct);

        Assert.False(result.Success);
        Assert.Equal("The selected Histology Ref Type could not be found.", result.Error);
    }

    [Fact]
    public async Task BookCounterRangeAsync_NonNumericCounter_Fails()
    {
        _repo.Setup(r => r.GetCountersAsync(Ct)).ReturnsAsync([Counter(1, "ABCDE")]);

        var result = await Sut().BookCounterRangeAsync(1, 2, Ct);

        Assert.False(result.Success);
        Assert.Equal("The current Histology Ref counter is invalid.", result.Error);
    }

    [Fact]
    public async Task BookCounterRangeAsync_RepositoryThrows_FailsWithRetryMessage()
    {
        _repo.Setup(r => r.GetCountersAsync(Ct)).ThrowsAsync(Boom);

        var result = await Sut().BookCounterRangeAsync(1, 2, Ct);

        Assert.False(result.Success);
        Assert.Contains("try again", result.Error);
        VerifyLogged();
    }

    // ---------- GetNextAvailableRef failure paths ----------

    [Theory]
    [InlineData(HistologyRefTypeCode.UsePgNumber)]
    [InlineData(99)]
    public async Task GetNextAvailableRefAsync_TypeWithoutRange_ReturnsNullWithoutQuerying(int type)
    {
        Assert.Null(await Sut().GetNextAvailableRefAsync(type, Ct));

        _repo.Verify(r => r.GetUnusedRefsAsync(It.IsAny<int>(), Ct), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_PoolThrows_ReturnsNullAndLogs()
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(1, Ct)).ThrowsAsync(Boom);

        Assert.Null(await Sut().GetNextAvailableRefAsync(1, Ct));
        VerifyLogged();
    }

    [Theory]
    [InlineData("ABCDE")]
    [InlineData("09999")]
    [InlineData("20000")]
    public async Task GetNextAvailableRefAsync_CounterOutsideTypeRange_ReturnsNull(string claimed)
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(1, Ct)).ReturnsAsync([]);
        _repo.Setup(r => r.DrawNextRefAsync(1, Ct)).ReturnsAsync(claimed);

        Assert.Null(await Sut().GetNextAvailableRefAsync(1, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetNextAvailableRefAsync_CounterEmpty_ReturnsNull()
    {
        _repo.Setup(r => r.GetUnusedRefsAsync(1, Ct)).ReturnsAsync([]);
        _repo.Setup(r => r.DrawNextRefAsync(1, Ct)).ReturnsAsync((string?)null);

        Assert.Null(await Sut().GetNextAvailableRefAsync(1, Ct));
    }

    // ---------- GetNextAvailableRefRun failure paths ----------

    [Fact]
    public async Task GetNextAvailableRefRunAsync_RunOutsideRange_ReturnsEmptyAndLogs()
    {
        _repo.Setup(r => r.DrawNextRefRunAsync(1, 2, 10000, 20000, Ct)).ReturnsAsync(["19999", "20000"]);

        Assert.Empty(await Sut().GetNextAvailableRefRunAsync(1, 2, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetNextAvailableRefRunAsync_NonNumericRun_ReturnsEmpty()
    {
        _repo.Setup(r => r.DrawNextRefRunAsync(1, 2, 10000, 20000, Ct)).ReturnsAsync(["10001", "ZZZZZ"]);

        Assert.Empty(await Sut().GetNextAvailableRefRunAsync(1, 2, Ct));
    }

    [Fact]
    public async Task GetNextAvailableRefRunAsync_WrongCount_ReturnsEmpty()
    {
        _repo.Setup(r => r.DrawNextRefRunAsync(1, 3, 10000, 20000, Ct)).ReturnsAsync(["10001"]);

        Assert.Empty(await Sut().GetNextAvailableRefRunAsync(1, 3, Ct));
    }

    [Fact]
    public async Task GetNextAvailableRefRunAsync_TypeWithoutRange_ReturnsEmpty()
    {
        Assert.Empty(await Sut().GetNextAvailableRefRunAsync(HistologyRefTypeCode.UsePgNumber, 2, Ct));

        _repo.Verify(r => r.DrawNextRefRunAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), Ct), Times.Never);
    }

    [Fact]
    public async Task GetNextAvailableRefRunAsync_RepositoryThrows_ReturnsEmptyAndLogs()
    {
        _repo.Setup(r => r.DrawNextRefRunAsync(1, 2, 10000, 20000, Ct)).ThrowsAsync(Boom);

        Assert.Empty(await Sut().GetNextAvailableRefRunAsync(1, 2, Ct));
        VerifyLogged();
    }
}
