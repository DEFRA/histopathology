using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="BatchesForDispatchModel"/>.</summary>
public class BatchesForDispatchModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public BatchesForDispatchModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.IsViewSubmissionMode, true);
    }

    private BatchesForDispatchModel CreateSut() =>
        new(_session.Object, _batches.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnGetAsync_LoadsForDispatchBatches()
    {
        _batches.Setup(b => b.GetForDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 1 }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(1, sut.TotalCount);
    }

    [Fact]
    public void OnPostSelect_SetsSessionStateAndRedirectsToQualityData()
    {
        var sut = CreateSut();

        var result = sut.OnPostSelect(42);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/QC/QualityData", redirect.PageName);
        Assert.Equal(42, _session.Object.BatchID);
        Assert.False(_session.Object.IsViewSubmissionMode);
    }

    [Fact]
    public async Task OnPostGoAsync_NoQuickGoId_ReturnsPageWithError()
    {
        _batches.Setup(b => b.GetForDispatchAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        var sut = CreateSut();
        sut.QuickGoId = null;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a submission number.", sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_BatchNotInDispatchList_ReturnsPageWithError()
    {
        _batches.Setup(b => b.GetForDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 1 }]);
        var sut = CreateSut();
        sut.QuickGoId = 99;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("could not be found or is not ready for quality data entry", sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_BatchInDispatchList_SetsSessionAndRedirects()
    {
        _batches.Setup(b => b.GetForDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 99 }]);
        var sut = CreateSut();
        sut.QuickGoId = 99;

        var result = await sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/QC/QualityData", redirect.PageName);
        Assert.Equal(99, _session.Object.BatchID);
    }
}
