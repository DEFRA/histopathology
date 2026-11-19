using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SubmissionService.GetImportedDataAsync"/> — the
/// ViewImportedData.aspx replacement (legacy source: clsAnimal.vb GetImportedData).
/// </summary>
public class SubmissionServiceImportedDataTests
{
    private readonly Mock<ISubmissionRepository> _repoMock = new();
    private readonly Mock<IAppLogger> _loggerMock = new();

    private SubmissionService BuildSut() =>
        new(_repoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task GetImportedDataAsync_RepositoryReturnsRows_ReturnsThem()
    {
        var rows = new List<ImportedDataRow> { new() { SenderRef = "S1", Tissue = "Liver" } };
        _repoMock
            .Setup(r => r.GetImportedDataAsync("3", TestContext.Current.CancellationToken))
            .ReturnsAsync(rows);

        var sut = BuildSut();
        var result = await sut.GetImportedDataAsync("3", ct: TestContext.Current.CancellationToken);

        var row = Assert.Single(result);
        Assert.Equal("S1", row.SenderRef);
    }

    [Fact]
    public async Task GetImportedDataAsync_RepositoryThrows_ReturnsEmptyList()
    {
        _repoMock
            .Setup(r => r.GetImportedDataAsync(It.IsAny<string?>(), TestContext.Current.CancellationToken))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = BuildSut();
        var result = await sut.GetImportedDataAsync("3", ct: TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }
}
