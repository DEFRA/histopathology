using Histo.Administration.Interfaces;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Verifies <see cref="SubmissionDetailsBlockModel.IsAssignTissueMode"/> and the PM date/Histology
/// ref lock gates it drives — PM date/Histology ref must always be editable when reached via the
/// Assign Tissues to Blocks journey (<c>Batches/BatchBlocks</c>), but keep the existing
/// editable-only-while-unset behaviour for the Create/Edit/View Submission journey
/// (<c>SampleSummary</c>), with zero change to that journey's outcome.
/// </summary>
public class SubmissionDetailsBlockModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockTestService> _blockTests = new();
    private readonly Mock<IHistologyRefService> _histologyRefs = new();

    public SubmissionDetailsBlockModelTests()
    {
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Histo.Administration.Models.LookupItem>)[]);
    }

    private SubmissionDetailsBlockModel CreateSut(int animalId, Animal animal, string? sampleDetailReturnPage)
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.AnimalID);
        _session.Setup(s => s.SampleDetailReturnPage).Returns(sampleDetailReturnPage);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[animal]);

        return new SubmissionDetailsBlockModel(_session.Object, _submissions.Object, _blocks.Object, _batches.Object,
            _lookups.Object, _blockTests.Object, _histologyRefs.Object, Mock.Of<ILogger<SubmissionDetailsBlockModel>>())
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            BatchId = 5,
            AnimalId = animalId,
        };
    }

    [Fact]
    public async Task AssignTissueJourney_AlreadyAssignedRef_StaysEditable()
    {
        var animal = new Animal { ID = 1, SenderRef = "S1", HistoRefSet = true, HistologyRef = "26/40001", PMDateSet = true, PMDate = "01/01/2026" };
        var sut = CreateSut(1, animal, "/Batches/BatchBlocks?batchId=5");

        await sut.OnGetAsync();

        Assert.True(sut.IsAssignTissueMode);
        Assert.False(sut.HistologyRefLocked);
        Assert.False(sut.PMDateLocked);
    }

    [Fact]
    public async Task CreateEditSubmissionJourney_AlreadyAssignedRef_StaysLocked()
    {
        var animal = new Animal { ID = 1, SenderRef = "S1", HistoRefSet = true, HistologyRef = "26/40001", PMDateSet = true, PMDate = "01/01/2026" };
        var sut = CreateSut(1, animal, "/Submissions/SampleSummary?batchId=5");

        await sut.OnGetAsync();

        Assert.False(sut.IsAssignTissueMode);
        Assert.True(sut.HistologyRefLocked);
        Assert.True(sut.PMDateLocked);
    }

    [Fact]
    public async Task CreateEditSubmissionJourney_UnassignedRef_StaysEditable()
    {
        var animal = new Animal { ID = 1, SenderRef = "S1", HistoRefSet = false, PMDateSet = false };
        var sut = CreateSut(1, animal, "/Submissions/SampleSummary?batchId=5");

        await sut.OnGetAsync();

        Assert.False(sut.IsAssignTissueMode);
        Assert.False(sut.HistologyRefLocked);
        Assert.False(sut.PMDateLocked);
    }
}
