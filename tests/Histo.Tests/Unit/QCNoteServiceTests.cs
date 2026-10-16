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
    public async Task AddAsync_RepositoryThrows_ReturnsZero()
    {
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<int>(), It.IsAny<int>(), default))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = BuildSut();
        var result = await sut.AddAsync(1, 99);

        Assert.Equal(0, result);
    }

    private void SetupNote(string text) =>
        _repoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QCNote { ID = 1, QCNoteRef = 5, Text = text, RowStamp = [7] });

    [Fact]
    public async Task AppendAsync_AddsOnlyNewLines_ToExistingText()
    {
        SetupNote("H&E (26/1 / 01): first");

        var result = await BuildSut().AppendAsync(5, ["H&E (26/1 / 01): first", "H&E (26/1 / 02): second"], 99);

        Assert.Equal(QCNoteAppendResult.Appended, result);
        _repoMock.Verify(r => r.UpdateAsync(5,
            "H&E (26/1 / 01): first" + Environment.NewLine + "H&E (26/1 / 02): second",
            It.IsAny<byte[]>(), 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AppendAsync_AllLinesAlreadyPresent_DoesNotWrite()
    {
        SetupNote("H&E (26/1 / 01): first");

        var result = await BuildSut().AppendAsync(5, ["H&E (26/1 / 01): first"], 99);

        Assert.Equal(QCNoteAppendResult.NothingToAppend, result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AppendAsync_WouldExceedColumnSize_DoesNotWrite()
    {
        SetupNote(new string('x', QCNoteService.MaxTextLength - 5));

        var result = await BuildSut().AppendAsync(5, ["too long"], 99);

        Assert.Equal(QCNoteAppendResult.TooLong, result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AppendAsync_Concurrency_ReturnsConcurrency()
    {
        SetupNote("");
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new QCNoteConcurrencyException());

        var result = await BuildSut().AppendAsync(5, ["line"], 99);

        Assert.Equal(QCNoteAppendResult.Concurrency, result);
    }
}
