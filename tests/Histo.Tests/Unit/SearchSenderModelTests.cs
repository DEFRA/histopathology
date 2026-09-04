using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
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

    private SearchSenderModel CreateSut() =>
        new(_session.Object, _submissions.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = Mock.Of<ITempDataDictionary>(),
        };

    [Fact]
    public void IsPickerMode_NoReturnPage_IsFalse()
    {
        var sut = CreateSut();
        sut.ReturnPage = null;

        Assert.False(sut.IsPickerMode);
    }

    [Fact]
    public void IsPickerMode_ReturnPageSet_IsTrue()
    {
        var sut = CreateSut();
        sut.ReturnPage = "/Batches/CopyBatch";

        Assert.True(sut.IsPickerMode);
    }

    [Fact]
    public async Task OnPostAsync_BlankSenderRef_DoesNotSearchAndHasSearchedIsFalse()
    {
        var sut = CreateSut();
        sut.SenderRef = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(sut.HasSearched);
        _submissions.Verify(s => s.GetAnimalsBySenderRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_SenderRefProvided_TrimsAndSearches()
    {
        _submissions.Setup(s => s.GetAnimalsBySenderRefAsync("S123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 1, SenderRef = "S123" }]);
        var sut = CreateSut();
        sut.SenderRef = "  S123  ";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.HasSearched);
        Assert.Single(sut.Results);
    }

    [Fact]
    public void OnPostSelect_WithReturnPageAndId_RedirectsWithSourceBatchIdRouteValue()
    {
        var sut = CreateSut();

        var result = sut.OnPostSelect("S123", "/Batches/CopyBatch", 42);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/CopyBatch", redirect.PageName);
        Assert.Equal(42, redirect.RouteValues!["sourceBatchId"]);
    }

    [Fact]
    public void OnPostSelect_WithReturnPageNoId_RedirectsWithoutRouteValues()
    {
        var sut = CreateSut();

        var result = sut.OnPostSelect("S123", "/Batches/CopyBatch", null);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/CopyBatch", redirect.PageName);
    }

    [Fact]
    public void OnPostSelect_NoReturnPage_RedirectsToSelf()
    {
        var sut = CreateSut();

        var result = sut.OnPostSelect("S123", null, null);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(redirect.PageName);
    }
}
