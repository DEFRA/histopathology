using Histo.QualityControl.Models;
using Histo.QualityControl.Services;
using Histo.Infrastructure;
using Histo.QualityControl.Interfaces;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="QCNoteService"/> — focusing on concurrency handling
/// and error containment.
/// </summary>
public class QCNoteServiceTests
{
    private readonly Mock<IQCNoteRepository> _repoMock = new();
    private readonly Mock<IAppLogger> _loggerMock = new();

    private QCNoteService BuildSut() =>
        new(_repoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task UpdateAsync_ConcurrencyException_Propagates()
    {
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), default))
            .ThrowsAsync(new QCNoteConcurrencyException());

        var sut = BuildSut();

        await Assert.ThrowsAsync<QCNoteConcurrencyException>(
            () => sut.UpdateAsync(1, "text", new byte[] { 0x01 }, 99));
    }

    [Fact]
    public async Task GetBySubmissionAsync_RepositoryThrows_ReturnsEmptyList()
    {
        _repoMock
            .Setup(r => r.GetBySubmissionAsync(It.IsAny<int>(), default))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = BuildSut();
        var result = await sut.GetBySubmissionAsync(1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_RepositoryThrows_ReturnsZero()
    {
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<int>(), It.IsAny<int>(), default))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = BuildSut();
        var result = await sut.AddAsync(1, 99);

        Assert.Equal(0, result);
    }
}
