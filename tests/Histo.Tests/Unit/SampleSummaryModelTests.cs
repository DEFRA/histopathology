using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SampleSummaryModel"/> — the flat sample list reached from
/// <c>BatchDetails</c>'s "Samples" button (replaces legacy BatchSummary/BatchBlockSummary).
/// </summary>
public class SampleSummaryModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();

    public SampleSummaryModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.AnimalID);
        _session.SetupProperty(s => s.BatchSubmissionID);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _session.Setup(s => s.UserID).Returns(99);

        _batches.Setup(b => b.GetSubmittedAsCodeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(11, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LookupItem { Code = "4", Name = "Wet Tissue" },
                new LookupItem { Code = "5", Name = "Wax Block" }
            ]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
    }

    private SampleSummaryModel CreateSut() =>
        new(_session.Object, _submissions.Object, _batches.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = Mock.Of<ITempDataDictionary>(),
        };

    [Fact]
    public async Task OnGetAsync_NoBatchId_RedirectsToIndex()
    {
        var sut = CreateSut();
        sut.BatchId = null;

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_ValidBatch_LoadsPageAndSyncsSession()
    {
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 1 });
        var sut = CreateSut();
        sut.BatchId = 1;

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(1, _session.Object.BatchID);
    }

    [Fact]
    public async Task OnPostSelect_NoBatchId_RedirectsToIndex()
    {
        var sut = CreateSut();
        sut.BatchId = null;

        var result = await sut.OnPostSelect(5);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostSelect_WetTissue_RoutesToSubmissionDetails()
    {
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("4");
        var sut = CreateSut();
        sut.BatchId = 1;

        var result = await sut.OnPostSelect(5);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetails", redirect.PageName);
        Assert.Equal(5, _session.Object.AnimalID);
    }

    [Fact]
    public async Task OnPostSelect_NotWetTissue_RoutesToSubmissionDetailsBlock()
    {
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("5");
        var sut = CreateSut();
        sut.BatchId = 1;

        var result = await sut.OnPostSelect(5);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
    }

    [Theory]
    [InlineData(BatchStatus.Submitted, true)]
    [InlineData(BatchStatus.Rejected, true)]
    [InlineData(BatchStatus.Received, false)]
    [InlineData(BatchStatus.Completed, false)]
    public async Task CanModifySamples_ReflectsBatchStatus(string status, bool expected)
    {
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 1, Status = status });
        var sut = CreateSut();
        sut.BatchId = 1;

        await sut.OnGetAsync();

        Assert.Equal(expected, sut.CanModifySamples);
    }

    [Fact]
    public void IsViewMode_ReflectsSessionFlag()
    {
        _session.Setup(s => s.IsViewSubmissionMode).Returns(true);
        var sut = CreateSut();

        Assert.True(sut.IsViewMode);
    }
}
