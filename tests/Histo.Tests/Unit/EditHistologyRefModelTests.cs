using Histo.Histology.Interfaces;
using Histo.Web.Pages.Bookings;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="EditHistologyRefModel"/> — the pool-level "next
/// histology ref" counter maintenance page.
/// </summary>
public class EditHistologyRefModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IHistologyRefService> _refs = new();

    public EditHistologyRefModelTests()
    {
        _session.Setup(s => s.UserID).Returns(99);
    }

    private EditHistologyRefModel CreateSut() =>
        new(_session.Object, _refs.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnPostAsync_NoHistologyTypeSelected_ReturnsPageWithError()
    {
        var sut = CreateSut();
        sut.HistologyType = 0;
        sut.NewHistologyRef = "24/00123";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("You must select a Histology Ref Type.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_EmptyNewHistologyRef_ReturnsPageWithError()
    {
        var sut = CreateSut();
        sut.HistologyType = 1;
        sut.NewHistologyRef = "   ";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("You must enter a Histology Ref.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_RepositoryReturnsFalse_ReturnsPageWithConcurrencyError()
    {
        _refs.Setup(r => r.UpdateRefAsync("24/00123", 1, 99, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();
        sut.HistologyType = 1;
        sut.NewHistologyRef = "24/00123";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to update the Histology Reference. Another user may have altered the record — please try again.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_Success_SetsSuccessMessage()
    {
        _refs.Setup(r => r.UpdateRefAsync("24/00123", 1, 99, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.HistologyType = 1;
        sut.NewHistologyRef = "24/00123";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The Histology Reference has been updated.", sut.SuccessMessage);
        Assert.Null(sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_TrimsWhitespaceBeforeCallingRepository()
    {
        _refs.Setup(r => r.UpdateRefAsync("24/00123", 1, 99, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.HistologyType = 1;
        sut.NewHistologyRef = "  24/00123  ";

        await sut.OnPostAsync();

        _refs.Verify(r => r.UpdateRefAsync("24/00123", 1, 99, It.IsAny<CancellationToken>()), Times.Once);
    }
}
