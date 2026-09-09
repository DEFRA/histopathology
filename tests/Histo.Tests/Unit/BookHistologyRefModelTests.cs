using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Pages.Bookings;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="BookHistologyRefModel"/>.</summary>
public class BookHistologyRefModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IHistologyRefService> _refs = new();

    private BookHistologyRefModel CreateSut() =>
        new(_session.Object, _refs.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnGetAsync_LoadsCounters()
    {
        _refs.Setup(r => r.GetCountersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[new HistologyRefCounter { Type = 1, Description = "Neuropath", NextHistologyRef = "10000" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Single(sut.Counters);
    }

    [Fact]
    public async Task OnPostAsync_NoTypeSelected_ReturnsErrorWithoutBooking()
    {
        _refs.Setup(r => r.GetCountersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[]);
        var sut = CreateSut();
        sut.HistologyType = 0;
        sut.NumberToBook = 5;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("You must select a Histology Ref Range Type.", sut.Error);
        _refs.Verify(r => r.BookCounterRangeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_NoNumberEntered_ReturnsErrorWithoutBooking()
    {
        _refs.Setup(r => r.GetCountersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[]);
        var sut = CreateSut();
        sut.HistologyType = HistologyRefTypeCode.Neuropath;
        sut.NumberToBook = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("You must enter a valid Number Required (a whole number greater than zero).", sut.Error);
        _refs.Verify(r => r.BookCounterRangeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_BookingFails_ReturnsPageWithError()
    {
        _refs.Setup(r => r.GetCountersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[]);
        _refs.Setup(r => r.BookCounterRangeAsync(HistologyRefTypeCode.Neuropath, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistologyBookingResult { Success = false, Error = "Cannot book the required histology numbers as the maximum neuropath histology number is 19999." });
        var sut = CreateSut();
        sut.HistologyType = HistologyRefTypeCode.Neuropath;
        sut.NumberToBook = 5;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Cannot book the required histology numbers as the maximum neuropath histology number is 19999.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_BookingSucceeds_ReturnsPageWithSuccessMessage()
    {
        _refs.Setup(r => r.GetCountersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[]);
        _refs.Setup(r => r.BookCounterRangeAsync(HistologyRefTypeCode.Neuropath, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistologyBookingResult { Success = true, FirstBooked = 10000, LastBooked = 10004 });
        var sut = CreateSut();
        sut.HistologyType = HistologyRefTypeCode.Neuropath;
        sut.NumberToBook = 5;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("You have successfully booked Histology numbers in the range 10000 - 10004, inclusive.", sut.SuccessMessage);
    }
}
