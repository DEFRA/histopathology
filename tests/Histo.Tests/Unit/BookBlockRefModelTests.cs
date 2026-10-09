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

/// <summary>Unit tests for <see cref="BookBlockRefModel"/> — pre-books block placeholders for a Sender Ref range.</summary>
public class BookBlockRefModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    private BookBlockRefModel CreateSut() =>
        new(_session.Object, _blocks.Object, _submissions.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnPostAsync_MissingSenderRefFrom_ReturnsError()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "";
        sut.BlockRefFrom = "01";

        await sut.OnPostAsync();

        Assert.Equal("Enter a Sender Ref from.", sut.Error);
        _blocks.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnPostAsync_InvalidBlockRefFrom_ReturnsError()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "S1";
        sut.BlockRefFrom = "not-a-number";

        await sut.OnPostAsync();

        Assert.Equal("The requested block ref range cannot be created.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_PlainSenderRef_BooksBlocksForFirstRefOnly()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "PLAINREF";
        sut.BlockRefFrom = "01";
        sut.BlockRefTo = "02";

        _blocks.Setup(b => b.TryGetUsedBlockRefsBySenderRefAsync("PLAINREF", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        _submissions.Setup(s => s.GetAnimalBySenderAsync("PLAINREF", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 7 }]);
        _blocks.Setup(b => b.CreatePreBookedBlockAsync(7, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await sut.OnPostAsync();

        Assert.Null(sut.Error);
        _blocks.Verify(b => b.CreatePreBookedBlockAsync(7, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Contains(sut.ResultMessages, m => m.Contains("2 blocks booked, 0 blocks not booked"));
    }

    [Fact]
    public async Task OnPostAsync_NoExistingAnimal_CreatesOne()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "NEWREF";
        sut.BlockRefFrom = "01";

        _blocks.Setup(b => b.TryGetUsedBlockRefsBySenderRefAsync("NEWREF", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        _submissions.Setup(s => s.GetAnimalBySenderAsync("NEWREF", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[]);
        _submissions.Setup(s => s.AddAnimalAsync(0, "NEWREF", It.IsAny<int>(), null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(9);
        _blocks.Setup(b => b.CreatePreBookedBlockAsync(9, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await sut.OnPostAsync();

        Assert.Null(sut.Error);
        _blocks.Verify(b => b.CreatePreBookedBlockAsync(9, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Blocks claimed by a received submission are Status Used against a BatchID — the old
    // pre-booked-placeholder check missed them and silently double-booked the refs.
    [Theory]
    [InlineData(BlockStatus.Used)]
    [InlineData(BlockStatus.PreBooked)]
    [InlineData(BlockStatus.PreBookedUsed)]
    public async Task OnPostAsync_BlockRefAlreadyExistsForSenderRef_DoesNotBook(int status)
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "RefTest";
        sut.BlockRefFrom = "01";

        _blocks.Setup(b => b.TryGetUsedBlockRefsBySenderRefAsync("RefTest", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[new UsedBlockRef { BlockRef = 1, Status = status }]);

        await sut.OnPostAsync();

        _blocks.Verify(b => b.CreatePreBookedBlockAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _submissions.Verify(s => s.AddAnimalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains(sut.ResultMessages, m => m == "Sample: RefTest Block 01 not booked as it already exists.");
        Assert.Null(sut.SuccessMessage);
    }

    [Fact]
    public async Task OnPostAsync_PartiallyExistingRange_BooksOnlyFreeRefs()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "RefTest";
        sut.BlockRefFrom = "01";
        sut.BlockRefTo = "03";

        _blocks.Setup(b => b.TryGetUsedBlockRefsBySenderRefAsync("RefTest", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)
            [
                new UsedBlockRef { BlockRef = 1, Status = BlockStatus.Used },
                new UsedBlockRef { BlockRef = 2, Status = BlockStatus.Used },
            ]);
        _submissions.Setup(s => s.GetAnimalBySenderAsync("RefTest", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 7 }]);
        _blocks.Setup(b => b.CreatePreBookedBlockAsync(7, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await sut.OnPostAsync();

        _blocks.Verify(b => b.CreatePreBookedBlockAsync(7, "03", It.IsAny<CancellationToken>()), Times.Once);
        _blocks.Verify(b => b.CreatePreBookedBlockAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(
        [
            "Sample: RefTest Block 01 not booked as it already exists.",
            "Sample: RefTest Block 02 not booked as it already exists.",
            "Sample: RefTest 1 blocks booked, 2 blocks not booked.",
        ], sut.ResultMessages);
    }

    // An empty result from a failed lookup would read as "the ref is free" and double-book it.
    [Fact]
    public async Task OnPostAsync_ExistingRefLookupFails_BooksNothing()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "RefTest";
        sut.BlockRefFrom = "01";

        _blocks.Setup(b => b.TryGetUsedBlockRefsBySenderRefAsync("RefTest", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>?)null);

        await sut.OnPostAsync();

        _blocks.Verify(b => b.CreatePreBookedBlockAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Null(sut.SuccessMessage);
        Assert.Contains(sut.ResultMessages, m => m.Contains("existing block ref check failed"));
    }
}
