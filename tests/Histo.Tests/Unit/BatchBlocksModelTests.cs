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

/// <summary>
/// Covers the "Assign tissues to blocks" delete-sample journey on <see cref="BatchBlocksModel"/>.
///
/// Regression: the handler used to call <c>DeleteAnimalAsync</c>, whose stored procedure only
/// removes the shared <c>Animal</c> row. With no FK cascades in this schema, the sample's blocks,
/// block tissues, block test selections, batch submissions and submission tissues were all left
/// behind — the sample was only partially deleted.
/// </summary>
public class BatchBlocksModelTests
{
    private const int BatchId = 29395;
    private const int AnimalId = 101531;

    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockTestService> _blockTests = new();
    private readonly Mock<IUserService> _users = new();

    public BatchBlocksModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.ReturnPage);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _session.Setup(s => s.UserID).Returns(99);

        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = BatchId });
        _batches.Setup(b => b.GetAnimalsWithUnassignedTissuesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int>)[]);
        _batches.Setup(b => b.CompleteBlockAssignmentAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _batches.Setup(b => b.RefreshAllTissuesAssignedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 317164, BatchID = BatchId, AnimalID = AnimalId, BlockRef = "01" }]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = AnimalId, SenderRef = "Test TSE" }]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.DeleteSampleFromBatchAsync(BatchId, AnimalId, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissions.Setup(s => s.DeleteSampleFromBatchAsync(BatchId, 42, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<User>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _blockTests.Setup(b => b.GetAllSelectionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[]);
    }

    private BatchBlocksModel CreateSut() =>
        new(_session.Object, _submissions.Object, _blocks.Object, _batches.Object, _lookups.Object,
            _blockTests.Object, _users.Object, Mock.Of<ILogger<BatchBlocksModel>>())
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            BatchId = BatchId,
        };

    [Fact]
    public async Task OnPostDeleteSampleAsync_RemovesTheWholeSampleFromTheSubmission()
    {
        var result = await CreateSut().OnPostDeleteSampleAsync(AnimalId);

        Assert.IsType<RedirectToPageResult>(result);
        _submissions.Verify(s => s.DeleteSampleFromBatchAsync(BatchId, AnimalId, 99, It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.DeleteAnimalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _batches.Verify(b => b.RefreshAllTissuesAssignedAsync(BatchId, 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostDeleteSampleAsync_DeleteFails_ShowsErrorAndStaysOnPage()
    {
        _submissions.Setup(s => s.DeleteSampleFromBatchAsync(BatchId, AnimalId, 99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.OnPostDeleteSampleAsync(AnimalId);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Could not delete this sample. No changes were made.", sut.ErrorMessage);
    }

    [Fact]
    public async Task OnPostDoneAsync_AllTissuesInBlocks_RecordsAllTissuesAssigned()
    {
        var sut = CreateSut();

        await sut.OnPostDoneAsync();

        _batches.Verify(b => b.CompleteBlockAssignmentAsync(BatchId, true, 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Defect 756752: the animal has a block, so the old animal-level check reported "all assigned"
    // even though a tissue added after blocking was still unassigned.
    [Fact]
    public async Task OnPostDoneAsync_AnimalHasBlockButUnassignedTissue_RecordsNotAllTissuesAssigned()
    {
        _batches.Setup(b => b.GetAnimalsWithUnassignedTissuesAsync(BatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int>)[AnimalId]);
        var sut = CreateSut();

        await sut.OnPostDoneAsync();

        _batches.Verify(b => b.CompleteBlockAssignmentAsync(BatchId, false, 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostDoneAsync_NoAnimals_RecordsNotAllTissuesAssigned()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        var sut = CreateSut();

        await sut.OnPostDoneAsync();

        _batches.Verify(b => b.CompleteBlockAssignmentAsync(BatchId, false, 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_ExposesAnimalsWithUnassignedTissuesForTheGridIndicator()
    {
        _batches.Setup(b => b.GetAnimalsWithUnassignedTissuesAsync(BatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int>)[AnimalId]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Contains(AnimalId, sut.AnimalsWithUnassignedTissues);
    }
}
