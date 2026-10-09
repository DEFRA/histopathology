using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Blocks;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// UAT TEST004.3 — "Copy from a previous submission" (<see cref="CopySamplesModel"/>) silently
/// dropped a copied block's Histology/Antibodies/Stain test-type ticks. Root cause: this page
/// had its own private block-copy loop that duplicated (and diverged from) the shared
/// <see cref="SampleCopyHelper.CopyBlocksToAnimalAsync"/> used by "Copy blocks"
/// (<see cref="CopyBlocksModel"/>) — it copied the block and its tissues but never called the
/// test-selection copy step, so <c>BatchBlocks</c>' H&amp;E(BSE)/IHC-PrP grid columns (and the
/// print output) came back blank for samples added via Copy from a previous submission.
/// </summary>
public class CopySamplesModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBlockTestService> _blockTests = new();

    public CopySamplesModelTests()
    {
        _session.SetupProperty(s => s.BatchID, 5);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private CopySamplesModel CreateSut() =>
        new(_session.Object, _batches.Object, _submissions.Object, _blocks.Object, _blockTests.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
        };

    [Fact]
    public async Task OnPostCopyAsync_SourceBlockHasTestSelections_CopiesThemOntoTheNewBlock()
    {
        var sourceBlock = new Block { ID = 1, BatchID = 9, AnimalID = 100, BlockRef = "01" };
        var target = new Animal { ID = 20, SenderRef = "C54322" };

        _batches.Setup(b => b.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 9 });
        _blocks.Setup(b => b.GetByBatchAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[sourceBlock]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 100, SenderRef = "C54321" }]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[target]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(9, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);
        // Source batch (9) and target batch (5) are different here — the test selections to copy
        // must be read from the source batch. A mock on the target batch's selections would mask
        // the real bug (reading tests from the wrong batch) if it happened to return the same data.
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[
                new BlockTest { BlockID = 1, TestType = BlockTestType.Histology, Code = "H&E(BSE)" },
                new BlockTest { BlockID = 1, TestType = BlockTestType.Histology, Code = "IHC-PrP" },
                new BlockTest { BlockID = 1, TestType = BlockTestType.Antibodies, Code = "F99" },
            ]);
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[]);
        _blocks.Setup(b => b.CopyBlockAsync(sourceBlock, 5, 20, It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<int>()))
            .ReturnsAsync(55);

        var sut = CreateSut();
        sut.SourceBatchId = 9;
        sut.SourceAnimalId = 100;
        sut.TargetAnimalIds = [20];

        await sut.OnPostCopyAsync();

        _blockTests.Verify(t => t.SaveTestSelectionsAsync(
            5, 55,
            It.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { "H&E(BSE)", "IHC-PrP" })),
            It.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { "F99" })),
            It.Is<IReadOnlyList<string>>(c => c.Count == 0),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
