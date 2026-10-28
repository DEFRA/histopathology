using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="SubmissionDetailsModel"/>.</summary>
public class SubmissionDetailsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();

    public SubmissionDetailsModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.AnimalID);
        _session.SetupProperty(s => s.SampleDetailReturnPage);
        _session.SetupGet(s => s.UserID).Returns(9);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private SubmissionDetailsModel CreateSut() =>
        new(_session.Object, _submissions.Object, _batches.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    /// <summary>Makes animal 5 resolvable in batch 42 under batch submission 77.</summary>
    private void SetupAnimal()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 5, SenderRef = "S5", PMDate = "01/02/2024", RowStamp = [1] }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 77, AnimalID = 5 }]);
    }

    [Fact]
    public void BackLinkPage_NoSampleDetailReturnPage_FallsBackToSampleSummaryForBatch()
    {
        var sut = CreateSut();
        sut.BatchId = 42;

        Assert.Equal("/Submissions/SampleSummary?batchId=42", sut.BackLinkPage);
    }

    [Fact]
    public void BackLinkPage_SampleDetailReturnPageSet_UsesIt()
    {
        _session.Object.SampleDetailReturnPage = "/Somewhere";
        var sut = CreateSut();

        Assert.Equal("/Somewhere", sut.BackLinkPage);
    }

    [Fact]
    public async Task OnGetAsync_NoBatch_RedirectsToIndex()
    {
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("Sample Details", sut.ViewData["Title"]);
    }

    [Fact]
    public async Task OnGetAsync_BatchButNoAnimal_RedirectsToSampleSummary()
    {
        _session.Object.BatchID = 42;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.Equal("/Submissions/SampleSummary", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_AnimalNotFound_RendersPageWithoutAnimal()
    {
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(sut.Animal);
    }

    [Fact]
    public async Task OnGetAsync_AnimalFound_LoadsTissuesResolvesSubmissionAndWetTissue()
    {
        SetupAnimal();
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync("W");
        _lookups.Setup(l => l.GetLookupDataAsync(11, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { Code = "W", Name = "Wet Tissue" }]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(42, 77, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 3, TissueCode = "BR", NoPieces = 2, Comment = "c" }]);
        var sut = CreateSut();
        sut.EditTissueId = 3;

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(77, sut.Animal!.BatchSubmissionID);
        Assert.True(sut.IsWetTissue);
        Assert.Single(sut.Tissues);
        Assert.Equal(3, sut.TissueId);
        Assert.Equal("BR", sut.EditTissueCode);
        Assert.Equal((short)2, sut.EditNoPieces);
        Assert.Equal("c", sut.EditComment);
    }

    [Fact]
    public async Task OnGetAsync_AnimalOnlyInAnimalList_StillResolved()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 5 }]);
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(5, sut.Animal!.ID);
    }

    [Fact]
    public async Task OnPostSaveDetailsAsync_NoReturnPage_UpdatesAnimalAndRedirectsToSelf()
    {
        SetupAnimal();
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        Animal? saved = null;
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), 9, It.IsAny<CancellationToken>()))
            .Callback<Animal, int, CancellationToken>((a, _, _) => saved = a).ReturnsAsync(true);
        var sut = CreateSut();
        sut.PMDate = "2024-02-01";

        var result = await sut.OnPostSaveDetailsAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(5, saved!.ID);
        Assert.Equal(77, saved.BatchSubmissionID);
        Assert.True(saved.PMDateSet);
    }

    [Fact]
    public async Task OnPostSaveDetailsAsync_ReturnPageSet_RedirectsToReturnPage()
    {
        SetupAnimal();
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        _session.Object.SampleDetailReturnPage = "/Submissions/SampleSummary";
        var sut = CreateSut();
        sut.Url = new Mock<IUrlHelper>().Object;

        var result = await sut.OnPostSaveDetailsAsync();

        Assert.Equal("/Submissions/SampleSummary", Assert.IsType<LocalRedirectResult>(result).Url);
    }

    [Fact]
    public async Task OnPostSaveDetailsAsync_AnimalNotFound_RedirectsToSampleSummary()
    {
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        var sut = CreateSut();

        var result = await sut.OnPostSaveDetailsAsync();

        Assert.Equal("/Submissions/SampleSummary", Assert.IsType<RedirectToPageResult>(result).PageName);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAddTissueAsync_WithCode_AddsTissueAndRefreshesBatchFlag()
    {
        SetupAnimal();
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        Tissue? added = null;
        _submissions.Setup(s => s.AddTissueAsync(It.IsAny<Tissue>(), 9, It.IsAny<CancellationToken>()))
            .Callback<Tissue, int, CancellationToken>((t, _, _) => added = t).ReturnsAsync(1);
        var sut = CreateSut();
        sut.TissueCode = "LV";
        sut.NoPieces = 3;

        var result = await sut.OnPostAddTissueAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(77, added!.OwnerID);
        Assert.Equal(TissueOwner.Submission, added.Owner);
        Assert.Equal("LV", added.TissueCode);
        _batches.Verify(b => b.RefreshAllTissuesAssignedAsync(42, 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAddTissueAsync_BlankCode_AddsNothing()
    {
        SetupAnimal();
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        var sut = CreateSut();

        await sut.OnPostAddTissueAsync();

        _submissions.Verify(s => s.AddTissueAsync(It.IsAny<Tissue>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateTissueAsync_ExistingTissue_UpdatesKeepingArchiveFields()
    {
        SetupAnimal();
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(42, 77, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 3, OwnerID = 77, TissueCode = "BR", ArchiveLocation = "A1" }]);
        Tissue? updated = null;
        _submissions.Setup(s => s.UpdateTissueAsync(It.IsAny<Tissue>(), 9, It.IsAny<CancellationToken>()))
            .Callback<Tissue, int, CancellationToken>((t, _, _) => updated = t).ReturnsAsync(true);
        var sut = CreateSut();
        sut.TissueId = 3;
        sut.EditTissueCode = "LV";
        sut.EditNoPieces = 4;

        await sut.OnPostUpdateTissueAsync();

        Assert.Equal("LV", updated!.TissueCode);
        Assert.Equal("A1", updated.ArchiveLocation);
        _batches.Verify(b => b.RefreshAllTissuesAssignedAsync(42, 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostUpdateTissueAsync_UnknownTissue_UpdatesNothing()
    {
        SetupAnimal();
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        var sut = CreateSut();
        sut.TissueId = 99;
        sut.EditTissueCode = "LV";

        var result = await sut.OnPostUpdateTissueAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _submissions.Verify(s => s.UpdateTissueAsync(It.IsAny<Tissue>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostDeleteTissueAsync_DeletesAndRefreshesBatchFlag()
    {
        _session.Object.BatchID = 42;
        _session.Object.AnimalID = 5;
        var sut = CreateSut();

        var result = await sut.OnPostDeleteTissueAsync(3);

        Assert.IsType<RedirectToPageResult>(result);
        _submissions.Verify(s => s.DeleteTissueAsync(3, TissueOwner.Submission, 9, It.IsAny<CancellationToken>()), Times.Once);
        _batches.Verify(b => b.RefreshAllTissuesAssignedAsync(42, 9, It.IsAny<CancellationToken>()), Times.Once);
    }
}
