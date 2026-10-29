using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Pages.Bookings;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="EditHistologyRefModel"/>.</summary>
public class EditHistologyRefModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IHistologyRefService> _refs = new();

    private EditHistologyRefModel CreateSut(int type, string newRef) =>
        new(_session.Object, _refs.Object)
        {
            HistologyType = type,
            NewHistologyRef = newRef,
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public void OnGet_SetsPageTitles()
    {
        var sut = CreateSut(0, string.Empty);

        sut.OnGet();

        Assert.Equal("Edit Histology Reference", sut.ViewData["Title"]);
        Assert.Equal("Edit Histology Reference", sut.ViewData["PageTitle"]);
    }

    [Fact]
    public async Task OnPostAsync_NoTypeSelected_ReturnsPageWithTypeError()
    {
        var sut = CreateSut(0, "40001");

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("You must select a Histology Ref Type.", sut.Error);
        Assert.Equal("Edit Histology Reference", sut.ViewData["Title"]);
        Assert.Equal("Edit Histology Reference", sut.ViewData["PageTitle"]);
    }

    [Fact]
    public async Task OnPostAsync_BlankRef_ReturnsPageWithRefError()
    {
        var sut = CreateSut(HistologyRefTypeCode.GeneralPool, " ");

        await sut.OnPostAsync();

        Assert.Equal("You must enter a Histology Ref.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_TypeWithoutRange_ReturnsPageWithTypeError()
    {
        var sut = CreateSut(HistologyRefTypeCode.UsePgNumber, "40001");

        await sut.OnPostAsync();

        Assert.Equal("You must select a Histology Ref Type.", sut.Error);
        _refs.Verify(r => r.SetCounterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("4001")]
    [InlineData("040001")]
    [InlineData("4000a")]
    [InlineData("10000")]
    [InlineData("60000")]
    public async Task OnPostAsync_RefNotFiveDigitsInRange_ReturnsRangeError(string entered)
    {
        var sut = CreateSut(HistologyRefTypeCode.GeneralPool, entered);

        await sut.OnPostAsync();

        Assert.StartsWith("The Histology Ref must be a 5-digit number between 40000 and 59999", sut.Error);
        _refs.Verify(r => r.SetCounterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_SetCounterFails_ReturnsPageWithFailureError()
    {
        _refs.Setup(r => r.SetCounterAsync(HistologyRefTypeCode.GeneralPool, "40001", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut(HistologyRefTypeCode.GeneralPool, "40001");

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.StartsWith("Failed to update the Histology Reference.", sut.Error);
        Assert.Null(sut.SuccessMessage);
    }

    [Fact]
    public async Task OnPostAsync_ValidRef_SavesTrimmedCounterAndReportsSuccess()
    {
        _refs.Setup(r => r.SetCounterAsync(HistologyRefTypeCode.GeneralPool, "40001", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut(HistologyRefTypeCode.GeneralPool, " 40001 ");

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The Histology Reference has been updated.", sut.SuccessMessage);
        Assert.Null(sut.Error);
    }
}
