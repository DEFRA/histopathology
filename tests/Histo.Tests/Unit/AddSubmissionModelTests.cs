using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Verifies <see cref="AddSubmissionModel.IsAssignTissueMode"/> — reached via the "Assign Tissues
/// to Blocks" journey (<c>Batches/BatchBlocks</c>'s "Add sample" button), the Sender Ref field must
/// become a closed choice from every sample in the batch (not just unblocked ones — a sample often
/// needs several blocks added one at a time by revisiting this same picker) rather than free text,
/// and submitting must select that existing sample (no new Animal created) and jump straight to
/// block assignment. The Create/Edit Submission journey (reached without that context) must be
/// entirely unaffected — it still creates a brand-new Animal from free-typed text.
/// </summary>
public class AddSubmissionModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBlockTestService> _blockTests = new();
    private readonly Mock<IHistologyRefService> _histologyRefs = new();

    public AddSubmissionModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.BatchSubmissionID);
        _session.SetupProperty(s => s.SampleDetailReturnPage);
        _session.Setup(s => s.UserID).Returns(7);
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[]);
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
    }

    private AddSubmissionModel CreateSut(string? returnPage = null) =>
        new(_session.Object, _submissions.Object, _batches.Object, _lookups.Object, _blocks.Object, _blockTests.Object, _histologyRefs.Object)
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
    public void IsAssignTissueMode_StaleSessionBreadcrumbFromEarlierBatchBlocksVisit_IsIgnored()
    {
        // Regression: Session.SampleDetailReturnPage is set by OTHER pages (BatchBlocks,
        // SampleSummary, this page's own POST handler) purely to drive THEIR OWN later back-link —
        // it must never leak into this page's own mode detection. Reported bug: a user who visited
        // the Assign Tissue journey earlier in the same browser session, then started a completely
        // unrelated Create Submission journey, saw the dropdown instead of the free-text field
        // because this stale session value was still "/Batches/BatchBlocks?...".
        _session.Setup(s => s.SampleDetailReturnPage).Returns("/Batches/BatchBlocks?batchId=999");
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
    public async Task OnGetAsync_AssignTissueMode_PopulatesEverySampleInBatch()
    {
        // Regression: a sample commonly needs several blocks added one at a time, so this picker
        // must keep offering an already-blocked sample back — filtering it out once it had its
        // first block made the dropdown appear to "stop populating" on a later visit.
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[
                new Animal { ID = 1, SenderRef = "S1" },
                new Animal { ID = 2, SenderRef = "S2" },
            ]);
        var sut = CreateSut(returnPage: "/Batches/BatchBlocks?batchId=5");

        await sut.OnGetAsync(null, null);

        Assert.Equal(2, sut.AvailableAnimals.Count);
        Assert.Contains(sut.AvailableAnimals, a => a.SenderRef == "S1");
        Assert.Contains(sut.AvailableAnimals, a => a.SenderRef == "S2");
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
        var sut = CreateSut(returnPage: "/Batches/BatchBlocks?batchId=5");
        sut.SenderRef = "S2";

        await sut.OnPostAsync();

        Assert.Equal("/Batches/BatchBlocks?batchId=5", sut.Session.SampleDetailReturnPage);
    }

    [Fact]
    public async Task OnPostAsync_AssignTissueMode_SampleNotInAvailableList_SetsModelError()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        var sut = CreateSut(returnPage: "/Batches/BatchBlocks?batchId=5");
        sut.SenderRef = "NoSuchSample";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select a sample from the list.", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_WhenBatchIdIsMissing_RedirectsToIndex()
    {
        var sut = CreateSut();
        sut.BatchId = null;

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_WhenSenderRefAndMouseRangeAreBothProvided_RejectsInput()
    {
        _session.Setup(s => s.UserArea).Returns("Histopath");
        var sut = CreateSut();
        sut.SourceAnimalId = 1;
        sut.SenderRef = "S1";
        sut.MouseNumberFrom = "MC000001";
        sut.MouseNumberTo = "MC000002";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter either the Sender ref or the mouse number ranges.", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_WhenMouseRangeInputIsIncomplete_SetsMouseRangeValidationError()
    {
        _session.Setup(s => s.UserArea).Returns("Histopath");
        var sut = CreateSut();
        sut.SourceAnimalId = 1;
        sut.MouseNumberFrom = "MC000001";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.MouseRangeHasError);
        Assert.Equal("Enter both the from and to mouse numbers.", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_WhenCopyBatchRowIndexIsOutOfRange_AppendsNewAnimalEntry()
    {
        var sut = CreateSut(returnPage: "/Batches/CopyBatch");
        sut.SourceAnimalId = 42;
        sut.RowIndex = 99;
        sut.SenderRef = "NewRef";
        sut.TempData["CopyBatch_Animals"] = JsonSerializer.Serialize(new List<CopyBatchModel.AnimalRow>
        {
            new() { AnimalId = 7, SubmissionId = 9, SenderRef = "OldRef", NewSenderRef = "OldRef" },
        });

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/CopyBatch", redirect.PageName);

        var animals = JsonSerializer.Deserialize<List<CopyBatchModel.AnimalRow>>(sut.TempData["CopyBatch_Animals"] as string ?? "[]");
        Assert.NotNull(animals);
        Assert.Equal(2, animals!.Count);
        Assert.Equal("NewRef", animals[1].NewSenderRef);
        Assert.Equal(42, animals[1].AnimalId);
    }

    [Fact]
    public async Task OnPostAsync_WhenWetTissueSourceSampleIsCopied_CopiesSourceTissuesAndReturnsToSampleSummary()
    {
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _session.Setup(s => s.UserID).Returns(7);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, IsPreCassetted = false });
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync("WT");
        _lookups.Setup(l => l.GetLookupDataAsync(11, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { Code = "WT", Name = "Wet Tissue" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[
                new() { ID = 99, BatchID = 5, AnimalID = 10, Order = 1 },
                new() { ID = 100, BatchID = 5, AnimalID = 42, Order = 2 },
            ]);
        _submissions.Setup(s => s.AddAnimalAsync(99, "S1", 7, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(101);
        _submissions.Setup(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(200);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(5, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 1, TissueCode = "LIVER" }, new Tissue { ID = 2, TissueCode = "LUNG" }]);
        _submissions.Setup(s => s.CopyTissueAsync(It.IsAny<Tissue>(), 200, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();
        sut.BatchId = 5;
        sut.SourceAnimalId = 42;
        sut.SenderRef = "S1";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SampleSummary", redirect.PageName);
        Assert.Equal(5, redirect.RouteValues!["batchId"]);
        _submissions.Verify(s => s.CopyTissueAsync(It.IsAny<Tissue>(), 200, 7, It.IsAny<CancellationToken>()), Times.Exactly(2));
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

    [Fact]
    public async Task OnPostAsync_CopySubmissionReturnFlow_StagesTheUpdatedSenderRefWithoutCreatingSourceRecords()
    {
        _session.Object.BatchSubmissionID = 99;
        var sut = CreateSut(returnPage: "/Batches/CopyBatch");
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 42;
        sut.SenderRef = "NewRef";
        sut.RowIndex = 1;
        sut.TempData["CopyBatch_Animals"] = JsonSerializer.Serialize(new List<CopyBatchModel.AnimalRow>
        {
            new() { AnimalId = 10, SenderRef = "OldA", NewSenderRef = string.Empty },
            new() { AnimalId = 11, SenderRef = "OldB", NewSenderRef = string.Empty },
        });

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/CopyBatch", redirect.PageName);
        Assert.Equal(5, redirect.RouteValues!["sourceBatchId"]);

        _submissions.Verify(s => s.AddAnimalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _submissions.Verify(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

        var animals = JsonSerializer.Deserialize<List<CopyBatchModel.AnimalRow>>(sut.TempData["CopyBatch_Animals"] as string ?? "[]");
        Assert.Equal("NewRef", animals![1].NewSenderRef);
    }

    [Fact]
    public async Task OnPostAsync_CopySubmissionReturnFlow_ReturnPageCarriesQueryString_StillStagesAndReturns()
    {
        // Regression: CopyBatchModel.OnPostPick sends "/Batches/CopyBatch?sourceBatchId=N", so the
        // old exact-string match never fired — the Change flow fell through, wrote samples into the
        // SOURCE batch and dumped the user on Sample Summary with no way back to Finish.
        _session.Object.BatchSubmissionID = 99;
        var sut = CreateSut(returnPage: "/Batches/CopyBatch?sourceBatchId=5");
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 42;
        sut.SenderRef = "NewRef";
        sut.RowIndex = 0;
        sut.TempData["CopyBatch_Animals"] = JsonSerializer.Serialize(new List<CopyBatchModel.AnimalRow>
        {
            new() { AnimalId = 10, SenderRef = "OldA", NewSenderRef = string.Empty },
        });

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/CopyBatch", redirect.PageName);
        _submissions.Verify(s => s.AddAnimalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);

        var animals = JsonSerializer.Deserialize<List<CopyBatchModel.AnimalRow>>(sut.TempData["CopyBatch_Animals"] as string ?? "[]");
        Assert.Equal("NewRef", animals![0].NewSenderRef);
    }

    [Fact]
    public async Task OnPostAsync_CopySubmissionReturnFlow_MouseRange_StagesOneRowPerMouseNumber()
    {
        _session.Object.BatchSubmissionID = 99;
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetExistingSenderRefsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)[]);
        var sut = CreateSut(returnPage: "/Batches/CopyBatch?sourceBatchId=5");
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 42;
        sut.RowIndex = 0;
        sut.MouseNumberFrom = "MC000001";
        sut.MouseNumberTo = "MC000003";
        sut.TempData["CopyBatch_Animals"] = JsonSerializer.Serialize(new List<CopyBatchModel.AnimalRow>
        {
            new() { AnimalId = 10, SenderRef = "OldA", NewSenderRef = string.Empty },
            new() { AnimalId = 11, SenderRef = "OldB", NewSenderRef = string.Empty },
        });

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/CopyBatch", redirect.PageName);
        _submissions.Verify(s => s.CreateMouseRangeAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

        var animals = JsonSerializer.Deserialize<List<CopyBatchModel.AnimalRow>>(sut.TempData["CopyBatch_Animals"] as string ?? "[]");
        Assert.Equal(4, animals!.Count);
        Assert.Equal(["MC000001", "MC000002", "MC000003"], animals.Take(3).Select(a => a.NewSenderRef));
        Assert.All(animals.Take(3), a => Assert.Equal(10, a.AnimalId));
        Assert.Equal("OldB", animals[3].SenderRef);
    }

    [Fact]
    public async Task OnPostAsync_CopySample_CopiesSourceBlocksWithTestSelectionsAndDrawsAHistologyRef()
    {
        _session.Object.BatchSubmissionID = 99;
        _submissions.Setup(s => s.AddAnimalAsync(99, "S1-COPY", 7, (string?)null, false, It.IsAny<CancellationToken>())).ReturnsAsync(123);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        _submissions.Setup(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(200);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[
                new Animal { ID = 42, SenderRef = "S1", HistologyRef = "26/60001", HistoRefSet = true },
                new Animal { ID = 123, SenderRef = "S1-COPY" }]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 70, BatchID = 5, AnimalID = 42, BlockRef = "01" }]);
        _blocks.Setup(b => b.CopyBlockAsync(It.IsAny<Block>(), 5, 123, It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(300);
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[new BlockTest { BlockID = 70, TestType = BlockTestType.Histology, Code = HistologyCode.HAndE }]);
        _histologyRefs.Setup(h => h.GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync("26/60002");

        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 42;
        sut.SenderRef = "S1-COPY";

        await sut.OnPostAsync();

        _blocks.Verify(b => b.CopyBlockAsync(It.Is<Block>(x => x.ID == 70), 5, 123,
            It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _blockTests.Verify(t => t.SaveTestSelectionsAsync(5, 300,
            It.Is<IReadOnlyList<string>>(c => c.Single() == HistologyCode.HAndE),
            It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.UpdateAnimalAsync(
            It.Is<Animal>(a => a.ID == 123 && a.HistologyRef == "26/60002" && a.HistoRefSet),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_CopySample_BlockInsertFails_ShowsAnErrorInsteadOfRedirecting()
    {
        _session.Object.BatchSubmissionID = 99;
        _submissions.Setup(s => s.AddAnimalAsync(99, "S1-COPY", 7, (string?)null, false, It.IsAny<CancellationToken>())).ReturnsAsync(123);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        _submissions.Setup(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(200);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[
                new Animal { ID = 42, SenderRef = "S1", HistologyRef = "26/60001", HistoRefSet = true },
                new Animal { ID = 123, SenderRef = "S1-COPY" }]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 70, BatchID = 5, AnimalID = 42, BlockRef = "01" }]);
        _blocks.Setup(b => b.CopyBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 42;
        sut.SenderRef = "S1-COPY";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Could not copy block '01'", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_CopySampleRange_CopiesBlocksAndDrawsARefForEveryNewSample()
    {
        _session.Object.BatchSubmissionID = 99;
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        _submissions.Setup(s => s.GetExistingSenderRefsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)[]);
        _submissions.Setup(s => s.CreateMouseRangeAsync(5, 42, "MC000001", "MC000002", 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[
                new Animal { ID = 42, SenderRef = "SRC", HistologyRef = "26/60001", HistoRefSet = true },
                new Animal { ID = 51, SenderRef = "MC000001" },
                new Animal { ID = 52, SenderRef = "MC000002" }]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 70, BatchID = 5, AnimalID = 42, BlockRef = "01" }]);
        _blocks.Setup(b => b.CopyBlockAsync(It.IsAny<Block>(), 5, It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(300);
        _histologyRefs.SetupSequence(h => h.GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync("26/60002")
            .ReturnsAsync("26/60003");

        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 42;
        sut.MouseNumberFrom = "MC000001";
        sut.MouseNumberTo = "MC000002";

        await sut.OnPostAsync();

        _blocks.Verify(b => b.CopyBlockAsync(It.IsAny<Block>(), 5, 51, It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _blocks.Verify(b => b.CopyBlockAsync(It.IsAny<Block>(), 5, 52, It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        // One ref identifies one sample, so each target must get its own draw.
        _histologyRefs.Verify(h => h.GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _submissions.Verify(s => s.UpdateAnimalAsync(It.Is<Animal>(a => a.ID == 51 && a.HistologyRef == "26/60002"), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.Is<Animal>(a => a.ID == 52 && a.HistologyRef == "26/60003"), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_PreCassetted_SenderHasPreBookedBlock_ReusesPreBookedAnimal()
    {
        _session.Object.BatchSubmissionID = 99;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, IsPreCassetted = true });
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        _submissions.Setup(s => s.GetAnimalBySenderAsync("PreBookedRef", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 77, SenderRef = "PreBookedRef" }]);
        _blocks.Setup(b => b.GetPreBookedByAnimalAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, AnimalID = 77, BlockRef = "01" }]);
        _submissions.Setup(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(200);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SenderRef = "PreBookedRef";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
        Assert.Equal(77, redirect.RouteValues!["animalId"]);
        _submissions.Verify(s => s.AddAnimalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_PreCassetted_SenderHasNoPreBookedBlock_SetsModelErrorAndDoesNotCreateAnimal()
    {
        _session.Object.BatchSubmissionID = 99;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, IsPreCassetted = true });
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        _submissions.Setup(s => s.GetAnimalBySenderAsync("NoBookingRef", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[]);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SenderRef = "NoBookingRef";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("This sender reference has no pre-booked block. Book a block reference for this sender before adding the sample.", sut.ModelError);
        _submissions.Verify(s => s.AddAnimalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_SenderRefError_DoesNotSetMouseRangeHasError()
    {
        var sut = CreateSut(returnPage: null);

        await sut.OnPostAsync();

        Assert.False(sut.MouseRangeHasError);
    }

    [Fact]
    public async Task OnPostAsync_MouseRangeError_SetsMouseRangeHasError()
    {
        _session.Object.BatchSubmissionID = 99;
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 1;
        sut.MouseNumberFrom = "BAD";
        sut.MouseNumberTo = "MC000002";

        await sut.OnPostAsync();

        Assert.True(sut.MouseRangeHasError);
    }

    [Fact]
    public void ShowMouseRange_ForHistopathUserAreaWithSourceAnimal_IsTrue()
    {
        _session.Setup(s => s.UserArea).Returns("Histopath");
        var sut = CreateSut();
        sut.SourceAnimalId = 1;
        Assert.True(sut.ShowMouseRange);
    }

    [Fact]
    public void ShowMouseRange_ForOtherUserArea_IsFalse()
    {
        _session.Setup(s => s.UserArea).Returns("External Customer");
        var sut = CreateSut();
        sut.SourceAnimalId = 1;
        Assert.False(sut.ShowMouseRange);
    }

    [Fact]
    public void ShowMouseRange_ForHistopathUserAreaWithoutSourceAnimal_IsFalse()
    {
        // Legacy only shows this section on the "copying a submission" journey — the plain
        // Create/Edit Submission journey (no Copy sample context) never qualifies.
        _session.Setup(s => s.UserArea).Returns("Histopath");
        Assert.False(CreateSut().ShowMouseRange);
    }

    [Fact]
    public async Task OnPostAsync_NeitherSenderRefNorMouseRange_SetsModelError()
    {
        var sut = CreateSut(returnPage: null);

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter the sender reference.", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_BothSenderRefAndMouseRange_SetsModelError()
    {
        _session.Setup(s => s.UserArea).Returns("Histopath");
        var sut = CreateSut(returnPage: null);
        sut.SourceAnimalId = 1;
        sut.SenderRef = "S1";
        sut.MouseNumberFrom = "MC000001";
        sut.MouseNumberTo = "MC000002";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter either the Sender ref or the mouse number ranges.", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_MouseRange_InvalidFormat_SetsModelError()
    {
        _session.Object.BatchSubmissionID = 99;
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 1;
        sut.MouseNumberFrom = "BAD";
        sut.MouseNumberTo = "MC000002";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The mouse number format is MC followed by 6 digits, i.e. MC000105.", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_MouseRange_FromNotLessThanTo_SetsModelError()
    {
        _session.Object.BatchSubmissionID = 99;
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 1;
        sut.MouseNumberFrom = "MC000005";
        sut.MouseNumberTo = "MC000002";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The from number must be less than the to number.", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_MouseRange_DuplicateInBatch_SetsModelErrorAndDoesNotCreateAnimal()
    {
        _session.Object.BatchSubmissionID = 99;
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        _submissions.Setup(s => s.GetExistingSenderRefsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)["MC000002"]);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 1;
        sut.MouseNumberFrom = "MC000001";
        sut.MouseNumberTo = "MC000003";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Mouse number MC000002 already exists. Alter the range and try again.", sut.ModelError);
        _submissions.Verify(s => s.AddAnimalAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_MouseRange_UsesTransactionalRangeCreate_WhenValid()
    {
        _session.Object.BatchSubmissionID = 99;
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        _submissions.Setup(s => s.GetExistingSenderRefsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)[]);
        _submissions.Setup(s => s.CreateMouseRangeAsync(5, 1, "MC000001", "MC000002", 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 1;
        sut.MouseNumberFrom = "MC000001";
        sut.MouseNumberTo = "MC000002";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SampleSummary", redirect.PageName);
        _submissions.Verify(s => s.CreateMouseRangeAsync(5, 1, "MC000001", "MC000002", 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_MouseRange_ExceedsMaximumRangeSize_SetsModelError()
    {
        _session.Object.BatchSubmissionID = 99;
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 99, BatchID = 5, Order = 1 }]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 1;
        sut.MouseNumberFrom = "MC000001";
        sut.MouseNumberTo = "MC001001";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The mouse number range cannot exceed 1000 entries. Use a smaller range or create a bulk job.", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_MouseRange_DelegatesToTransactionalRangeService()
    {
        _session.Object.BatchSubmissionID = 99;
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 50, BatchID = 5, AnimalID = 1, Order = 1 }]);
        _submissions.Setup(s => s.GetExistingSenderRefsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)[]);
        _submissions.Setup(s => s.CreateMouseRangeAsync(5, 1, "MC000001", "MC000002", 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 1;
        sut.MouseNumberFrom = "MC000001";
        sut.MouseNumberTo = "MC000002";

        var result = await sut.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _submissions.Verify(s => s.CreateMouseRangeAsync(5, 1, "MC000001", "MC000002", 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_MouseRange_ServiceFailure_SetsModelError()
    {
        _session.Object.BatchSubmissionID = 99;
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 50, BatchID = 5, AnimalID = 1, Order = 1 }]);
        _submissions.Setup(s => s.GetExistingSenderRefsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)[]);
        _submissions.Setup(s => s.CreateMouseRangeAsync(5, 1, "MC000001", "MC000002", 7, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut(returnPage: null);
        sut.BatchSubmissionId = 99;
        sut.SourceAnimalId = 1;
        sut.MouseNumberFrom = "MC000001";
        sut.MouseNumberTo = "MC000002";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Could not add the sample range. Please try again.", sut.ModelError);
    }
}
