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

/// <summary>Unit tests for <see cref="BatchesForEditingModel"/>.</summary>
public class BatchesForEditingModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public BatchesForEditingModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
        _session.SetupProperty(s => s.IsViewSubmissionMode, true);
    }

    private BatchesForEditingModel CreateSut() =>
        new(_session.Object, _batches.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnGetAsync_LoadsAllBatches()
    {
        _batches.Setup(b => b.GetAllBatchesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 1 }, new BatchListResult { ID = 2 }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(2, sut.TotalCount);
    }

    [Fact]
    public void OnPostSelect_SetsSessionStateAndRedirectsToEditBatch()
    {
        var sut = CreateSut();

        var result = sut.OnPostSelect(42);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/EditBatch", redirect.PageName);
        Assert.Equal(42, _session.Object.BatchID);
        Assert.Equal("/Batches/BatchesForEditing", _session.Object.ReturnPage);
        Assert.False(_session.Object.IsViewSubmissionMode);
    }

    [Fact]
    public async Task OnPostGoAsync_NoQuickGoId_ReturnsPageWithError()
    {
        _batches.Setup(b => b.GetAllBatchesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        var sut = CreateSut();
        sut.QuickGoId = null;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a submission number.", sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_BatchNotFound_ReturnsPageWithError()
    {
        _batches.Setup(b => b.GetAllBatchesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.QuickGoId = 99;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Submission 99 could not be found.", sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_BatchFound_SetsSessionAndRedirects()
    {
        _batches.Setup(b => b.GetAllBatchesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 99 });
        var sut = CreateSut();
        sut.QuickGoId = 99;

        var result = await sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/EditBatch", redirect.PageName);
        Assert.Equal(99, _session.Object.BatchID);
    }
}
