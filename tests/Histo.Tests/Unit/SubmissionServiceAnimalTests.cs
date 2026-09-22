using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for SubmissionService animal creation.
///
/// The PG-number auto-reversal / Neuropath-specific behaviour previously
/// covered here was removed together with the Neuropath user area - see
/// docs/Mouse-Bioassay-Neuropath-Removal-Analysis.md, section 3, item 4.
/// </summary>
public class SubmissionServiceAnimalTests
{
    private readonly Mock<ISubmissionRepository> _repoMock = new();
    private readonly Mock<IAppLogger> _loggerMock = new();

    private SubmissionService BuildSut() =>
        new(_repoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task AddAnimalAsync_SetsNoHistologyRef()
    {
        _repoMock
            .Setup(r => r.AddAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), default))
            .ReturnsAsync(1);

        var sut = BuildSut();
        await sut.AddAnimalAsync(
            batchSubmissionId: 1,
            senderRef: "PG012302",
            userId: 1);

        _repoMock.Verify(r => r.AddAnimalAsync(
            It.Is<Animal>(a => a.HistologyRef == null && !a.IsPGNumber),
            1,
            default));
    }

    [Fact]
    public async Task AddAnimalAsync_SetsDefaultNextBlockRef_To01()
    {
        _repoMock
            .Setup(r => r.AddAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), default))
            .ReturnsAsync(1);

        var sut = BuildSut();
        await sut.AddAnimalAsync(1, "REF001", 1);

        _repoMock.Verify(r => r.AddAnimalAsync(
            It.Is<Animal>(a => a.NextBlockRef == "01"),
            1,
            default));
    }

    [Fact]
    public async Task AddAnimalAsync_RepositoryThrows_ReturnsZero()
    {
        _repoMock
            .Setup(r => r.AddAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), default))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = BuildSut();
        var result = await sut.AddAnimalAsync(1, "REF001", 1);

        Assert.Equal(0, result);
        _loggerMock.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()));
    }
}
