using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Pages.Bookings;
using Histo.Web.Services;
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

    private BookBlockRefModel CreateSut() =>
        new(_session.Object, _blocks.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    private void SetupBooking(string senderRef, params PreBookedBlockResult[] results) =>
        _blocks.Setup(b => b.BookPreBookedBlocksAsync(senderRef, It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PreBookedBlockResult>)results);

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

        SetupBooking("PLAINREF",
            new PreBookedBlockResult(1, PreBookedBlockOutcome.Booked),
            new PreBookedBlockResult(2, PreBookedBlockOutcome.Booked));

        await sut.OnPostAsync();

        Assert.Null(sut.Error);
        _blocks.Verify(b => b.BookPreBookedBlocksAsync(
            "PLAINREF",
            It.Is<IReadOnlyList<int>>(r => r.SequenceEqual(new[] { 1, 2 })),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(sut.ResultMessages, m => m.Contains("2 blocks booked, 0 blocks not booked"));
    }

    // Blocks claimed by a received submission are Status Used against a BatchID — the original
    // pre-booked-placeholder check missed them and silently double-booked the refs.
    [Fact]
    public async Task OnPostAsync_BlockRefAlreadyExistsForSenderRef_DoesNotBook()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "RefTest";
        sut.BlockRefFrom = "01";

        SetupBooking("RefTest", new PreBookedBlockResult(1, PreBookedBlockOutcome.AlreadyExists));

        await sut.OnPostAsync();

        Assert.Contains(sut.ResultMessages, m => m == "Sample: RefTest Block 01 not booked as it already exists.");
        Assert.Null(sut.SuccessMessage);
    }

    [Fact]
    public async Task OnPostAsync_PartiallyExistingRange_ReportsLegacyMessageOrder()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "RefTest";
        sut.BlockRefFrom = "01";
        sut.BlockRefTo = "03";

        SetupBooking("RefTest",
            new PreBookedBlockResult(1, PreBookedBlockOutcome.AlreadyExists),
            new PreBookedBlockResult(2, PreBookedBlockOutcome.AlreadyExists),
            new PreBookedBlockResult(3, PreBookedBlockOutcome.Booked));

        await sut.OnPostAsync();

        Assert.Equal(
        [
            "Sample: RefTest Block 01 not booked as it already exists.",
            "Sample: RefTest Block 02 not booked as it already exists.",
            "Sample: RefTest 1 blocks booked, 2 blocks not booked.",
        ], sut.ResultMessages);
    }

    [Fact]
    public async Task OnPostAsync_SampleCouldNotBeResolved_ReportsFailure()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "RefTest";
        sut.BlockRefFrom = "01";

        SetupBooking("RefTest", new PreBookedBlockResult(1, PreBookedBlockOutcome.NoSample));

        await sut.OnPostAsync();

        Assert.Contains(sut.ResultMessages, m => m.Contains("failed to retrieve or create sample data"));
        Assert.Null(sut.SuccessMessage);
    }

    // A failed booking must not read as success, or the user assumes the refs are reserved.
    [Fact]
    public async Task OnPostAsync_BookingFails_ReportsFailureAndNoSuccessMessage()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "RefTest";
        sut.BlockRefFrom = "01";

        _blocks.Setup(b => b.BookPreBookedBlocksAsync("RefTest", It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PreBookedBlockResult>?)null);

        await sut.OnPostAsync();

        Assert.Null(sut.SuccessMessage);
        Assert.Contains(sut.ResultMessages, m => m.Contains("booking failed"));
    }

    [Fact]
    public async Task OnPostAsync_MouseNumberRange_BooksEachSenderRefInRange()
    {
        var sut = CreateSut();
        sut.SenderRefFrom = "MC000001";
        sut.SenderRefTo = "MC000002";
        sut.BlockRefFrom = "01";

        SetupBooking("MC000001", new PreBookedBlockResult(1, PreBookedBlockOutcome.Booked));
        SetupBooking("MC000002", new PreBookedBlockResult(1, PreBookedBlockOutcome.Booked));

        await sut.OnPostAsync();

        _blocks.Verify(b => b.BookPreBookedBlocksAsync("MC000001", It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        _blocks.Verify(b => b.BookPreBookedBlocksAsync("MC000002", It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
