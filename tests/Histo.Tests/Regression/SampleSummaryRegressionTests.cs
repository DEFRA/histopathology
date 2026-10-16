using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Tier 2 gap closure: regression suite for Sample summary (SampleSummary) — the flat
/// per-animal sample list behind the Create/Edit/View Submission journeys, which previously
/// only had atomic Unit/ coverage. Plain Moq; the merge/tissue-resolution logic is exercised
/// through the real (non-faked) collaborator return values rather than a stateful fake, since
/// nothing here needs save-then-reload round-tripping.
/// </summary>
public class SampleSummaryRegressionTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ILookupService> _lookups = new();

    public SampleSummaryRegressionTests()
    {
        _session.SetupAllProperties();
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _lookups.Setup(l => l.GetLookupDataAsync(9, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(11, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "WT", Name = "Wet Tissue" },
            new LookupItem { Code = "WB", Name = "Wax Block" },
        ]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
    }

    private SampleSummaryModel CreateSut(int batchId)
    {
        _batches.Setup(b => b.GetByIdAsync(batchId, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = batchId, Status = BatchStatus.Submitted, UserAreaCode = 1 });
        return new(_session.Object, _submissions.Object, _batches.Object, _blocks.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            BatchId = batchId,
        };
    }

    [Fact]
    public async Task OnGetAsync_NoBatchId_RedirectsToIndex()
    {
        var sut = new SampleSummaryModel(_session.Object, _submissions.Object, _batches.Object, _blocks.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
        };

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_MergesBlockAnimalsAndPlainAnimals_ByIdUnion()
    {
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[
            new Animal { ID = 1, SenderRef = "S1", HistologyRef = "H1" },
        ]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[
            new Animal { ID = 1, SenderRef = "S1-STALE" }, // same ID as a block animal — block-animal row wins
            new Animal { ID = 2, SenderRef = "S2" },       // not yet block-assigned — must still appear
        ]);
        var sut = CreateSut(1);

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(2, sut.Animals.Count);
        Assert.Equal("S1", sut.Animals.Single(a => a.ID == 1).SenderRef); // block-animal row preserved, not overwritten
    }

    [Fact]
    public async Task OnGetAsync_NoSubmissionsYet_CreatesDefaultBatchSubmission()
    {
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(42);
        var sut = CreateSut(1);

        await sut.OnGetAsync();

        Assert.Equal(42, _session.Object.BatchSubmissionID);
    }

    [Fact]
    public async Task OnPostSelect_WetTissue_RoutesToSubmissionDetails()
    {
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("WT");
        var sut = CreateSut(1);

        var result = await sut.OnPostSelect(5);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetails", redirect.PageName);
        Assert.Equal(5, _session.Object.AnimalID);
    }

    [Fact]
    public async Task OnPostSelect_NonWetTissue_RoutesToSubmissionDetailsBlock()
    {
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("WB");
        var sut = CreateSut(1);

        var result = await sut.OnPostSelect(6);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
    }

    [Fact]
    public async Task OnPostDeleteAsync_ServiceReportsFailure_SetsTempDataError()
    {
        _submissions.Setup(s => s.DeleteAnimalAsync(7, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut(1);

        var result = await sut.OnPostDeleteAsync(7);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Could not delete this sample. It may still have blocks or tissues recorded against it.", sut.TempData["SampleSummary_Error"]);
    }

    [Fact]
    public void CanModifySamples_IsFalseWhenBatchReceived()
    {
        var batch = new Batch { ID = 1, Status = BatchStatus.Received };
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        var sut = new SampleSummaryModel(_session.Object, _submissions.Object, _batches.Object, _blocks.Object, _lookups.Object);
        typeof(SampleSummaryModel).GetProperty(nameof(SampleSummaryModel.Batch))!.SetValue(sut, batch);

        Assert.False(sut.CanModifySamples);
    }
}
