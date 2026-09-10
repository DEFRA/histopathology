using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="SubmissionsOnHoldModel"/>.</summary>
public class SubmissionsOnHoldModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    public SubmissionsOnHoldModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.IsViewSubmissionMode, true);
        _session.Setup(s => s.UserID).Returns(99);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
    }

    private SubmissionsOnHoldModel CreateSut() =>
        new(_session.Object, _batches.Object, _submissions.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnGetAsync_BatchId_LoadsAnimals()
    {
        _session.Object.BatchID = 42;
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42, Status = BatchStatus.Received });
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "S1" }]);

        var sut = CreateSut();
        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Single(sut.Animals);
    }

    [Fact]
    public void OnPostDone_RedirectsToEditSubmissionStatus()
    {
        var sut = CreateSut();
        _session.Object.BatchID = 42;

        var result = sut.OnPostDone();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/EditSubmissionStatus", redirect.PageName);
        Assert.Equal(42, redirect.RouteValues!["batchId"]);
    }
}
