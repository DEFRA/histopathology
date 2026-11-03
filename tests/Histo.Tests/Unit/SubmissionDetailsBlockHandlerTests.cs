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
using Microsoft.Extensions.Logging;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Handler-level branches of the sample blocks page: access, validation, delete, edit, copy and histology ref assignment.</summary>
public class SubmissionDetailsBlockHandlerTests
{
    private static readonly string Yy = (DateTime.Now.Year % 100).ToString("D2");

    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockTestService> _blockTests = new();
    private readonly Mock<IHistologyRefService> _histologyRefs = new();

    public SubmissionDetailsBlockHandlerTests()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _blocks.Setup(b => b.GetUsedBlockRefsBySenderRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyRefTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _histologyRefs.Setup(h => h.GetUnusedRefsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<HistologyRef>)[]);
        _histologyRefs.Setup(h => h.GetCountersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[]);
    }

    private SubmissionDetailsBlockModel CreateSut(Animal? animal = null, int? batchId = 5, int? animalId = 1,
        string? userArea = "Histopath", bool viewMode = false)
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.AnimalID);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _session.Setup(s => s.UserArea).Returns(userArea ?? string.Empty);
        _session.Setup(s => s.IsViewSubmissionMode).Returns(viewMode);
        if (animal is not null)
            _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<Animal>)[animal]);

        return new SubmissionDetailsBlockModel(_session.Object, _submissions.Object, _blocks.Object, _batches.Object,
            _lookups.Object, _blockTests.Object, _histologyRefs.Object, Mock.Of<ILogger<SubmissionDetailsBlockModel>>())
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            BatchId = batchId,
            AnimalId = animalId,
        };
    }

    private void GivenBlocks(params Block[] blocks) =>
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)blocks);

    private void GivenCompletedBlock()
    {
        GivenBlocks(new Block { ID = 100, BatchID = 5, AnimalID = 1, BlockRef = "01", Status = BlockStatus.Used });
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 1, OwnerID = 100, Owner = TissueOwner.Block, TissueCode = "AGAR" }]);
    }

    [Fact]
    public async Task OnGet_NoAnimalId_RedirectsToBatchBlocksOverview()
    {
        var sut = CreateSut(animalId: null);

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchBlocks", redirect.PageName);
        Assert.Equal(5, redirect.RouteValues!["batchId"]);
    }

    [Fact]
    public async Task OnGet_NoBatchAnywhere_RedirectsToIndex()
    {
        var sut = CreateSut(batchId: null);

        var result = await sut.OnGetAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGet_BatchBelongsToAnotherArea_ReturnsForbid()
    {
        var sut = CreateSut();
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.IsMaintenance).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(1);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, UserAreaCode = 9 });

        var result = await sut.OnGetAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnGet_AnimalNotInBatch_ReturnsPageWithNoAnimal()
    {
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(sut.Animal);
    }

    [Fact]
    public async Task OnGet_TempDataErrors_ArePassedToTheView()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        sut.TempData["SubmissionDetailsBlock_Error"] = "boom";
        sut.TempData["SubmissionDetailsBlock_ErrorFieldId"] = "EditHistologyRef";

        await sut.OnGetAsync();

        Assert.Equal("boom", sut.ErrorMessage);
        Assert.Equal("EditHistologyRef", sut.ErrorFieldId);
    }

    [Fact]
    public async Task OnGet_ShowUsedRefs_LoadsUsedBlockRefRanges()
    {
        var sut = CreateSut(new Animal { ID = 1, SenderRef = "S1" });
        sut.ShowUsedRefs = true;
        _blocks.Setup(b => b.GetUsedBlockRefsBySenderRefAsync("S1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[new UsedBlockRef { BlockRef = 1, Status = BlockStatus.Used }, new UsedBlockRef { BlockRef = 2, Status = BlockStatus.Used }]);

        await sut.OnGetAsync();

        Assert.NotEmpty(sut.UsedBlockRefResults);
    }

    [Fact]
    public async Task OnPostConfirmDelete_StoresBlockIdsAndRendersPage()
    {
        var sut = CreateSut(new Animal { ID = 1 });

        var result = await sut.OnPostConfirmDeleteAsync([100, 101]);

        Assert.IsType<PageResult>(result);
        Assert.Equal([100, 101], sut.ConfirmDeleteBlockIds);
    }

    [Fact]
    public async Task OnPostConfirmDelete_NullIds_LeavesNoPendingDeletes()
    {
        var sut = CreateSut(new Animal { ID = 1 });

        await sut.OnPostConfirmDeleteAsync(null);

        Assert.Empty(sut.ConfirmDeleteBlockIds);
    }

    [Fact]
    public void DisplayHelpers_ResolveTissueNamesAndBatchFlags()
    {
        var sut = CreateSut();

        Assert.True(sut.IsTse);
        Assert.False(sut.ShowTestDetails);
        Assert.Equal("AGAR", sut.TissueName("AGAR"));
    }

    [Fact]
    public async Task OnPostSaveHistologyDetails_ViewMode_ReturnsForbid()
    {
        var sut = CreateSut(new Animal { ID = 1 }, viewMode: true);

        var result = await sut.OnPostSaveHistologyDetailsAsync();

        Assert.IsType<ForbidResult>(result);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostSaveHistologyDetails_AnimalMissing_ReturnsPage()
    {
        var sut = CreateSut();

        var result = await sut.OnPostSaveHistologyDetailsAsync();

        Assert.IsType<PageResult>(result);
    }

    public static TheoryData<string, string> InvalidHistologyRefs => new()
    {
        { "2640004", "NN/NNNNN format" },
        { "99/10000", "cannot be greater than current year" },
        { $"{Yy}/40020", "must be less than the next available reference" },
    };

    [Theory]
    [MemberData(nameof(InvalidHistologyRefs))]
    public async Task OnPostSaveHistologyDetails_InvalidRef_ShowsErrorAndDoesNotSave(string histologyRef, string expectedFragment)
    {
        var sut = CreateSut(new Animal { ID = 1 });
        GivenCompletedBlock();
        _histologyRefs.Setup(h => h.GetCountersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[new HistologyRefCounter { Type = 4, NextHistologyRef = "40010" }]);
        sut.EditHistologyRef = histologyRef;

        var result = await sut.OnPostSaveHistologyDetailsAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(expectedFragment, sut.ErrorMessage, StringComparison.Ordinal);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostSaveHistologyDetails_RefAlreadyUsedElsewhere_ShowsDuplicateError()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        GivenCompletedBlock();
        _submissions.Setup(s => s.IsHistologyRefInUseAsync($"{Yy}/05000", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        sut.EditHistologyRef = $"{Yy}/05000";

        await sut.OnPostSaveHistologyDetailsAsync();

        Assert.Contains("already exists", sut.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostSaveHistologyDetails_NoBlocks_RequiresAtLeastOneBlock()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        sut.EditHistologyRef = null;

        await sut.OnPostSaveHistologyDetailsAsync();

        Assert.Equal("At least one block must be added against the sample.", sut.ErrorMessage);
    }

    [Theory]
    [InlineData("/Submissions/SampleSummary?batchId=5", true)]
    [InlineData(null, false)]
    public async Task OnPostSaveHistologyDetails_ChangedValues_SavesAndRedirectsToReturnPage(string? returnPage, bool local)
    {
        var sut = CreateSut(new Animal { ID = 1, SenderRef = "S1" });
        GivenCompletedBlock();
        _session.Setup(s => s.SampleDetailReturnPage).Returns(returnPage);
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        sut.EditHistologyRef = $"{Yy}/05000";
        sut.EditPMDate = "2026-01-02";

        var result = await sut.OnPostSaveHistologyDetailsAsync();

        if (local) Assert.Equal(returnPage, Assert.IsType<LocalRedirectResult>(result).Url);
        else Assert.IsType<RedirectToPageResult>(result);
        _submissions.Verify(s => s.UpdateAnimalAsync(
            It.Is<Animal>(a => a.HistologyRef == $"{Yy}/05000" && a.HistoRefSet && a.PMDateSet), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostSaveHistologyDetails_UnchangedValues_DoesNotCallUpdate()
    {
        var sut = CreateSut(new Animal { ID = 1, HistologyRef = null, PMDate = null });
        GivenCompletedBlock();
        sut.EditHistologyRef = null;
        sut.EditPMDate = null;

        await sut.OnPostSaveHistologyDetailsAsync();

        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostSaveHistologyDetails_UpdateRejected_ShowsErrorInsteadOfSuccess()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        GivenCompletedBlock();
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        sut.EditHistologyRef = $"{Yy}/05000";

        var result = await sut.OnPostSaveHistologyDetailsAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("could not be saved", sut.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostSaveHistologyDetails_LockedFields_KeepExistingValues()
    {
        var animal = new Animal { ID = 1, HistoRefSet = true, HistologyRef = $"{Yy}/05001", PMDateSet = true, PMDate = "01/01/2026" };
        var sut = CreateSut(animal);
        GivenCompletedBlock();
        _session.Setup(s => s.SampleDetailReturnPage).Returns("/Submissions/SampleSummary");
        sut.EditHistologyRef = $"{Yy}/05999";
        sut.EditPMDate = "2020-05-05";

        await sut.OnPostSaveHistologyDetailsAsync();

        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal($"{Yy}/05001", sut.EditHistologyRef);
    }

    [Theory]
    [InlineData("26/70000")]
    [InlineData("26/15000")]
    [InlineData("26/25000")]
    [InlineData("26/35000")]
    public async Task OnPostSaveHistologyDetails_RangeWithoutCounterRow_PassesValidation(string histologyRef)
    {
        var sut = CreateSut(new Animal { ID = 1 });
        GivenCompletedBlock();
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        sut.EditHistologyRef = $"{Yy}{histologyRef[2..]}";

        var result = await sut.OnPostSaveHistologyDetailsAsync();

        Assert.IsNotType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostSaveHistologyDetails_PreviousYearRef_SkipsNextRefCheck()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        GivenCompletedBlock();
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        sut.EditHistologyRef = "01/40999";

        var result = await sut.OnPostSaveHistologyDetailsAsync();

        Assert.IsNotType<PageResult>(result);
        _histologyRefs.Verify(h => h.GetCountersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public async Task OnPostBack_ClearsNewHrOnlyWhenAssignedThisVisitAndNoBlock(bool assignedThisVisit, bool hasBlock, bool expectCleared)
    {
        var sut = CreateSut(new Animal { ID = 1, HistoRefSet = true, HistologyRef = $"{Yy}/40001" });
        if (hasBlock) GivenCompletedBlock();
        sut.HrAssignedThisVisit = assignedThisVisit;

        var result = await sut.OnPostBackAsync();

        Assert.IsType<LocalRedirectResult>(result);
        _submissions.Verify(s => s.UpdateAnimalAsync(
            It.Is<Animal>(a => !a.HistoRefSet && a.HistologyRef == null), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            expectCleared ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task OnPostBack_ViewMode_NeverClearsRef()
    {
        var sut = CreateSut(new Animal { ID = 1, HistoRefSet = true, HistologyRef = $"{Yy}/40001" }, viewMode: true);
        sut.HrAssignedThisVisit = true;

        await sut.OnPostBackAsync();

        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostBack_NoBatch_RedirectsToIndex()
    {
        var sut = CreateSut(batchId: null);

        var result = await sut.OnPostBackAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostDelete_BlockOutsideBatch_RejectsWholeDelete()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        GivenBlocks(new Block { ID = 100, AnimalID = 1, BlockRef = "01", Status = BlockStatus.Used });

        var result = await sut.OnPostDeleteAsync([100, 999]);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Contains("do not belong", (string)sut.TempData["SubmissionDetailsBlock_Error"]!, StringComparison.Ordinal);
        _blocks.Verify(b => b.DeleteBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostDelete_UsedBlock_AdvancesCounterThenDeletes()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        GivenBlocks(new Block { ID = 100, AnimalID = 1, BlockRef = "03", Status = BlockStatus.Used });
        _submissions.Setup(s => s.AdvanceNextBlockRefAsync(1, "04", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _blocks.Setup(b => b.DeleteBlockAsync(100, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await sut.OnPostDeleteAsync([100]);

        _blocks.Verify(b => b.DeleteBlockAsync(100, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(sut.TempData.ContainsKey("SubmissionDetailsBlock_Error"));
    }

    [Fact]
    public async Task OnPostDelete_CounterAdvanceFails_SkipsDeleteAndReportsFailure()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        GivenBlocks(new Block { ID = 100, AnimalID = 1, BlockRef = "03", Status = BlockStatus.Used });
        _submissions.Setup(s => s.AdvanceNextBlockRefAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await sut.OnPostDeleteAsync([100]);

        _blocks.Verify(b => b.DeleteBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains("100", (string)sut.TempData["SubmissionDetailsBlock_Error"]!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostDelete_PreBookedBlockDeleteFails_ReportsFailureWithoutAdvancingCounter()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        GivenBlocks(new Block { ID = 100, AnimalID = 1, BlockRef = "03", Status = BlockStatus.PreBooked });
        _blocks.Setup(b => b.DeleteBlockAsync(100, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await sut.OnPostDeleteAsync([100]);

        _submissions.Verify(s => s.AdvanceNextBlockRefAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains("Could not delete", (string)sut.TempData["SubmissionDetailsBlock_Error"]!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostDelete_NullIds_JustRedirects()
    {
        var sut = CreateSut(new Animal { ID = 1 });

        var result = await sut.OnPostDeleteAsync(null);

        Assert.IsType<RedirectToPageResult>(result);
        _blocks.Verify(b => b.DeleteBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostDelete_NoBatch_RedirectsToIndex()
    {
        var sut = CreateSut(batchId: null);

        var result = await sut.OnPostDeleteAsync([1]);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostEditBlockSelect_NoBatch_RedirectsToIndex()
    {
        var sut = CreateSut(batchId: null);

        var result = await sut.OnPostEditBlockSelectAsync([1]);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostEditBlockSelect_AnimalMissing_RedirectsToSampleSummary()
    {
        var sut = CreateSut();

        var result = await sut.OnPostEditBlockSelectAsync([1]);

        Assert.Equal("/Submissions/SampleSummary", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new int[0])]
    [InlineData(new[] { 1, 2 })]
    public async Task OnPostEditBlockSelect_NotExactlyOneBlock_ShowsErrorOnPage(int[]? ids)
    {
        var sut = CreateSut(new Animal { ID = 1 });
        GivenCompletedBlock();

        var result = await sut.OnPostEditBlockSelectAsync(ids?.ToList());

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select exactly one block to edit.", sut.ErrorMessage);
        Assert.Single(sut.Blocks);
    }

    [Fact]
    public async Task OnPostEditBlockSelect_ExactlyOneBlock_RedirectsToBlockDetails()
    {
        var sut = CreateSut(new Animal { ID = 1 });

        var result = await sut.OnPostEditBlockSelectAsync([42]);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Blocks/BlockDetails", redirect.PageName);
        Assert.Equal(42, redirect.RouteValues!["blockId"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new int[0])]
    public async Task OnPostCopy_NothingSelected_RedirectsBackToThePage(int[]? ids)
    {
        var sut = CreateSut(new Animal { ID = 1 });

        var result = await sut.OnPostCopyAsync(ids?.ToList());

        Assert.Null(Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostCopy_NothingSelectedAndNoBatch_RedirectsToIndex()
    {
        var sut = CreateSut(batchId: null);

        var result = await sut.OnPostCopyAsync(null);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostCopy_WithSelection_StoresIdsAndRedirectsToCopyBlocks()
    {
        var sut = CreateSut(new Animal { ID = 1 });

        var result = await sut.OnPostCopyAsync([3, 4]);

        Assert.Equal("/Blocks/CopyBlocks", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("3,4", sut.TempData["CopyBlockIds"]);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRef_NoBatch_RedirectsToIndex()
    {
        var sut = CreateSut(batchId: null);

        var result = await sut.OnPostGetNextHistologyRefAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRef_AnimalMissing_RedirectsToSampleSummary()
    {
        var sut = CreateSut();

        var result = await sut.OnPostGetNextHistologyRefAsync();

        Assert.Equal("/Submissions/SampleSummary", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Theory]
    [InlineData("Customer", 4)]
    [InlineData("Histopath", null)]
    [InlineData("Histopath", 0)]
    public async Task OnPostGetNextHistologyRef_PickerUnavailableOrNoType_DoesNotAssign(string area, int? type)
    {
        var sut = CreateSut(new Animal { ID = 1 }, userArea: area);
        sut.HistologyRefType = type;

        var result = await sut.OnPostGetNextHistologyRefAsync();

        Assert.IsType<PageResult>(result);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRef_PooledRefOfCurrentYear_UsesLowestPooledRef()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        sut.HistologyRefType = HistologyRefTypeCode.GeneralPool;
        _histologyRefs.Setup(h => h.GetUnusedRefsAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<HistologyRef>)[
            new HistologyRef { Ref = $"{Yy}/40009", HistologyType = 4 },
            new HistologyRef { Ref = $"{Yy}/40005", HistologyType = 4 },
            new HistologyRef { Ref = "01/40001", HistologyType = 4 },
        ]);

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.Equal($"{Yy}/40005", sut.Animal!.HistologyRef);
        Assert.True(sut.HrAssignedThisVisit);
        Assert.Equal($"{Yy}/40005", sut.EditHistologyRef);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRef_EmptyPool_MintsFromCounterAndAdvancesIt()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        sut.HistologyRefType = HistologyRefTypeCode.GeneralPool;
        _histologyRefs.Setup(h => h.GetCountersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[new HistologyRefCounter { Type = 4, NextHistologyRef = "40010" }]);
        _histologyRefs.Setup(h => h.SetCounterAsync(4, "40011", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.EndsWith("/40010", sut.Animal!.HistologyRef, StringComparison.Ordinal);
        Assert.Null(sut.ErrorMessage);
    }

    [Theory]
    [InlineData(false, "40010")]
    [InlineData(true, "abc")]
    [InlineData(true, "39999")]
    [InlineData(true, "60000")]
    public async Task OnPostGetNextHistologyRef_CounterUnusable_ShowsNoRefAvailable(bool hasCounter, string next)
    {
        var sut = CreateSut(new Animal { ID = 1 });
        sut.HistologyRefType = HistologyRefTypeCode.GeneralPool;
        _histologyRefs.Setup(h => h.GetCountersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasCounter ? (IReadOnlyList<HistologyRefCounter>)[new HistologyRefCounter { Type = 4, NextHistologyRef = next }] : []);

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.Contains("No histology reference is available", sut.ErrorMessage, StringComparison.Ordinal);
        Assert.False(sut.HrAssignedThisVisit);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRef_CounterAdvanceFails_ShowsNoRefAvailable()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        sut.HistologyRefType = HistologyRefTypeCode.GeneralPool;
        _histologyRefs.Setup(h => h.GetCountersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[new HistologyRefCounter { Type = 4, NextHistologyRef = "40010" }]);
        _histologyRefs.Setup(h => h.SetCounterAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.NotNull(sut.ErrorMessage);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRef_UnknownTypeWithNoRange_ShowsNoRefAvailable()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        sut.HistologyRefType = 99;

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.Contains("No histology reference is available", sut.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRef_UsePgNumberWithValidSenderRef_ReversesPgRef()
    {
        var sut = CreateSut(new Animal { ID = 1, SenderRef = "PG1234/20" });
        sut.HistologyRefType = HistologyRefTypeCode.UsePgNumber;

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.Equal("20/01234", sut.Animal!.HistologyRef);
        Assert.True(sut.Animal.IsPGNumber);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRef_UsePgNumberWithBadFormat_ShowsFormatError()
    {
        var sut = CreateSut(new Animal { ID = 1, SenderRef = "ABC" });
        sut.HistologyRefType = HistologyRefTypeCode.UsePgNumber;

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.Equal("PG Number Format: PGNNNN/NN", sut.ErrorMessage);
        Assert.Null(sut.Animal!.HistologyRef);
    }

    [Theory]
    [InlineData("PG1234/00")]
    [InlineData("PG1234/85")]
    public async Task OnPostGetNextHistologyRef_UsePgNumberOutsideYearWindow_IsSilentNoOp(string senderRef)
    {
        var sut = CreateSut(new Animal { ID = 1, SenderRef = senderRef });
        sut.HistologyRefType = HistologyRefTypeCode.UsePgNumber;

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.Null(sut.ErrorMessage);
        Assert.Null(sut.Animal!.HistologyRef);
    }

    [Fact]
    public async Task OnGet_SupportingDataLoadThrows_LogsAndStillRendersPage()
    {
        var sut = CreateSut(new Animal { ID = 1 });
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("shape"));

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Empty(sut.HistologyCodesByBlockId);
    }

    [Fact]
    public void BackLinkPage_FallsBackToSampleSummaryForBatch()
    {
        var sut = CreateSut();

        Assert.Equal("/Submissions/SampleSummary?batchId=5", sut.BackLinkPage);
    }

    [Theory]
    [InlineData("Histopath", false, true)]
    [InlineData(" histopath ", false, true)]
    [InlineData("Customer", false, false)]
    [InlineData(null, false, false)]
    [InlineData("Histopath", true, false)]
    public void HistologyRefTypePicker_OnlyForHistopathOutsideViewMode(string? area, bool viewMode, bool expected)
    {
        var sut = CreateSut(userArea: area, viewMode: viewMode);

        Assert.Equal(expected, sut.ShowHistologyRefTypePicker);
        Assert.Equal(!viewMode && expected || (area?.Trim().Equals("Histopath", StringComparison.OrdinalIgnoreCase) == true), sut.HistologyRefRequired);
    }
}
