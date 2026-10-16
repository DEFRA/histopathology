using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Blocks;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the "Samples copied" confirmation page — previously had zero test
/// coverage of any kind (Tier 1 gap). A single-page display test over Moq mocks is appropriate
/// here (no cross-page state to chain, unlike the stateful-fake suites elsewhere).
/// </summary>
public class CopySamplesSummaryRegressionTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    private CopySamplesSummaryModel CreateSut() => new(_session.Object, _batches.Object, _submissions.Object)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() },
    };

    [Fact]
    public async Task OnGetAsync_ResolvesTheSourceAnimalFromTheSourceBatch()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 5, SenderRef = "S005" }]);
        var sut = CreateSut();

        await sut.OnGetAsync(sourceBatchId: 10, sourceAnimalId: 5, targetAnimalIds: null, blocksCopiedCount: 2);

        Assert.Equal(10, sut.SourceBatchId);
        Assert.Equal(2, sut.BlocksCopiedCount);
        Assert.Equal("S005", sut.SourceAnimal?.SenderRef);
    }

    [Fact]
    public async Task OnGetAsync_NoTargetIds_SkipsTheAccessCheckAndLeavesTargetAnimalsEmpty()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        var sut = CreateSut();

        await sut.OnGetAsync(sourceBatchId: 10, sourceAnimalId: 5, targetAnimalIds: null, blocksCopiedCount: 0);

        Assert.Empty(sut.TargetAnimals);
        _batches.Verify(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_WithTargetIds_ResolvesAndSortsTargetAnimalsBySenderRef()
    {
        _session.Setup(s => s.BatchID).Returns(10);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 10 });
        _submissions.SetupSequence(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "S001" }]) // source batch lookup
            .ReturnsAsync((IReadOnlyList<Animal>)[ // target batch lookup
                new Animal { ID = 2, SenderRef = "Zebra" },
                new Animal { ID = 3, SenderRef = "Alpha" },
            ]);
        var sut = CreateSut();

        await sut.OnGetAsync(sourceBatchId: 10, sourceAnimalId: 1, targetAnimalIds: "2,3", blocksCopiedCount: 4);

        Assert.Equal(2, sut.TargetAnimals.Count);
        Assert.Equal("Alpha", sut.TargetAnimals[0].SenderRef); // sorted by SenderRef
        Assert.Equal("Zebra", sut.TargetAnimals[1].SenderRef);
    }

    [Fact]
    public async Task OnGetAsync_MalformedTargetIdsCsv_IgnoresNonNumericEntries()
    {
        _session.Setup(s => s.BatchID).Returns(10);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 10 });
        _submissions.SetupSequence(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[])
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S002" }]);
        var sut = CreateSut();

        await sut.OnGetAsync(sourceBatchId: 10, sourceAnimalId: 1, targetAnimalIds: "2,abc,,0,-1", blocksCopiedCount: 1);

        var animal = Assert.Single(sut.TargetAnimals);
        Assert.Equal(2, animal.ID);
    }

    [Fact]
    public void OnPostDone_RedirectsToBatchBlocks()
    {
        var sut = CreateSut();

        var result = sut.OnPostDone();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchBlocks", redirect.PageName);
    }
}
