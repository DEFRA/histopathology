using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Blocks;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Guard and validation branches of <see cref="CopySamplesModel"/> (find/copy preconditions).</summary>
public class CopySamplesModelValidationTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBlockTestService> _blockTests = new();

    public CopySamplesModelValidationTests()
    {
        _session.SetupProperty(s => s.BatchID, 5);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, BatchType = BatchTypeConstants.Tse });
        _batches.Setup(b => b.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 9, BatchType = BatchTypeConstants.Tse });
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 20, SenderRef = "T1" }]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 100, SenderRef = "B" }, new Animal { ID = 101, SenderRef = "A" }]);
        _blocks.Setup(b => b.GetByBatchAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, BatchID = 9, AnimalID = 100, BlockRef = "01" }]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
    }

    private CopySamplesModel CreateSut() =>
        new(_session.Object, _batches.Object, _submissions.Object, _blocks.Object, _blockTests.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    private void ForbidCurrentBatch()
    {
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(1);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, UserAreaCode = 8 });
    }

    [Fact]
    public async Task OnGetAsync_WithBatch_ListsTargetAnimals()
    {
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Single(sut.TargetAnimals);
    }

    [Fact]
    public async Task OnGetAsync_NoSessionBatch_RedirectsToIndex()
    {
        _session.Object.BatchID = null;

        var result = await CreateSut().OnGetAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_UserOutsideArea_ReturnsForbid()
    {
        ForbidCurrentBatch();

        Assert.IsType<ForbidResult>(await CreateSut().OnGetAsync());
    }

    [Fact]
    public async Task OnPostFindAsync_NoSessionBatch_RedirectsToIndex()
    {
        _session.Object.BatchID = null;

        var result = await CreateSut().OnPostFindAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostFindAsync_UserOutsideArea_ReturnsForbid()
    {
        ForbidCurrentBatch();

        Assert.IsType<ForbidResult>(await CreateSut().OnPostFindAsync());
    }

    [Fact]
    public async Task OnPostFindAsync_SourceBatchMissing_ReportsNotFound()
    {
        var sut = CreateSut();
        sut.SourceBatchId = 404;

        await sut.OnPostFindAsync();

        Assert.Equal("The selected submission could not be found.".Replace("selected submission", "submission to copy from"), sut.Error);
        Assert.Null(sut.SourceBatch);
    }

    [Fact]
    public async Task OnPostFindAsync_SubmissionTypesDiffer_ReportsTypeMismatch()
    {
        _batches.Setup(b => b.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 9, BatchType = BatchTypeConstants.NonTse });
        var sut = CreateSut();
        sut.SourceBatchId = 9;

        await sut.OnPostFindAsync();

        Assert.Contains("not the same type", sut.Error);
    }

    [Fact]
    public async Task OnPostFindAsync_SourceHasNoBlocks_ReportsNoBlocks()
    {
        _blocks.Setup(b => b.GetByBatchAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        var sut = CreateSut();
        sut.SourceBatchId = 9;

        await sut.OnPostFindAsync();

        Assert.Contains("no blocks assigned", sut.Error);
    }

    [Fact]
    public async Task OnPostFindAsync_BlocksBelongToUnknownAnimals_ReportsNoSamples()
    {
        _blocks.Setup(b => b.GetByBatchAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, BatchID = 9, AnimalID = 777, BlockRef = "01" }]);
        var sut = CreateSut();
        sut.SourceBatchId = 9;

        await sut.OnPostFindAsync();

        Assert.Contains("no samples with blocks", sut.Error);
        Assert.Empty(sut.SourceAnimals);
    }

    [Fact]
    public async Task OnPostFindAsync_SourceHasBlockedSamples_PopulatesSourceBatchAndAnimals()
    {
        var sut = CreateSut();
        sut.SourceBatchId = 9;

        var result = await sut.OnPostFindAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(sut.Error);
        Assert.Equal(9, sut.SourceBatch?.ID);
        Assert.Equal([100], sut.SourceAnimals.Select(a => a.ID));
    }

    [Fact]
    public async Task OnPostCopyAsync_NoSessionBatch_RedirectsToIndex()
    {
        _session.Object.BatchID = null;

        var result = await CreateSut().OnPostCopyAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostCopyAsync_UserOutsideArea_ReturnsForbid()
    {
        ForbidCurrentBatch();

        Assert.IsType<ForbidResult>(await CreateSut().OnPostCopyAsync());
    }

    [Fact]
    public async Task OnPostCopyAsync_SourceBatchMissing_AsksToFindSourceFirst()
    {
        var sut = CreateSut();
        sut.SourceBatchId = 404;

        var result = await sut.OnPostCopyAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Find a source submission before copying samples.", sut.Error);
    }

    [Fact]
    public async Task OnPostCopyAsync_NoTargetSamples_AsksToSelectTargets()
    {
        var sut = CreateSut();
        sut.SourceBatchId = 9;
        sut.SourceAnimalId = 100;
        sut.TargetAnimalIds = [];

        await sut.OnPostCopyAsync();

        Assert.StartsWith("Select at least one sample", sut.Error);
    }

    [Fact]
    public async Task OnPostCopyAsync_SourceSampleHasNoBlocks_AsksToSelectSourceSample()
    {
        var sut = CreateSut();
        sut.SourceBatchId = 9;
        sut.SourceAnimalId = 101;
        sut.TargetAnimalIds = [20];

        await sut.OnPostCopyAsync();

        Assert.Equal("Select a sample to copy from.", sut.Error);
        _blocks.Verify(b => b.CopyBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task OnPostCopyAsync_UnknownTargetAnimal_IsSkippedAndRedirectsWithZeroCopied()
    {
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[]);
        var sut = CreateSut();
        sut.SourceBatchId = 9;
        sut.SourceAnimalId = 100;
        sut.TargetAnimalIds = [999];

        var result = await sut.OnPostCopyAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Blocks/CopySamplesSummary", redirect.PageName);
        Assert.Equal(0, redirect.RouteValues?["blocksCopiedCount"]);
        Assert.Equal("999", redirect.RouteValues?["targetAnimalIds"]);
    }

    [Fact]
    public void OnPostCancel_RedirectsToBatchBlocks()
    {
        var result = CreateSut().OnPostCancel();

        Assert.Equal("/Batches/BatchBlocks", Assert.IsType<RedirectToPageResult>(result).PageName);
    }
}
