using Histo.Histology.Interfaces;
using Histo.Histology.Services;
using Histo.Infrastructure;
using Moq;

namespace Histo.Tests.Unit;

public class BlockTestServiceTests
{
    private static readonly string[] Histology = ["2"];
    private static readonly string[] Antibodies = ["A1"];
    private static readonly string[] Stains = ["S1"];

    private readonly Mock<IBlockTestRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private BlockTestService CreateSut() => new(_repo.Object, _logger.Object);

    [Fact]
    public async Task AddTestsToBatchBlocksAsync_DelegatesToRepository()
    {
        await CreateSut().AddTestsToBatchBlocksAsync(5, Histology, Antibodies, Stains, 7);

        _repo.Verify(r => r.AddTestsToBatchBlocksAsync(5, Histology, Antibodies, Stains, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTestsToBatchBlocksAsync_RepositoryThrows_RethrowsSoCallerDoesNotReportSuccess()
    {
        _repo.Setup(r => r.AddTestsToBatchBlocksAsync(
                It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().AddTestsToBatchBlocksAsync(5, Histology, Antibodies, Stains, 7));
    }
}
