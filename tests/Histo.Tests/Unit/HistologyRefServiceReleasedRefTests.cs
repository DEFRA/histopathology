using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

/// <summary><see cref="HistologyRefService.FindReleasedRefForSenderAsync"/>: only a ref this sender released, of the asked type and year.</summary>
public class HistologyRefServiceReleasedRefTests
{
    private readonly Mock<IHistologyRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private static string ThisYear => $"{DateTime.Now.Year % 100:D2}";

    private HistologyRefService CreateSut() => new(_repo.Object, _logger.Object);

    private void GivenPool(params (string Ref, string Senders)[] rows) =>
        _repo.Setup(r => r.GetReleasedRefsForSenderAsync(It.IsAny<string>(), $"{ThisYear}/", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[.. rows.Select(r => new HistologyRef { Ref = r.Ref, SenderRef = r.Senders })]);

    [Fact]
    public async Task ReturnsTheLowestRefOfTheTypeReleasedByThatSender()
    {
        GivenPool(($"{ThisYear}/10009", "HISTO1"), ($"{ThisYear}/10004", "OTHER, HISTO1"), ($"{ThisYear}/40001", "HISTO1"));

        var result = await CreateSut().FindReleasedRefForSenderAsync("HISTO1", HistologyRefTypeCode.Neuropath, TestContext.Current.CancellationToken);

        Assert.Equal($"{ThisYear}/10004", result);
    }

    [Fact]
    public async Task IgnoresRowsWhereTheSenderRefOnlyAppearsInsideAnotherSenderRef()
    {
        GivenPool(($"{ThisYear}/10004", "HISTO10"));

        var result = await CreateSut().FindReleasedRefForSenderAsync("HISTO1", HistologyRefTypeCode.Neuropath, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task BlankSenderRef_DoesNotQueryThePool()
    {
        var result = await CreateSut().FindReleasedRefForSenderAsync(" ", HistologyRefTypeCode.Neuropath, TestContext.Current.CancellationToken);

        Assert.Null(result);
        _repo.Verify(r => r.GetReleasedRefsForSenderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RepositoryFailure_ReturnsNullSoTheCallerDrawsFromTheCounter()
    {
        _repo.Setup(r => r.GetReleasedRefsForSenderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await CreateSut().FindReleasedRefForSenderAsync("HISTO1", HistologyRefTypeCode.Neuropath, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
}
