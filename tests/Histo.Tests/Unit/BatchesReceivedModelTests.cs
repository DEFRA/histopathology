using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="BatchesReceivedModel"/>.</summary>
public class BatchesReceivedModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public BatchesReceivedModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.IsViewSubmissionMode, true);
    }

    private BatchesReceivedModel CreateSut() =>
        new(_session.Object, _batches.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnGetAsync_LoadsReceivedBatches()
    {
        _batches.Setup(b => b.GetReceivedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 1 }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(1, sut.TotalCount);
    }

    [Fact]
    public void OnPostSelect_SetsSessionStateAndRedirectsToBatchDetails()
    {
        var sut = CreateSut();

        var result = sut.OnPostSelect(42);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchBlocks", redirect.PageName);
        Assert.Equal(42, _session.Object.BatchID);
        Assert.False(_session.Object.IsViewSubmissionMode);
    }

    [Fact]
    public async Task OnPostGoAsync_QuickGoIdSet_SetsSessionAndRedirects()
    {
        var sut = CreateSut();
        sut.QuickGoId = 99;
        _batches.Setup(b => b.GetReceivedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 99, Status = BatchStatus.Received });

        var result = await sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchBlocks", redirect.PageName);
        Assert.Equal(99, _session.Object.BatchID);
    }

    [Fact]
    public async Task OnPostGoAsync_ZeroQuickGoId_ShouldRejectInvalidInput()
    {
        var sut = CreateSut();
        sut.QuickGoId = 0;
        _batches.Setup(b => b.GetReceivedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[]);

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(_session.Object.BatchID);
    }

    [Fact]
    public async Task OnPostGoAsync_NullQuickGoId_ShowsValidationError()
    {
        var sut = CreateSut();
        sut.QuickGoId = null;
        _batches.Setup(b => b.GetReceivedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[]);

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(_session.Object.BatchID);
    }
}
