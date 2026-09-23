using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SampleSummaryModel"/>'s <c>OnPostFinishAsync</c> handler —
/// the create-flow's "Finish" gate, equivalent to legacy's <c>btSubmit_Click</c>
/// (<c>BatchBlocks.aspx.vb</c> / <c>BatchSummary.aspx.vb</c> / <c>BatchBlockSummary.aspx.vb</c>),
/// which only proceeded once <c>dtBatch.Rows.Count &gt; 0</c>.
/// </summary>
public class SampleSummaryModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ILookupService> _lookups = new();

    public SampleSummaryModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.Setup(s => s.UserID).Returns(42);
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
    }

    private SampleSummaryModel CreateSut() =>
        new(_session.Object, _submissions.Object, _batches.Object, _blocks.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()),
            BatchId = 5,
        };

    [Fact]
    public async Task OnPostFinishAsync_NoSamples_ReturnsErrorAndDoesNotChangeStatus()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);

        var sut = CreateSut();
        var result = await sut.OnPostFinishAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(redirect.PageName);
        Assert.Equal("You must add at least one sample before finishing this submission.", sut.TempData["SampleSummary_Error"]);
        _batches.Verify(b => b.CompleteBlockAssignmentAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostFinishAsync_HasSamples_UpdatesStatusAndRedirectsToBatchesNotReceived()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "S1" }]);
        _batches.Setup(b => b.CompleteBlockAssignmentAsync(5, It.IsAny<bool>(), 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.OnPostFinishAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesNotReceived", redirect.PageName);
        _batches.Verify(b => b.CompleteBlockAssignmentAsync(5, It.IsAny<bool>(), 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostFinishAsync_WetTissue_DoesNotAdvanceStatusToInProgress()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "S1" }]);
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync("1");
        _lookups.Setup(l => l.GetLookupDataAsync(11, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Code = "1", Name = "Wet Tissue" }]);

        var sut = CreateSut();
        var result = await sut.OnPostFinishAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesNotReceived", redirect.PageName);
        _batches.Verify(b => b.CompleteBlockAssignmentAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
