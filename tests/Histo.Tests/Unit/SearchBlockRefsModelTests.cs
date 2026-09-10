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
    public async Task OnGetAsync_NeitherFieldEntered_ShowsEmptyResults()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Empty(sut.Errors);
        Assert.Empty(sut.Results);
    }

    [Fact]
    public async Task OnGetAsync_BothFieldsEntered_ReturnsValidationError()
    {
        var sut = CreateSut();
        sut.SenderRef = "S123";
        sut.HistologyRef = "24/00001";

        await sut.OnGetAsync();

        Assert.Contains(nameof(SearchBlockRefsModel.SenderRef), sut.Errors.Keys);
        Assert.Equal("Enter either the Sender Ref or the Histology Ref, not both.", sut.Errors[nameof(SearchBlockRefsModel.SenderRef)]);
    }

    [Fact]
    public async Task OnGetAsync_OnlySenderRefEntered_SearchesBySenderRef()
    {
        _blocks.Setup(b => b.GetUsedBlockRefsBySenderRefAsync("S123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        var sut = CreateSut();
        sut.SenderRef = "S123";

        await sut.OnGetAsync();

        Assert.True(sut.Searched);
        _blocks.Verify(b => b.GetUsedBlockRefsBySenderRefAsync("S123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_OnlyHistologyRefEntered_SearchesByHistologyRef()
    {
        _blocks.Setup(b => b.GetUsedBlockRefsByHistologyRefAsync("24/00001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        var sut = CreateSut();
        sut.HistologyRef = "24/00001";

        await sut.OnGetAsync();

        Assert.True(sut.Searched);
        _blocks.Verify(b => b.GetUsedBlockRefsByHistologyRefAsync("24/00001", It.IsAny<CancellationToken>()), Times.Once);
    }
}
