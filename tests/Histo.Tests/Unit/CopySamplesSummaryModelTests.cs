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

public class CopySamplesSummaryModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    public CopySamplesSummaryModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 100, SenderRef = "SRC" }]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[
                new Animal { ID = 21, SenderRef = "B" },
                new Animal { ID = 20, SenderRef = "A" },
                new Animal { ID = 22, SenderRef = "C" }]);
    }

    private CopySamplesSummaryModel CreateSut() =>
        new(_session.Object, _batches.Object, _submissions.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnGetAsync_TargetIdsAndSessionBatch_ListsTargetsOrderedBySenderRef()
    {
        _session.Object.BatchID = 5;
        var sut = CreateSut();

        var result = await sut.OnGetAsync(9, 100, "21,x,20,0,", 3);

        Assert.IsType<PageResult>(result);
        Assert.Equal("SRC", sut.SourceAnimal?.SenderRef);
        Assert.Equal(9, sut.SourceBatchId);
        Assert.Equal(3, sut.BlocksCopiedCount);
        Assert.Equal([20, 21], sut.TargetAnimals.Select(a => a.ID));
    }

    [Theory]
    [InlineData(null, 5)]
    [InlineData("", 5)]
    [InlineData("abc,0", 5)]
    [InlineData("21", null)]
    public async Task OnGetAsync_NoTargetsOrNoSessionBatch_LeavesTargetsEmpty(string? targetIds, int? sessionBatch)
    {
        _session.Object.BatchID = sessionBatch;
        var sut = CreateSut();

        var result = await sut.OnGetAsync(9, 999, targetIds, 0);

        Assert.IsType<PageResult>(result);
        Assert.Null(sut.SourceAnimal);
        Assert.Empty(sut.TargetAnimals);
    }

    [Fact]
    public async Task OnGetAsync_UserOutsideCurrentBatchArea_ReturnsForbid()
    {
        _session.Object.BatchID = 5;
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(2);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, UserAreaCode = 9 });
        var sut = CreateSut();

        var result = await sut.OnGetAsync(9, 100, "20", 1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void OnPostDone_RedirectsToBatchBlocks()
    {
        var result = CreateSut().OnPostDone();

        Assert.Equal("/Batches/BatchBlocks", Assert.IsType<RedirectToPageResult>(result).PageName);
    }
}
