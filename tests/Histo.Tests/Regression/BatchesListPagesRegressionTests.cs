using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Tier 2 gap closure: regression suite for three single-page, no-cross-page-state Batches
/// list/confirmation pages that previously only had atomic Unit/ coverage — BatchesForEditing,
/// BatchesReceived (both Quick-Go variants) and PrintSubmission. Plain Moq throughout; none of
/// these need a stateful fake.
/// </summary>
public class BatchesListPagesRegressionTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public BatchesListPagesRegressionTests() => _session.SetupAllProperties();

    private static PageContext NewPageContext() => new()
    {
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
    };

    // ── BatchesForEditing ───────────────────────────────────────────────────

    [Fact]
    public async Task BatchesForEditing_OnGetAsync_LoadsAllBatches()
    {
        _batches.Setup(b => b.GetAllBatchesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 1 }, new BatchListResult { ID = 2 }]);
        var sut = new BatchesForEditingModel(_session.Object, _batches.Object) { PageContext = NewPageContext() };

        await sut.OnGetAsync();

        Assert.Equal(2, sut.TotalCount);
    }

    [Fact]
    public void BatchesForEditing_OnPostSelect_SetsSessionAndRedirectsToEditSubmissionStatus()
    {
        var sut = new BatchesForEditingModel(_session.Object, _batches.Object) { PageContext = NewPageContext() };

        var result = sut.OnPostSelect(42);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/EditSubmissionStatus", redirect.PageName);
        Assert.Equal(42, _session.Object.BatchID);
    }

    [Fact]
    public async Task BatchesForEditing_QuickGo_BlankId_IsRejected()
    {
        _batches.Setup(b => b.GetAllBatchesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        var sut = new BatchesForEditingModel(_session.Object, _batches.Object) { PageContext = NewPageContext() };
        sut.QuickGoId = null;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a submission number.", sut.GoError);
    }

    [Fact]
    public async Task BatchesForEditing_QuickGo_UnknownId_IsRejected()
    {
        _batches.Setup(b => b.GetAllBatchesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = new BatchesForEditingModel(_session.Object, _batches.Object) { PageContext = NewPageContext() };
        sut.QuickGoId = 999;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Submission 999 could not be found.", sut.GoError);
    }

    [Fact]
    public async Task BatchesForEditing_QuickGo_ValidId_RedirectsToEditSubmissionStatus()
    {
        _batches.Setup(b => b.GetAllBatchesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        var sut = new BatchesForEditingModel(_session.Object, _batches.Object) { PageContext = NewPageContext() };
        sut.QuickGoId = 5;

        var result = await sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/EditSubmissionStatus", redirect.PageName);
        Assert.Equal(5, _session.Object.BatchID);
    }

    // ── BatchesReceived ──────────────────────────────────────────────────────

    [Fact]
    public async Task BatchesReceived_OnGetAsync_LoadsReceivedBatches()
    {
        _batches.Setup(b => b.GetReceivedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 1 }]);
        var sut = new BatchesReceivedModel(_session.Object, _batches.Object) { PageContext = NewPageContext() };

        await sut.OnGetAsync();

        Assert.Equal(1, sut.TotalCount);
    }

    [Fact]
    public void BatchesReceived_OnPostSelect_RedirectsToBatchBlocks()
    {
        var sut = new BatchesReceivedModel(_session.Object, _batches.Object) { PageContext = NewPageContext() };

        var result = sut.OnPostSelect(7);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchBlocks", redirect.PageName);
        Assert.Equal(7, _session.Object.BatchID);
    }

    [Theory]
    [InlineData(BatchStatus.Completed)]
    [InlineData(BatchStatus.Rejected)]
    public async Task BatchesReceived_QuickGo_WrongStatus_IsRejected(string status)
    {
        _batches.Setup(b => b.GetReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 3, Status = status });
        var sut = new BatchesReceivedModel(_session.Object, _batches.Object) { PageContext = NewPageContext() };
        sut.QuickGoId = 3;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Submission 3 could not be found or does not have the required status.", sut.GoError);
    }

    [Fact]
    public async Task BatchesReceived_QuickGo_InProgressStatus_RedirectsToBatchBlocks()
    {
        _batches.Setup(b => b.GetReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 4, Status = BatchStatus.InProgress });
        var sut = new BatchesReceivedModel(_session.Object, _batches.Object) { PageContext = NewPageContext() };
        sut.QuickGoId = 4;

        var result = await sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchBlocks", redirect.PageName);
    }

    // ── PrintSubmission ──────────────────────────────────────────────────────

    [Fact]
    public async Task PrintSubmission_NoBatchInSession_RedirectsToBatchesNotReceived()
    {
        var blocks = new Mock<IBlockService>();
        var submissions = new Mock<ISubmissionService>();
        var tests = new Mock<IBlockTestService>();
        _session.Object.BatchID = null;
        var sut = new PrintSubmissionModel(_session.Object, _batches.Object, blocks.Object, submissions.Object, tests.Object) { PageContext = NewPageContext() };

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesNotReceived", redirect.PageName);
    }

    [Fact]
    public async Task PrintSubmission_ValidBatch_LoadsBatchAndHonoursReturnPage()
    {
        var blocks = new Mock<IBlockService>();
        var submissions = new Mock<ISubmissionService>();
        var tests = new Mock<IBlockTestService>();
        blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Histo.Histology.Models.Block>)[]);
        submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        tests.Setup(t => t.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Histo.Histology.Models.BlockTest>)[]);
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 10 });
        _session.Object.BatchID = 10;
        _session.Object.ReturnPage = "/Batches/BatchesReceived";
        var sut = new PrintSubmissionModel(_session.Object, _batches.Object, blocks.Object, submissions.Object, tests.Object) { PageContext = NewPageContext() };

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(sut.Batch);
        Assert.Equal("/Batches/BatchesReceived", sut.ContinuePage);
    }
}
