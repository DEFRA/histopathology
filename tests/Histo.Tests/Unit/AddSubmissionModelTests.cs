using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
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
/// Verifies <see cref="AddSubmissionModel.IsAssignTissueMode"/> — reached via the "Assign Tissues
/// to Blocks" journey (<c>Batches/BatchBlocks</c>'s "Add sample" button), the Sender Ref field must
/// become a closed choice from the batch's own unblocked samples rather than free text, and
/// submitting must select that existing sample (no new Animal created) and jump straight to block
/// assignment. The Create/Edit Submission journey (reached without that context) must be entirely
/// unaffected — it still creates a brand-new Animal from free-typed text.
/// </summary>
public class AddSubmissionModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockService> _blocks = new();

    public AddSubmissionModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.BatchSubmissionID);
        _session.SetupProperty(s => s.SampleDetailReturnPage);
        _session.Setup(s => s.UserID).Returns(7);
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
    }

    private AddSubmissionModel CreateSut(string? returnPage = null) =>
        new(_session.Object, _submissions.Object, _batches.Object, _lookups.Object, _blocks.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            BatchId = 5,
            ReturnPage = returnPage,
        };

    [Fact]
    public void IsAssignTissueMode_WhenReturnPageIsBatchBlocks_IsTrue()
    {
        var sut = CreateSut(returnPage: "/Batches/BatchBlocks?batchId=5");
        Assert.True(sut.IsAssignTissueMode);
    }

    [Fact]
    public void IsAssignTissueMode_ForCreateEditSubmission_IsFalse()
    {
        var sut = CreateSut(returnPage: null);
        Assert.False(sut.IsAssignTissueMode);
    }

    [Fact]
    public void IsAssignTissueMode_ForSampleSummaryOrigin_IsAlwaysFalse()
    {
        // Regression guard: SampleSummary's own "Add sample" must ALWAYS be free text, never a
        // dropdown, regardless of how many existing samples the batch has — only BatchBlocks'
        // "Add sample" (reached via BatchesReceived) is a picker. A prior iteration added a
        // lenient SampleSummary-origin picker mode; it was reverted per explicit user correction.
        var sut = CreateSut(returnPage: "/Submissions/SampleSummary?batchId=5");
        Assert.False(sut.IsAssignTissueMode);
    }

    [Fact]
    public async Task OnGetAsync_AssignTissueMode_PopulatesOnlyUnblockedSamples()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[
                new Animal { ID = 1, SenderRef = "S1" },
                new Animal { ID = 2, SenderRef = "S2" },
            ]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 10, AnimalID = 1, BlockRef = "01" }]);
        var sut = CreateSut(returnPage: "/Batches/BatchBlocks?batchId=5");

        await sut.OnGetAsync(null, null);

        Assert.Single(sut.AvailableAnimals);
        Assert.Equal("S2", sut.AvailableAnimals[0].SenderRef);
    }

    [Fact]
    public async Task OnGetAsync_CreateEditSubmission_DoesNotLoadAvailableAnimals()
    {
        var sut = CreateSut(returnPage: null);

        await sut.OnGetAsync(null, null);

        Assert.Empty(sut.AvailableAnimals);
        _submissions.Verify(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_AssignTissueMode_SelectsExistingSample_DoesNotCreateNewAnimal()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        var sut = CreateSut(returnPage: "/Batches/BatchBlocks?batchId=5");
        sut.SenderRef = "S2";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
        Assert.Equal(2, redirect.RouteValues!["animalId"]);
        _submissions.Verify(s => s.AddAnimalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_AssignTissueMode_RedirectsToSubmissionDetailsBlock_EvenIfSubmittedAsCodeLooksLikeWetTissue()
    {
        // Regression: BatchBlocks' "Add sample" must always continue into the block-assignment
        // detail page, decided purely by ORIGIN — it must never re-derive Wet-Tissue-ness from the
        // "Submitted As" lookup for this branch (that previously misrouted BatchBlocks-origin
        // samples to SubmissionDetails).
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync("WT");
        _lookups.Setup(l => l.GetLookupDataAsync(11, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { Code = "WT", Name = "Wet Tissue" }]);
        var sut = CreateSut(returnPage: "/Batches/BatchBlocks?batchId=5");
        sut.SenderRef = "S2";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_AssignTissueMode_SetsSampleDetailReturnPageToBatchBlocks()
    {
        // SubmissionDetailsBlock's own Back link reads this — without it, Back would fall through
        // to the wrong default (SampleSummary) instead of returning to BatchBlocks.
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        var sut = CreateSut(returnPage: "/Batches/BatchBlocks?batchId=5");
        sut.SenderRef = "S2";

        await sut.OnPostAsync();

        Assert.Equal("/Batches/BatchBlocks?batchId=5", sut.Session.SampleDetailReturnPage);
    }

    [Fact]
    public async Task OnPostAsync_AssignTissueMode_SampleNotInAvailableList_SetsModelError()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        var sut = CreateSut(returnPage: "/Batches/BatchBlocks?batchId=5");
        sut.SenderRef = "NoSuchSample";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select a sample from the list.", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_CreateEditSubmission_StillCreatesNewAnimal()
    {
        _session.Object.BatchSubmissionID = 99;
        _submissions.Setup(s => s.AddAnimalAsync(99, "NewRef", 7, (string?)null, false, It.IsAny<CancellationToken>())).ReturnsAsync(123);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        _submissions.Setup(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(200);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SenderRef = "NewRef";

        var result = await sut.OnPostAsync();

        _submissions.Verify(s => s.AddAnimalAsync(99, "NewRef", 7, (string?)null, false, It.IsAny<CancellationToken>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
    }
}
