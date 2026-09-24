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
                HttpContext = new DefaultHttpContext(),
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
        Assert.Equal("/Batches/BatchDetails", redirect.PageName);
        Assert.Equal(42, _session.Object.BatchID);
        Assert.False(_session.Object.IsViewSubmissionMode);
    }

    [Fact]
    public void OnPostGoAsync_QuickGoIdSet_SetsSessionAndRedirects()
    {
        var sut = CreateSut();
        sut.QuickGoId = 99;

        var result = sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchDetails", redirect.PageName);
        Assert.Equal(99, _session.Object.BatchID);
    }

    [Fact]
    public void OnPostGoAsync_ZeroQuickGoId_ShouldRejectInvalidInput()
    {
        var sut = CreateSut();
        sut.QuickGoId = 0;

        var result = sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(_session.Object.BatchID);
    }

    [Fact]
    public void OnPostGoAsync_NoQuickGoId_StillRedirectsWithoutSettingSession_BUG()
    {
        // BUG: unlike every other "Go" handler in this module (BatchesForEditing,
        // BatchesForDispatch, BatchesNotReceived), this one has no validation at all —
        // when QuickGoId is null it redirects to BatchDetails anyway, without setting
        // Session.BatchID, silently loading whatever batch (if any) was already in session.
        var sut = CreateSut();
        sut.QuickGoId = null;

        var result = sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchDetails", redirect.PageName);
        Assert.Null(_session.Object.BatchID);
    }
}
