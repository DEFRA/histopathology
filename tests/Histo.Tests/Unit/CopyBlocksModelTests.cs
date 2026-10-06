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
/// Verifies <see cref="CopyBlocksModel"/>'s "Auto generate histology refs" option (legacy
/// <c>CopyBlocks.aspx.vb</c>'s <c>cbAutoGenerateHisto</c>) — only samples with no histology ref
/// yet are assigned one, using the type derived from the source sample's own ref.
/// </summary>
public class CopyBlocksModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IHistologyRefService> _histologyRefs = new();

    public CopyBlocksModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, BatchID = 5, AnimalID = 10, BlockRef = "01" }]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.CopyBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<List<string>>(), It.IsAny<List<int>>(), It.IsAny<int>()))
            .ReturnsAsync(99);
    }

    private CopyBlocksModel CreateSut() =>
        new(_session.Object, _blocks.Object, _submissions.Object, _batches.Object, _histologyRefs.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            BatchId = 5,
            BlockIds = [1],
        };

    [Fact]
    public async Task OnPostAsync_AutoGenerateUnchecked_LeavesTargetHistologyRefUnchanged()
    {
        var target = new Animal { ID = 20, SenderRef = "S2", HistoRefSet = false };
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 10, SenderRef = "S1", HistologyRef = "26/60001", HistoRefSet = true }, target]);

        var sut = CreateSut();
        sut.TargetAnimalIds = [20];
        sut.AutoGenerateHistologyRefs = false;

        await sut.OnPostAsync();

        _histologyRefs.Verify(h => h.GetNextAvailableRefAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_AutoGenerateChecked_AssignsNextRefOnlyToSamplesWithoutOne()
    {
        var withoutRef = new Animal { ID = 20, SenderRef = "S2", HistoRefSet = false };
        var withRef = new Animal { ID = 30, SenderRef = "S3", HistologyRef = "26/60050", HistoRefSet = true };
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[
                new Animal { ID = 10, SenderRef = "S1", HistologyRef = "26/60001", HistoRefSet = true },
                withoutRef, withRef]);
        _histologyRefs.Setup(h => h.GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()))
            .ReturnsAsync("26/60002");
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();
        sut.TargetAnimalIds = [20, 30];
        sut.AutoGenerateHistologyRefs = true;

        await sut.OnPostAsync();

        _histologyRefs.Verify(h => h.GetNextAvailableRefAsync(HistologyRefTypeCode.MouseProjects, It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.UpdateAnimalAsync(
            It.Is<Animal>(a => a.ID == 20 && a.HistologyRef == "26/60002" && a.HistoRefSet), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.UpdateAnimalAsync(
            It.Is<Animal>(a => a.ID == 30), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_SourceHasNoHistologyRef_SkipsAutoGenerateSilently()
    {
        var target = new Animal { ID = 20, SenderRef = "S2", HistoRefSet = false };
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 10, SenderRef = "S1", HistologyRef = null, HistoRefSet = false }, target]);

        var sut = CreateSut();
        sut.TargetAnimalIds = [20];
        sut.AutoGenerateHistologyRefs = true;

        await sut.OnPostAsync();

        _histologyRefs.Verify(h => h.GetNextAvailableRefAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
