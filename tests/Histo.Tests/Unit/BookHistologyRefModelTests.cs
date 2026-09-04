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

    public BookHistologyRefModelTests()
    {
        _session.SetupProperty(s => s.AnimalID);
        _session.Setup(s => s.UserID).Returns(99);
    }

    private BookHistologyRefModel CreateSut() =>
        new(_session.Object, _refs.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnGetAsync_LoadsUnusedRefsForType1()
    {
        _refs.Setup(r => r.GetUnusedRefsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[new HistologyRef { Ref = "24/00001" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Single(sut.AvailableRefs);
    }

    [Fact]
    public async Task OnPostAsync_NullAnimalIdInSession_DoesNotRedirect_BUG()
    {
        // BUG: `Session.AnimalID <= 0` is false when AnimalID is null (nullable relational
        // comparisons against null are always false in C#), so this guard silently fails to
        // catch the "never selected an animal" case and falls through to book against
        // AnimalID 0 instead of redirecting like the explicit-zero case does below.
        _session.Object.AnimalID = null;
        _refs.Setup(r => r.BookRefAsync("24/00001", 0, 99, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();

        var result = await sut.OnPostAsync("24/00001");

        Assert.IsType<RedirectToPageResult>(result);
        _refs.Verify(r => r.BookRefAsync("24/00001", 0, 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ZeroAnimalIdInSession_RedirectsToIndex()
    {
        _session.Object.AnimalID = 0;
        var sut = CreateSut();

        var result = await sut.OnPostAsync("24/00001");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_BookingFails_ReturnsPageWithErrorAndReloadsRefs()
    {
        _session.Object.AnimalID = 42;
        _refs.Setup(r => r.BookRefAsync("24/00001", 42, 99, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _refs.Setup(r => r.GetUnusedRefsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<HistologyRef>)[]);
        var sut = CreateSut();

        var result = await sut.OnPostAsync("24/00001");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Could not book the selected reference.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_BookingSucceeds_RedirectsToSampleSummary()
    {
        _session.Object.AnimalID = 42;
        _refs.Setup(r => r.BookRefAsync("24/00001", 42, 99, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();

        var result = await sut.OnPostAsync("24/00001");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SampleSummary", redirect.PageName);
    }
}
