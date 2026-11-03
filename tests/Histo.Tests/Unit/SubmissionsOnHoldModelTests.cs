using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

public class SubmissionsOnHoldModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    public SubmissionsOnHoldModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _session.Setup(s => s.UserID).Returns(7);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private SubmissionsOnHoldModel CreateSut() =>
        new(_session.Object, _batches.Object, _submissions.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task OnGetAsync_NoUsableBatchId_RedirectsToIndex(int? sessionBatchId)
    {
        _session.Object.BatchID = sessionBatchId;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_UserOutsideBatchArea_ReturnsForbid()
    {
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(2);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, UserAreaCode = 9 });
        var sut = CreateSut();
        sut.BatchId = 5;

        var result = await sut.OnGetAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_BatchNotFound_RedirectsToIndex()
    {
        _batches.Setup(b => b.GetByIdAsync(6, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        _session.Setup(s => s.UserAreaID).Returns(0);
        var sut = CreateSut();
        sut.BatchId = 6;

        var result = await sut.OnGetAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_BlockAnimalsPresent_MergesWithMissingAnimalsAndPatchesOnHoldAndRowStamp()
    {
        byte[] stamp = [1, 2, 3];
        _session.Object.BatchID = 5;
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "A" }]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[
                new Animal { ID = 1, SenderRef = "A", OnHold = true, RowStamp = stamp },
                new Animal { ID = 2, SenderRef = "B" }]);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal([1, 2], sut.Animals.Select(a => a.ID));
        Assert.True(sut.Animals[0].OnHold);
        Assert.Same(stamp, sut.Animals[0].RowStamp);
        Assert.Equal(5, sut.Batch?.ID);
    }

    [Fact]
    public async Task OnGetAsync_NoBlockAnimals_UsesPlainAnimalList()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 3 }]);
        var sut = CreateSut();
        sut.BatchId = 5;

        await sut.OnGetAsync();

        Assert.Equal([3], sut.Animals.Select(a => a.ID));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public async Task OnPostSaveAsync_NoUsableBatchId_RedirectsToIndex(int? sessionBatchId)
    {
        _session.Object.BatchID = sessionBatchId;
        var sut = CreateSut();

        var result = await sut.OnPostSaveAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnPostSaveAsync_UserOutsideBatchArea_ReturnsForbid()
    {
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(2);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, UserAreaCode = 9 });
        var sut = CreateSut();
        sut.BatchId = 5;

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<ForbidResult>(result);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostSaveAsync_OnlyChangedAnimalsAreUpdated_ThenRedirectsBackToPage()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[
                new Animal { ID = 1, OnHold = false },   // ticked -> change
                new Animal { ID = 2, OnHold = true },    // unticked -> change
                new Animal { ID = 3, OnHold = false }]); // unchanged
        var sut = CreateSut();
        sut.BatchId = 5;
        sut.OnHoldAnimalIds = [1];

        var result = await sut.OnPostSaveAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(5, redirect.RouteValues?["batchId"]);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.Is<Animal>(a => a.ID == 1 && a.OnHold), 7, It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.Is<Animal>(a => a.ID == 2 && !a.OnHold), 7, It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.UpdateAnimalAsync(It.Is<Animal>(a => a.ID == 3), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostSaveAsync_UpdateFails_ReloadsPageWithSaveError()
    {
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, OnHold = false }]);
        _submissions.Setup(s => s.UpdateAnimalAsync(It.IsAny<Animal>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();
        sut.BatchId = 5;
        sut.OnHoldAnimalIds = [1];

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(sut.SaveError);
        Assert.NotNull(sut.Batch);
        Assert.Single(sut.Animals);
    }

    [Fact]
    public void OnPostDone_NoExplicitBatch_RedirectsToEditStatusWithSessionBatch()
    {
        _session.Object.BatchID = 5;
        var sut = CreateSut();

        var result = sut.OnPostDone();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/EditSubmissionStatus", redirect.PageName);
        Assert.Equal(5, redirect.RouteValues?["batchId"]);
    }
}
