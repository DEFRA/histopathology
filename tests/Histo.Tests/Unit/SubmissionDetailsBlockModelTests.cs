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

    [Fact]
    public async Task ShowHistologyRefTypePicker_NonHistopathUserArea_IsHidden()
    {
        var animal = new Animal { ID = 1, SenderRef = "S1" };
        _session.Setup(s => s.UserArea).Returns("Neuropath");
        var sut = CreateSut(1, animal, "/Batches/BatchBlocks?batchId=5");

        await sut.OnGetAsync();

        Assert.False(sut.ShowHistologyRefTypePicker);
    }

    [Fact]
    public async Task ShowHistologyRefTypePicker_HistopathUserAreaNoRefYet_IsShown()
    {
        var animal = new Animal { ID = 1, SenderRef = "S1" };
        _session.Setup(s => s.UserArea).Returns("Histopath");
        var sut = CreateSut(1, animal, "/Batches/BatchBlocks?batchId=5");

        await sut.OnGetAsync();

        Assert.True(sut.ShowHistologyRefTypePicker);
    }

    [Fact]
    public async Task ShowHistologyRefTypePicker_HistopathUserAreaViaSubmissionJourney_IsShown()
    {
        // Legacy has no Assign-Tissue-vs-Submission-journey split at all — ddlHistologyType's
        // ONLY visibility gate is the Histopath user area, regardless of how the page was reached.
        var animal = new Animal { ID = 1, SenderRef = "S1" };
        _session.Setup(s => s.UserArea).Returns("Histopath");
        var sut = CreateSut(1, animal, "/Submissions/SampleSummary?batchId=5");

        await sut.OnGetAsync();

        Assert.True(sut.ShowHistologyRefTypePicker);
    }

    [Fact]
    public async Task ShowHistologyRefTypePicker_RefAlreadySet_StaysVisible()
    {
        // A Histopath user can re-pick a type at any time to change the Histology Ref, even once
        // one is already set — the "Or pick" section must stay on-screen and usable.
        var animal = new Animal { ID = 1, SenderRef = "S1", HistologyRef = "16/40135" };
        _session.Setup(s => s.UserArea).Returns("Histopath");
        var sut = CreateSut(1, animal, "/Batches/BatchBlocks?batchId=5");

        await sut.OnGetAsync();

        Assert.True(sut.ShowHistologyRefTypePicker);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRefAsync_RefAlreadySet_CanStillBeChanged()
    {
        var animal = new Animal { ID = 1, SenderRef = "S1", HistologyRef = "16/40135" };
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _histologyRefs.Setup(h => h.GetUnusedRefsAsync(HistologyRefTypeCode.Neuropath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[]);
        _histologyRefs.Setup(h => h.GetCountersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[new HistologyRefCounter { Type = HistologyRefTypeCode.Neuropath, NextHistologyRef = "10001" }]);
        _histologyRefs.Setup(h => h.SetCounterAsync(HistologyRefTypeCode.Neuropath, "10002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Animal? saved = null;
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<Animal, int, CancellationToken>((a, _, _) => saved = a)
            .ReturnsAsync(true);
        var sut = CreateSut(1, animal, "/Batches/BatchBlocks?batchId=5");
        sut.HistologyRefType = HistologyRefTypeCode.Neuropath;

        await sut.OnPostGetNextHistologyRefAsync();

        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal($"{DateTime.Now.Year % 100:D2}/10001", saved?.HistologyRef);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRefAsync_NoPreBookedRef_FallsBackToCounterAndAdvancesIt()
    {
        var animal = new Animal { ID = 1, SenderRef = "S1" };
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _histologyRefs.Setup(h => h.GetUnusedRefsAsync(HistologyRefTypeCode.Neuropath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[]);
        _histologyRefs.Setup(h => h.GetCountersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRefCounter>)[new HistologyRefCounter { Type = HistologyRefTypeCode.Neuropath, NextHistologyRef = "10001" }]);
        _histologyRefs.Setup(h => h.SetCounterAsync(HistologyRefTypeCode.Neuropath, "10002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Animal? saved = null;
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<Animal, int, CancellationToken>((a, _, _) => saved = a)
            .ReturnsAsync(true);
        var sut = CreateSut(1, animal, "/Batches/BatchBlocks?batchId=5");
        sut.HistologyRefType = HistologyRefTypeCode.Neuropath;

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.Equal($"{DateTime.Now.Year % 100:D2}/10001", saved?.HistologyRef);
        Assert.False(saved!.IsPGNumber);
        _histologyRefs.Verify(h => h.SetCounterAsync(HistologyRefTypeCode.Neuropath, "10002", It.IsAny<CancellationToken>()), Times.Once);
        // EditHistologyRef belongs to a different <form> than this handler posts, so it must be
        // repopulated here or the redisplayed page shows the field blank despite the save succeeding.
        Assert.Equal(saved.HistologyRef, sut.EditHistologyRef);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRefAsync_UsePgNumber_ReversesSenderRefIntoHistologyRef()
    {
        var animal = new Animal { ID = 1, SenderRef = "PG0001/26" };
        _session.Setup(s => s.UserArea).Returns("Histopath");
        Animal? saved = null;
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<Animal, int, CancellationToken>((a, _, _) => saved = a)
            .ReturnsAsync(true);
        var sut = CreateSut(1, animal, "/Batches/BatchBlocks?batchId=5");
        sut.HistologyRefType = HistologyRefTypeCode.UsePgNumber;

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.Equal("26/00001", saved?.HistologyRef);
        Assert.True(saved!.IsPGNumber);
        Assert.Equal("26/00001", sut.EditHistologyRef);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRefAsync_UsePgNumber_NonPgSenderRef_ShowsFormatHint()
    {
        // Legacy: SenderRef.ascx.vb::CheckPGNumber's SetErrorToolTip("PG Number Format: PGNNNN/NN"),
        // shown when the Sender Ref doesn't match the PG format at all.
        var animal = new Animal { ID = 1, SenderRef = "2655-09" };
        _session.Setup(s => s.UserArea).Returns("Histopath");
        var sut = CreateSut(1, animal, "/Batches/BatchBlocks?batchId=5");
        sut.HistologyRefType = HistologyRefTypeCode.UsePgNumber;

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.Equal("PG Number Format: PGNNNN/NN", sut.ErrorMessage);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRefAsync_UsePgNumber_YearOutsideWindow_IsSilentNoOp()
    {
        // Legacy: DefaultHistoRefPGReverse only suppresses HistologyRef1.Text when IsAfter01(strYear)
        // — a valid PG format with an ineligible year is a silent no-op, not an error.
        var animal = new Animal { ID = 1, SenderRef = "PG0001/70" };
        _session.Setup(s => s.UserArea).Returns("Histopath");
        var sut = CreateSut(1, animal, "/Batches/BatchBlocks?batchId=5");
        sut.HistologyRefType = HistologyRefTypeCode.UsePgNumber;

        await sut.OnPostGetNextHistologyRefAsync();

        Assert.Null(sut.ErrorMessage);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostGetNextHistologyRefAsync_NonHistopathUserArea_IsIgnored()
    {
        var animal = new Animal { ID = 1, SenderRef = "PG0001/26" };
        _session.Setup(s => s.UserArea).Returns("Neuropath");
        var sut = CreateSut(1, animal, "/Batches/BatchBlocks?batchId=5");
        sut.HistologyRefType = HistologyRefTypeCode.UsePgNumber;

        await sut.OnPostGetNextHistologyRefAsync();

        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Null(sut.Animal!.HistologyRef);
    }
}
