using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Select-sample handlers, back link, view mode and fault tolerance of <see cref="BatchBlocksModel"/>.</summary>
public class BatchBlocksModelSelectionTests
{
    private const int BatchId = 500;

    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockTestService> _blockTests = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILogger<BatchBlocksModel>> _logger = new();

    public BatchBlocksModelSelectionTests()
    {
        _session.SetupAllProperties();
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _session.Setup(s => s.UserID).Returns(5);
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = BatchId });
        _batches.Setup(b => b.GetAnimalsWithUnassignedTissuesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[
                new Block { ID = 11, BatchID = BatchId, AnimalID = 71, BlockRef = "01" },
                new Block { ID = 12, BatchID = BatchId, AnimalID = 72, BlockRef = "02" }]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 71, SenderRef = "S71" }]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 71, SenderRef = "S71" }, new Animal { ID = 72, SenderRef = "S72" }]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Code = "BRAIN", Name = "Brain stem" }]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _blockTests.Setup(b => b.GetAllSelectionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[]);
    }

    private BatchBlocksModel CreateSut(int? batchId = BatchId) =>
        new(_session.Object, _submissions.Object, _blocks.Object, _batches.Object, _lookups.Object,
            _blockTests.Object, _users.Object, _logger.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            BatchId = batchId,
        };

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task OnGetAsync_NoUsableBatch_RedirectsToIndex(int? sessionBatch)
    {
        _session.Object.BatchID = sessionBatch;

        var result = await CreateSut(batchId: null).OnGetAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_BatchFromSession_LoadsGridAndStoresBatchId()
    {
        _session.Object.BatchID = BatchId;
        var sut = CreateSut(batchId: null);

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(BatchId, sut.BatchId);
        Assert.Equal(2, sut.Blocks.Count);
        Assert.Equal("S72", sut.SenderRefsByAnimalId[72]);
    }

    [Fact]
    public async Task OnGetAsync_UserOutsideBatchArea_ReturnsForbid()
    {
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(1);
        _batches.Setup(b => b.GetByIdAsync(BatchId, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = BatchId, UserAreaCode = 8 });

        var result = await CreateSut().OnGetAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_NonTseBatch_IsNotTse()
    {
        _batches.Setup(b => b.GetByIdAsync(BatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = BatchId, BatchType = BatchTypeConstants.NonTse });
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.False(sut.IsTse);
    }

    [Fact]
    public async Task TissueName_KnownCodeIsResolvedUnknownCodeFallsBack()
    {
        var sut = CreateSut();
        await sut.OnGetAsync();

        Assert.Equal("Brain stem", sut.TissueName("BRAIN"));
        Assert.Equal("ZZZ", sut.TissueName("ZZZ"));
    }

    [Fact]
    public async Task OnGetAsync_TestSelectionLoadThrows_LogsErrorAndStillRendersPage()
    {
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("shape"));
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Empty(sut.HistologyCodesByBlockId);
        _logger.Verify(l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Theory]
    [InlineData("", "/Batches/BatchesReceived")]
    [InlineData("  ", "/Batches/BatchesReceived")]
    [InlineData("/Search/SearchSubmissions", "/Search/SearchSubmissions")]
    public void BackLinkPage_FallsBackToBatchesReceived(string returnPage, string expected)
    {
        _session.Object.ReturnPage = returnPage;

        Assert.Equal(expected, CreateSut().BackLinkPage);
    }

    [Fact]
    public void IsViewMode_ReflectsSessionFlag()
    {
        _session.Object.IsViewSubmissionMode = true;

        Assert.True(CreateSut().IsViewMode);
    }

    [Fact]
    public async Task OnPostDeleteSampleAsync_ViewMode_ReturnsForbid()
    {
        _session.Object.IsViewSubmissionMode = true;

        var result = await CreateSut().OnPostDeleteSampleAsync(71);

        Assert.IsType<ForbidResult>(result);
        _submissions.Verify(s => s.DeleteSampleFromBatchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostEditSampleSelectAsync_OneBlockSelected_RedirectsToSampleBlocksPage()
    {
        var sut = CreateSut();

        var result = await sut.OnPostEditSampleSelectAsync([12]);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
        Assert.Equal(72, redirect.RouteValues?["animalId"]);
        Assert.Equal($"/Batches/BatchBlocks?batchId={BatchId}", _session.Object.SampleDetailReturnPage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new int[0])]
    [InlineData(new[] { 11, 12 })]
    [InlineData(new[] { 999 })]
    public async Task OnPostEditSampleSelectAsync_NotExactlyOneKnownBlock_ShowsErrorAndReloadsGrid(int[]? selected)
    {
        var sut = CreateSut();

        var result = await sut.OnPostEditSampleSelectAsync(selected?.ToList());

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select exactly one sample to edit.", sut.ErrorMessage);
        Assert.Equal(2, sut.Blocks.Count);
    }

    [Fact]
    public async Task OnPostEditSampleSelectAsync_NoBatch_RedirectsToIndex()
    {
        var result = await CreateSut(batchId: null).OnPostEditSampleSelectAsync([11]);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostDeleteSampleSelectAsync_OneBlockSelected_RedirectsWithConfirmationAnimal()
    {
        var result = await CreateSut().OnPostDeleteSampleSelectAsync([11]);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(71, redirect.RouteValues?["confirmDeleteAnimalId"]);
        Assert.Equal(BatchId, redirect.RouteValues?["batchId"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new int[0])]
    [InlineData(new[] { 11, 12 })]
    [InlineData(new[] { 999 })]
    public async Task OnPostDeleteSampleSelectAsync_NotExactlyOneKnownBlock_ShowsErrorAndReloadsGrid(int[]? selected)
    {
        var sut = CreateSut();

        var result = await sut.OnPostDeleteSampleSelectAsync(selected?.ToList());

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select exactly one sample to delete.", sut.ErrorMessage);
    }

    [Fact]
    public async Task OnPostDeleteSampleSelectAsync_NoBatch_RedirectsToIndex()
    {
        var result = await CreateSut(batchId: null).OnPostDeleteSampleSelectAsync([11]);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task OnPostDoneAsync_RecordsWhetherAllTissuesAreAssigned(bool noneUnassigned, bool expectedAllAssigned)
    {
        IReadOnlyCollection<int> unassigned = noneUnassigned ? [] : [71];
        _batches.Setup(b => b.GetAnimalsWithUnassignedTissuesAsync(BatchId, It.IsAny<CancellationToken>())).ReturnsAsync(unassigned);
        _batches.Setup(b => b.CompleteBlockAssignmentAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut().OnPostDoneAsync();

        Assert.Equal("/Batches/PrintSubmission", Assert.IsType<RedirectToPageResult>(result).PageName);
        _batches.Verify(b => b.CompleteBlockAssignmentAsync(BatchId, expectedAllAssigned, 5, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("/Batches/BatchesReceived", _session.Object.ReturnPage);
    }
}
