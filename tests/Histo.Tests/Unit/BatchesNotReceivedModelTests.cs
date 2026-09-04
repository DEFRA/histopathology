using Histo.Core.Domain;
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

/// <summary>Unit tests for <see cref="BatchesNotReceivedModel"/> — the "Batches not received" grid.</summary>
public class BatchesNotReceivedModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public BatchesNotReceivedModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
    }

    private BatchesNotReceivedModel CreateSut() =>
        new(_session.Object, _batches.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    private static BatchListResult MakeBatch(int id, string status = BatchStatus.Submitted) => new() { ID = id };

    [Fact]
    public async Task OnGetAsync_LoadsBatchesAndPopulatesGridViewData()
    {
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[MakeBatch(1), MakeBatch(2)]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(2, sut.TotalCount);
    }

    [Fact]
    public async Task PagedEntries_DefaultSort_OrdersByIdAscending()
    {
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[MakeBatch(3), MakeBatch(1), MakeBatch(2)]);
        var sut = CreateSut();
        await sut.OnGetAsync();

        var ids = sut.PagedEntries.Select(b => b.ID).ToList();

        Assert.Equal([1, 2, 3], ids);
    }

    [Fact]
    public async Task PagedEntries_SortDesc_ReversesOrder()
    {
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[MakeBatch(1), MakeBatch(2), MakeBatch(3)]);
        var sut = CreateSut();
        sut.SortDesc = true;
        await sut.OnGetAsync();

        var ids = sut.PagedEntries.Select(b => b.ID).ToList();

        Assert.Equal([3, 2, 1], ids);
    }

    [Fact]
    public async Task PagedEntries_MoreThanOnePage_RespectsPageSize()
    {
        var many = Enumerable.Range(1, 15).Select(i => MakeBatch(i)).ToList();
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)many);
        var sut = CreateSut();
        await sut.OnGetAsync();

        Assert.Equal(10, sut.PagedEntries.Count);
    }

    [Fact]
    public async Task PagedEntries_PageTwo_ReturnsRemainingEntries()
    {
        var many = Enumerable.Range(1, 15).Select(i => MakeBatch(i)).ToList();
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)many);
        var sut = CreateSut();
        sut.PageNumber = 2;
        await sut.OnGetAsync();

        Assert.Equal(5, sut.PagedEntries.Count);
    }

    [Fact]
    public async Task OnPostReceiveAsync_SetsSessionBatchIdAndRedirects()
    {
        var sut = CreateSut();

        var result = await sut.OnPostReceiveAsync(42);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/ReceiveBatch", redirect.PageName);
        Assert.Equal(42, _session.Object.BatchID);
    }

    [Fact]
    public async Task OnPostGoAsync_NoQuickGoId_ReturnsPageWithError()
    {
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        var sut = CreateSut();
        sut.QuickGoId = null;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a submission number.", sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_ZeroQuickGoId_ReturnsPageWithError()
    {
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        var sut = CreateSut();
        sut.QuickGoId = 0;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a submission number.", sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_BatchNotFound_ReturnsPageWithError()
    {
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.QuickGoId = 99;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("could not be found or is not awaiting receipt", sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_BatchNotSubmitted_ReturnsPageWithError()
    {
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 99, Status = BatchStatus.Received });
        var sut = CreateSut();
        sut.QuickGoId = 99;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_ValidSubmittedBatch_SetsSessionAndRedirects()
    {
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 99, Status = BatchStatus.Submitted });
        var sut = CreateSut();
        sut.QuickGoId = 99;

        var result = await sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/ReceiveBatch", redirect.PageName);
        Assert.Equal(99, _session.Object.BatchID);
    }
}
