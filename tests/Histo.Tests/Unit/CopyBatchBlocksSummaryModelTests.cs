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

/// <summary>Unit tests for <see cref="CopyBatchBlocksSummaryModel"/> — the read-only pre-copy
/// preview reached from <see cref="CopyBatchModel"/>'s cassetted-only "Summary" button.</summary>
public class CopyBatchBlocksSummaryModelTests
{
    private const int SourceBatchId = 42;

    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockTestService> _blockTests = new();

    public CopyBatchBlocksSummaryModelTests()
    {
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _batches.Setup(b => b.GetByIdAsync(SourceBatchId, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = SourceBatchId });
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _blockTests.Setup(b => b.GetAllSelectionsByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
    }

    private CopyBatchBlocksSummaryModel CreateSut() =>
        new(_session.Object, _batches.Object, _submissions.Object, _blocks.Object, _lookups.Object, _blockTests.Object, Mock.Of<ILogger<CopyBatchBlocksSummaryModel>>())
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnGetAsync_TestSelectionsFailToLoad_StillShowsThePage()
    {
        _blocks.Setup(b => b.GetByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 100, AnimalID = 7, BlockRef = "01" }]);
        _blockTests.Setup(b => b.GetAllSelectionsByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SP shape mismatch"));
        var sut = CreateSut();

        var result = await sut.OnGetAsync(SourceBatchId);

        Assert.IsType<PageResult>(result);
        Assert.Single(sut.Blocks);
        Assert.Empty(sut.HistologyCodesByBlockId);
    }

    [Fact]
    public async Task OnGetAsync_NoBlocks_ShowsEmptyGridWithoutError()
    {
        var sut = CreateSut();

        var result = await sut.OnGetAsync(SourceBatchId);

        Assert.IsType<PageResult>(result);
        Assert.Empty(sut.Blocks);
    }

    [Fact]
    public async Task OnGetAsync_SourceBatchNotFound_RedirectsToIndex()
    {
        _batches.Setup(b => b.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnGetAsync(999);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_MapsSenderRefBlockRefAndTissueDetails()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 7, SenderRef = "S1" }]);
        _blocks.Setup(b => b.GetByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 100, AnimalID = 7, BlockRef = "01" }]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 1, OwnerID = 100, TissueCode = "1", NoPieces = 2 }]);
        _lookups.Setup(l => l.GetLookupDataAsync(9, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { Code = "1", Name = "Liver" }]);
        var sut = CreateSut();

        await sut.OnGetAsync(SourceBatchId);

        Assert.Equal("S1", sut.SenderRefsByAnimalId[7]);
        Assert.Equal("01", sut.Blocks[0].BlockRef);
        Assert.Equal("Liver", sut.TissueName("1"));
        Assert.Single(sut.TissuesByBlockId[100]);
    }

    [Fact]
    public async Task OnGetAsync_HistologyCodes_ReadFromGetAllSelectionsNotGetByBatch()
    {
        // Regression (matching BatchBlocksModel/SubmissionDetailsBlockModel precedent):
        // Special Stain/IHC-Prp/IHC-Other must come from GetAllSelectionsByBatchAsync, since
        // GetByBatchAsync excludes those codes (worklist-only exclusion there).
        _blocks.Setup(b => b.GetByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 100, AnimalID = 7, BlockRef = "01" }]);
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)
            [
                new BlockTest { ID = 1, BlockID = 100, TestType = BlockTestType.Histology, Code = HistologyCode.SpecialStain },
                new BlockTest { ID = 2, BlockID = 100, TestType = BlockTestType.Histology, Code = HistologyCode.IhcPrp },
                new BlockTest { ID = 3, BlockID = 100, TestType = BlockTestType.Histology, Code = HistologyCode.IhcOther },
            ]);
        var sut = CreateSut();

        await sut.OnGetAsync(SourceBatchId);

        var codes = sut.HistologyCodesByBlockId[100];
        Assert.Contains(HistologyCode.SpecialStain, codes);
        Assert.Contains(HistologyCode.IhcPrp, codes);
        Assert.Contains(HistologyCode.IhcOther, codes);
    }

    [Fact]
    public async Task OnGetAsync_RepeatBlock_ReadDirectlyFromBlock()
    {
        _blocks.Setup(b => b.GetByBatchAsync(SourceBatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 100, AnimalID = 7, BlockRef = "01", RepeatBlock = true }]);
        var sut = CreateSut();

        await sut.OnGetAsync(SourceBatchId);

        Assert.True(sut.Blocks[0].RepeatBlock);
    }
}
