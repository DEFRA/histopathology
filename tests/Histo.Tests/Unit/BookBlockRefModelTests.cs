using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Bookings;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="BookBlockRefModel"/>.</summary>
public class BookBlockRefModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    public BookBlockRefModelTests()
    {
        _session.Setup(s => s.UserID).Returns(99);
        _blocks.Setup(b => b.GetPreBookedByAnimalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[]);
        _blocks.Setup(b => b.CreatePreBookedBlockAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private BookBlockRefModel CreateSut() =>
        new(_session.Object, _blocks.Object, _submissions.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnPostAsync_NoSenderRefFrom_ReturnsError()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "";
        sut.BlockRefFrom = "01";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a Sender Ref from.", sut.Error);
    }

    [Theory]
    [InlineData("00")]
    [InlineData("000")]
    [InlineData("1")]
    public async Task OnPostAsync_InvalidBlockRefFrom_ReturnsError(string blockRef)
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "MC000001";
        sut.BlockRefFrom = blockRef;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The requested block ref range cannot be created.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_PlainSenderRef_CreatesNewAnimalAndBooksSingleBlock()
    {
        _submissions.Setup(s => s.GetAnimalBySenderAsync("ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[]);
        _submissions.Setup(s => s.AddAnimalAsync(0, "ABC123", false, 99, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(501);
        var sut = CreateSut();
        sut.SenderRefFrom = "ABC123";
        sut.BlockRefFrom = "01";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(sut.Error);
        _blocks.Verify(b => b.CreatePreBookedBlockAsync(501, "01", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(sut.ResultMessages, m => m.Contains("1 blocks booked, 0 blocks not booked"));
    }

    [Fact]
    public async Task OnPostAsync_ExistingAnimal_ReusesAnimalId()
    {
        _submissions.Setup(s => s.GetAnimalBySenderAsync("ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 77, SenderRef = "ABC123" }]);
        var sut = CreateSut();
        sut.SenderRefFrom = "ABC123";
        sut.BlockRefFrom = "01";

        await sut.OnPostAsync();

        _submissions.Verify(s => s.AddAnimalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _blocks.Verify(b => b.CreatePreBookedBlockAsync(77, "01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_BlockAlreadyExists_SkipsCreateAndReportsFailure()
    {
        _submissions.Setup(s => s.GetAnimalBySenderAsync("ABC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 77 }]);
        _blocks.Setup(b => b.GetPreBookedByAnimalAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, BlockRef = "01" }]);
        var sut = CreateSut();
        sut.SenderRefFrom = "ABC123";
        sut.BlockRefFrom = "01";

        await sut.OnPostAsync();

        _blocks.Verify(b => b.CreatePreBookedBlockAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains(sut.ResultMessages, m => m.Contains("already exists"));
    }

    [Fact]
    public async Task OnPostAsync_PgNumberRange_BooksBlocksForEverySampleInRange()
    {
        _submissions.Setup(s => s.GetAnimalBySenderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[]);
        _submissions.Setup(s => s.AddAnimalAsync(0, It.IsAny<string>(), false, 99, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var sut = CreateSut();
        sut.SenderRefFrom = "PG0001/24";
        sut.SenderRefTo = "PG0003/24";
        sut.BlockRefFrom = "01";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(sut.Error);
        _submissions.Verify(s => s.AddAnimalAsync(0, "PG0001/24", false, 99, null, false, It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.AddAnimalAsync(0, "PG0002/24", false, 99, null, false, It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.AddAnimalAsync(0, "PG0003/24", false, 99, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_PgNumberRangeWithDifferentYears_ReturnsError()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "PG0001/24";
        sut.SenderRefTo = "PG0003/25";
        sut.BlockRefFrom = "01";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("PG Number years must be the same.", sut.Error);
    }
}
