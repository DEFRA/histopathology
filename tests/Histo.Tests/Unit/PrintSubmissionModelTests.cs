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

/// <summary>
/// Unit tests for <see cref="PrintSubmissionModel"/> — the Receive Submission
/// confirmation page (replaces legacy <c>FinalPrintBatch.aspx</c>).
/// </summary>
public class PrintSubmissionModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public PrintSubmissionModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
    }

    private PrintSubmissionModel CreateSut() =>
        new(_session.Object, _batches.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    private static Batch MakeBatch(string? comments = null, string? statusComments = null) => new()
    {
        ID = 1,
        Comments = comments,
        StatusComments = statusComments,
    };

    [Fact]
    public async Task OnGetAsync_NoBatchIdInSession_RedirectsToBatchesNotReceived()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesNotReceived", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_ZeroOrNegativeBatchId_RedirectsToBatchesNotReceived()
    {
        _session.Object.BatchID = 0;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesNotReceived", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_BatchNotFound_RedirectsToBatchesNotReceived()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesNotReceived", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_BatchFound_ReturnsPageAndSetsBatch()
    {
        _session.Object.BatchID = 1;
        var batch = MakeBatch();
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Same(batch, sut.Batch);
    }

    [Fact]
    public async Task OnGetAsync_NoCommentsOrStatusComments_HasNotesIsFalse()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.False(sut.HasNotes);
    }

    [Theory]
    [InlineData("Some comment", null)]
    [InlineData(null, "Some status comment")]
    [InlineData("Some comment", "Some status comment")]
    [InlineData("   ", "Some status comment")]
    public async Task OnGetAsync_CommentsOrStatusCommentsPresent_HasNotesIsTrue(string? comments, string? statusComments)
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeBatch(comments, statusComments));
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.True(sut.HasNotes);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    public async Task OnGetAsync_BlankCommentsAndStatusComments_HasNotesIsFalse(string? comments, string? statusComments)
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeBatch(comments, statusComments));
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.False(sut.HasNotes);
    }
}
