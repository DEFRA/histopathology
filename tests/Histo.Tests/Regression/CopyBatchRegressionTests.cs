using Histo.Administration.Interfaces;
using Histo.Administration.Models;
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

namespace Histo.Tests.Regression;

/// <summary>
/// Tier 2 gap closure: regression suite for Copy Submission (CopyBatch) and its confirmation
/// page (CopyBatchSummary), which previously only had atomic Unit/ coverage. Plain Moq —
/// neither page needs cross-page shared state beyond the TempData already modelled by
/// <see cref="TempDataDictionary"/> itself.
/// </summary>
public class CopyBatchRegressionTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<ILookupService> _lookups = new();

    public CopyBatchRegressionTests()
    {
        _session.SetupAllProperties();
        _lookups.Setup(l => l.GetLookupDataAsync(9, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private CopyBatchModel CreateCopyBatchSut()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return new(_session.Object, _batches.Object, _submissions.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    [Fact]
    public async Task OnGetAsync_SourceBatchNotFound_ShowsError()
    {
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateCopyBatchSut();

        var result = await sut.OnGetAsync(1);

        Assert.IsType<PageResult>(result);
        Assert.Equal("The submission to copy could not be found.", sut.Error);
    }

    [Fact]
    public async Task OnGetAsync_NonCassettedBatch_LoadsAnimalsFromGetAnimalsByBatch()
    {
        _batches.Setup(b => b.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 2 });
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[
            new Animal { ID = 10, SenderRef = "S2" }, new Animal { ID = 11, SenderRef = "S1" },
        ]);
        var sut = CreateCopyBatchSut();

        var result = await sut.OnGetAsync(2);

        Assert.IsType<PageResult>(result);
        Assert.False(sut.IsCassetted);
        Assert.Equal(2, sut.Animals.Count);
        Assert.Equal("S1", sut.Animals[0].SenderRef); // ordered by SenderRef
    }

    [Fact]
    public async Task OnPostAsync_NoRowsSelected_IsRejected()
    {
        _batches.Setup(b => b.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 3 });
        var sut = CreateCopyBatchSut();
        sut.SourceBatchId = 3;
        sut.Animals = [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1", NewSenderRef = "" }];

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select at least one sample to copy. Use Change to give a sample a new sender reference.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_FirstFinishClick_ShowsConfirmPanelWithoutStaging()
    {
        _batches.Setup(b => b.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 4 });
        var sut = CreateCopyBatchSut();
        sut.SourceBatchId = 4;
        sut.Animals = [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1", NewSenderRef = "S1-COPY" }];
        sut.Confirm = false;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.ShowConfirmPanel);
    }

    [Fact]
    public async Task OnPostAsync_Confirmed_StagesCopyAndRedirectsToBatchDetailsCreateMode()
    {
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, BatchType = BatchTypeConstants.NonTse });
        var sut = CreateCopyBatchSut();
        sut.SourceBatchId = 5;
        sut.Animals = [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1", NewSenderRef = "S1-COPY" }];
        sut.Confirm = true;

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchDetails", redirect.PageName);
        Assert.Equal("create", redirect.RouteValues?["mode"]);
        Assert.NotNull(redirect.RouteValues?["copyToken"]);
        Assert.Equal(BatchTypeConstants.NonTse, _session.Object.BatchType);
        Assert.NotNull(sut.TempData["CopyBatch_PendingCopy"]);
    }

    [Fact]
    public void OnPostPick_SavesAnimalsToTempDataAndRedirectsToAddSubmissionPicker()
    {
        var sut = CreateCopyBatchSut();
        sut.SourceBatchId = 6;
        sut.Animals = [new CopyBatchModel.AnimalRow { AnimalId = 42, SenderRef = "S1", NewSenderRef = "" }];

        var result = sut.OnPostPick(0);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/AddSubmission", redirect.PageName);
        Assert.Equal(42, redirect.RouteValues?["sourceAnimalId"]);
        Assert.NotNull(sut.TempData["CopyBatch_Animals"]);
    }

    // ── CopyBatchSummary ─────────────────────────────────────────────────────

    private CopyBatchSummaryModel CreateSummarySut()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return new(_session.Object, _batches.Object, _submissions.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
        };
    }

    [Fact]
    public async Task Summary_OnGetAsync_NoIdAndNoSessionBatch_RedirectsToIndex()
    {
        var sut = CreateSummarySut();

        var result = await sut.OnGetAsync(newBatchId: 0);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task Summary_OnGetAsync_FallsBackToSessionBatchIdWhenRouteValueIsZero()
    {
        _session.Object.BatchID = 77;
        _batches.Setup(b => b.GetByIdAsync(77, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 77 });
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(77, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 1 }]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(77, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1 }, new Animal { ID = 2 }]);
        var sut = CreateSummarySut();

        var result = await sut.OnGetAsync(newBatchId: 0);

        Assert.IsType<PageResult>(result);
        Assert.Equal(77, sut.NewBatchId);
        Assert.Equal(1, sut.SubmissionCount);
        Assert.Equal(2, sut.AnimalCount);
    }

    [Fact]
    public void Summary_OnPostViewAsync_SetsActiveBatchAndClearsViewMode_RedirectsToBatchDetails()
    {
        var sut = CreateSummarySut();

        var result = sut.OnPostViewAsync(88);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchDetails", redirect.PageName);
        Assert.Equal(88, _session.Object.BatchID);
        Assert.False(_session.Object.IsViewSubmissionMode);
    }

    [Fact]
    public void Summary_OnPostSamplesAsync_SetsSampleSummaryReturnPage_RedirectsToSampleSummary()
    {
        var sut = CreateSummarySut();

        var result = sut.OnPostSamplesAsync(89);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SampleSummary", redirect.PageName);
        Assert.Equal("/Batches/CopyBatchSummary", _session.Object.SampleSummaryReturnPage);
    }
}
