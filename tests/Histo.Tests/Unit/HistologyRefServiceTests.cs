using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Histology.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Verifies <see cref="HistologyRefService.GetNextAvailableRefAsync"/> draws via the
/// <c>GetNextHistologyRef</c> SP (<see cref="IHistologyRepository.DrawNextRefAsync"/>), which reads
/// and advances the counter in one server-side step, rather than a racy
/// <see cref="IHistologyRepository.GetCountersAsync"/> + <see cref="IHistologyRepository.UpdateCounterAsync"/>
/// pair. Mirrors legacy <c>clsHistology.vb::GetNextAvailableHistologyRef</c>, which likewise never
/// consults the unused-refs pool — that is only ever searched by Sender Ref for a specific sample.
/// </summary>
public class HistologyRefServiceTests
{
    private readonly Mock<IHistologyRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private HistologyRefService CreateSut() => new(_repo.Object, _logger.Object);

    [Fact]
    public async Task GetNextAvailableRefAsync_DrawsFromTheCounterSpAndPrefixesTheYear()
    {
        _repo.Setup(r => r.DrawNextRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync("60002");

        var result = await CreateSut().GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects);

        Assert.Equal($"{DateTime.Now.Year % 100:D2}/60002", result);
        // The unused-refs pool is only ever searched by SenderRef in legacy, never used as the draw.
        _repo.Verify(r => r.GetUnusedRefsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.GetCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.UpdateCounterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
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
    }
}
