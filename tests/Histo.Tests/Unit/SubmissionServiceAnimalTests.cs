using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SubmissionService"/> — animal creation with PG-number
/// auto-reversal.
///
/// These tests verify that the PG-number business rule from clsAnimal.vb NewRecord()
/// is preserved correctly in the Phase 4 domain service layer.
/// </summary>
public class SubmissionServiceAnimalTests
{
    private readonly Mock<ISubmissionRepository> _repoMock = new();
    private readonly Mock<IAppLogger> _loggerMock = new();

    private SubmissionService BuildSut() =>
        new(_repoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task AddAnimalAsync_NeuropathWithPgRef_SetsAutoHistologyRef()
    {
        _repoMock
            .Setup(r => r.AddAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), default))
            .ReturnsAsync(42);

        var sut = BuildSut();
        await sut.AddAnimalAsync(
            batchSubmissionId: 1,
            senderRef: "PG012302",
            isNeuropath: true,
            userId: 99);

        _repoMock.Verify(r => r.AddAnimalAsync(
            It.Is<Animal>(a => a.HistologyRef == "02/00123" && a.IsPGNumber),
            99,
            default));
    }

    [Fact]
    public async Task AddAnimalAsync_NotNeuropath_SetsNoHistologyRef()
    {
        _repoMock
            .Setup(r => r.AddAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), default))
            .ReturnsAsync(1);

        var sut = BuildSut();
        await sut.AddAnimalAsync(
            batchSubmissionId: 1,
            senderRef: "PG012302",
            isNeuropath: false,
            userId: 1);

        _repoMock.Verify(r => r.AddAnimalAsync(
            It.Is<Animal>(a => a.HistologyRef == null && !a.IsPGNumber),
            1,
            default));
    }

    [Fact]
    public async Task AddAnimalAsync_NonPgRef_SetsNoHistologyRef()
    {
        _repoMock
            .Setup(r => r.AddAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), default))
            .ReturnsAsync(1);

        var sut = BuildSut();
        await sut.AddAnimalAsync(
            batchSubmissionId: 1,
            senderRef: "AB012302",
            isNeuropath: true,
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
        await sut.AddAnimalAsync(1, "REF001", false, 1);

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
        var result = await sut.AddAnimalAsync(1, "REF001", false, 1);

        Assert.Equal(0, result);
        _loggerMock.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<object[]>()));
    }
}
