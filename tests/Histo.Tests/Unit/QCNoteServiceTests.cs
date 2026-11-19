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
            .Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), TestContext.Current.CancellationToken))
            .ThrowsAsync(new QCNoteConcurrencyException());

        var sut = BuildSut();

        await Assert.ThrowsAsync<QCNoteConcurrencyException>(
            () => sut.UpdateAsync(1, "text", new byte[] { 0x01 }, 99, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddAsync_RepositoryThrows_ReturnsZero()
    {
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<int>(), It.IsAny<int>(), TestContext.Current.CancellationToken))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var sut = BuildSut();
        var result = await sut.AddAsync(1, 99, ct: TestContext.Current.CancellationToken);

        Assert.Equal(0, result);
    }

    private void SetupNote(string text) =>
        _repoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QCNote { ID = 1, QCNoteRef = 5, Text = text, RowStamp = [7] });

    [Fact]
    public async Task AppendAsync_AddsOnlyNewLines_ToExistingText()
    {
        SetupNote("H&E (26/1 / 01): first");

        var result = await BuildSut().AppendAsync(5, ["H&E (26/1 / 01): first", "H&E (26/1 / 02): second"], 99, ct: TestContext.Current.CancellationToken);

        Assert.Equal(QCNoteAppendResult.Appended, result);
        _repoMock.Verify(r => r.UpdateAsync(5,
            "H&E (26/1 / 01): first" + Environment.NewLine + "H&E (26/1 / 02): second",
            It.IsAny<byte[]>(), 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AppendAsync_ShorterCommentThatIsPrefixOfExistingLine_IsStillAppended()
    {
        SetupNote("H&E (26/1 / 01): repeat stain\r\nOther line");

        var result = await BuildSut().AppendAsync(5, ["H&E (26/1 / 01): repeat"], 99, ct: TestContext.Current.CancellationToken);

        Assert.Equal(QCNoteAppendResult.Appended, result);
        _repoMock.Verify(r => r.UpdateAsync(5,
            It.Is<string>(s => s.EndsWith(Environment.NewLine + "H&E (26/1 / 01): repeat")),
            It.IsAny<byte[]>(), 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AppendAsync_RepeatedMultilineComment_IsNotAppendedAgain()
    {
        // The note already holds the whole two-line entry (stored with \r\n); the incoming one uses \n.
        SetupNote("Intro\r\nHistology - H&E (26/1 / 01): first line\r\nsecond line\r\nOther");

        var result = await BuildSut().AppendAsync(5, ["Histology - H&E (26/1 / 01): first line\nsecond line"], 99, ct: TestContext.Current.CancellationToken);

        Assert.Equal(QCNoteAppendResult.NothingToAppend, result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AppendAsync_MultilineCommentThatOnlyContainsExistingLines_IsStillAppended()
    {
        // Both physical lines exist separately, but not together as one entry.
        SetupNote("T (1 / 01): first line\r\nunrelated\r\nsecond line");

        var result = await BuildSut().AppendAsync(5, ["T (1 / 01): first line\r\nsecond line"], 99, ct: TestContext.Current.CancellationToken);

        Assert.Equal(QCNoteAppendResult.Appended, result);
        _repoMock.Verify(r => r.UpdateAsync(5,
            It.Is<string>(s => s.EndsWith(Environment.NewLine + "T (1 / 01): first line" + Environment.NewLine + "second line")),
            It.IsAny<byte[]>(), 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AppendAsync_AllLinesAlreadyPresent_DoesNotWrite()
    {
        SetupNote("H&E (26/1 / 01): first");

        var result = await BuildSut().AppendAsync(5, ["H&E (26/1 / 01): first"], 99, ct: TestContext.Current.CancellationToken);

        Assert.Equal(QCNoteAppendResult.NothingToAppend, result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AppendAsync_WouldExceedColumnSize_DoesNotWrite()
    {
        SetupNote(new string('x', QCNoteService.MaxTextLength - 5));

        var result = await BuildSut().AppendAsync(5, ["too long"], 99, ct: TestContext.Current.CancellationToken);

        Assert.Equal(QCNoteAppendResult.TooLong, result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AppendAsync_Concurrency_ReturnsConcurrency()
    {
        SetupNote("");
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new QCNoteConcurrencyException());

        var result = await BuildSut().AppendAsync(5, ["line"], 99, ct: TestContext.Current.CancellationToken);

        Assert.Equal(QCNoteAppendResult.Concurrency, result);
    }
}
