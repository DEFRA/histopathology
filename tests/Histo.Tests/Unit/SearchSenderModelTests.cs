using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="SearchSenderModel"/>.</summary>
public class SearchSenderModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    private SearchSenderModel CreateSut()
    {
        var http = new DefaultHttpContext();
        return new SearchSenderModel(_session.Object, _submissions.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = http,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>()),
        };
    }

    [Fact]
    public async Task OnGetAsync_NotPickerMode_UsesSearchTitleAndSearchesTrimmedSenderRef()
    {
        _submissions.Setup(s => s.GetAnimalsBySenderRefAsync("AB1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 1, SenderRef = "AB1" }]);
        var sut = CreateSut();
        sut.SenderRef = " AB1 ";

        await sut.OnGetAsync();

        Assert.Equal("Search by Sender", sut.ViewData["Title"]);
        Assert.Equal("Search by Sender", sut.ViewData["PageTitle"]);
        Assert.True(sut.HasSearched);
        Assert.Single(sut.Results);
    }

    [Fact]
    public async Task OnGetAsync_PickerMode_UsesPickerTitleAndSearchesAll()
    {
        _submissions.Setup(s => s.GetAnimalsBySenderRefAsync(string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[]);
        var sut = CreateSut();
        sut.ReturnPage = "/Submissions/CopyBatch";

        await sut.OnGetAsync();

        Assert.True(sut.IsPickerMode);
        Assert.Equal("Select sender ref", sut.ViewData["Title"]);
        Assert.Equal("Select sender ref", sut.ViewData["PageTitle"]);
        Assert.Empty(sut.Results);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPageWithResultsAndTitle()
    {
        _submissions.Setup(s => s.GetAnimalsBySenderRefAsync("X", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 2, SenderRef = "X" }]);
        var sut = CreateSut();
        sut.SenderRef = "X";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Search by Sender", sut.ViewData["Title"]);
        Assert.Equal("Search by Sender", sut.ViewData["PageTitle"]);
        Assert.Single(sut.GetPagedResults());
    }

    [Fact]
    public async Task OnPostAsync_PickerMode_UsesPickerTitle()
    {
        _submissions.Setup(s => s.GetAnimalsBySenderRefAsync(string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[]);
        var sut = CreateSut();
        sut.ReturnPage = "/Submissions/CopyBatch";

        await sut.OnPostAsync();

        Assert.Equal("Select sender ref", sut.ViewData["Title"]);
        Assert.Equal("Select sender ref", sut.ViewData["PageTitle"]);
    }

    [Fact]
    public void OnPostSelect_WithReturnPageAndId_StoresSelectionAndRedirectsWithSourceBatch()
    {
        var sut = CreateSut();

        var result = sut.OnPostSelect("AB1", "/Submissions/CopyBatch", 12);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/CopyBatch", redirect.PageName);
        Assert.Equal(12, redirect.RouteValues!["sourceBatchId"]);
        Assert.Equal("AB1", sut.TempData["SenderRefPicker_Selected"]);
    }

    [Fact]
    public void OnPostSelect_WithReturnPageOnly_RedirectsWithoutRouteValues()
    {
        var sut = CreateSut();

        var redirect = Assert.IsType<RedirectToPageResult>(sut.OnPostSelect("AB1", "/Submissions/CopyBatch", null));

        Assert.Equal("/Submissions/CopyBatch", redirect.PageName);
        Assert.Null(redirect.RouteValues);
    }

    [Fact]
    public void OnPostSelect_NoReturnPage_RedirectsToSamePage()
    {
        var sut = CreateSut();

        var redirect = Assert.IsType<RedirectToPageResult>(sut.OnPostSelect("AB1", null, null));

        Assert.Null(redirect.PageName);
    }
}
