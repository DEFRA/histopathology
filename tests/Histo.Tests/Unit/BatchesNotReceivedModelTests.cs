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

/// <summary>Unit tests for <see cref="BatchesNotReceivedModel"/>.</summary>
public class BatchesNotReceivedModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public BatchesNotReceivedModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 1 }, new BatchListResult { ID = 2 }]);
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

    [Fact]
    public async Task OnGetAsync_LoadsBatchesNewestFirstAndSetsTitles()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal("Batches not received", sut.ViewData["Title"]);
        Assert.Equal("Batches not received", sut.ViewData["PageTitle"]);
        Assert.Equal(2, sut.TotalCount);
        Assert.Equal([2, 1], sut.GetPagedEntries().Select(b => b.ID).ToArray());
    }

    [Fact]
    public async Task OnPostReceiveAsync_StoresBatchInSessionAndRedirectsToReceiveBatch()
    {
        var sut = CreateSut();

        var result = await sut.OnPostReceiveAsync(2);

        Assert.Equal("/Batches/ReceiveBatch", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal(2, _session.Object.BatchID);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task OnPostGoAsync_NoValidSubmissionNumber_ReturnsPageWithPromptError(int? quickGoId)
    {
        var sut = CreateSut();
        sut.QuickGoId = quickGoId;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a submission number.", sut.GoError);
        Assert.Equal("Batches not received", sut.ViewData["Title"]);
        Assert.Equal("Batches not received", sut.ViewData["PageTitle"]);
        Assert.Equal(2, sut.TotalCount);
    }

    [Fact]
    public async Task OnPostGoAsync_BatchNotFound_ReturnsPageWithNotFoundError()
    {
        _batches.Setup(b => b.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.QuickGoId = 99;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Submission 99 could not be found or is not awaiting receipt.", sut.GoError);
        Assert.Null(_session.Object.BatchID);
    }

    [Fact]
    public async Task OnPostGoAsync_BatchAlreadyReceived_ReturnsPageWithNotAwaitingError()
    {
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 5, Status = BatchStatus.Received });
        var sut = CreateSut();
        sut.QuickGoId = 5;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("not awaiting receipt", sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_SubmittedBatch_StoresBatchInSessionAndRedirects()
    {
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 5, Status = BatchStatus.Submitted });
        var sut = CreateSut();
        sut.QuickGoId = 5;

        var result = await sut.OnPostGoAsync();

        Assert.Equal("/Batches/ReceiveBatch", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal(5, _session.Object.BatchID);
        Assert.Null(sut.GoError);
    }
}
