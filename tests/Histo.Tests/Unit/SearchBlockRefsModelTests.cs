using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="SearchBlockRefsModel"/>.</summary>
public class SearchBlockRefsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockService> _blocks = new();

    private SearchBlockRefsModel CreateSut() =>
        new(_session.Object, _blocks.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnPostAsync_NeitherFieldEntered_ReturnsErrorMessage()
    {
        var sut = CreateSut();
        sut.SenderRef = null;
        sut.HistologyRef = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter either the Sender Ref or the Histology Ref, not both.", sut.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_BothFieldsEntered_ReturnsErrorMessage()
    {
        var sut = CreateSut();
        sut.SenderRef = "S123";
        sut.HistologyRef = "24/00001";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter either the Sender Ref or the Histology Ref, not both.", sut.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_OnlySenderRefEntered_SearchesBySenderRef()
    {
        _blocks.Setup(b => b.GetUsedBlockRefsBySenderRefAsync("S123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        var sut = CreateSut();
        sut.SenderRef = "S123";
        sut.HistologyRef = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(sut.ErrorMessage);
        _blocks.Verify(b => b.GetUsedBlockRefsBySenderRefAsync("S123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_OnlyHistologyRefEntered_SearchesByHistologyRef()
    {
        _blocks.Setup(b => b.GetUsedBlockRefsByHistologyRefAsync("24/00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        var sut = CreateSut();
        sut.SenderRef = null;
        sut.HistologyRef = "24/00001";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(sut.ErrorMessage);
        _blocks.Verify(b => b.GetUsedBlockRefsByHistologyRefAsync("24/00001", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_NoUsedBlocksFound_ReturnsSingleFullUnusedRange()
    {
        // BlockRefRangeHelpers.ComputeRanges always synthesises a full "01+" unused
        // range when there are no used blocks — it never returns an empty collection.
        _blocks.Setup(b => b.GetUsedBlockRefsBySenderRefAsync("S123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        var sut = CreateSut();
        sut.SenderRef = "S123";

        await sut.OnPostAsync();

        var row = Assert.Single(sut.Results);
        Assert.Equal("01+", row.UnusedBlockRefs);
    }
}
