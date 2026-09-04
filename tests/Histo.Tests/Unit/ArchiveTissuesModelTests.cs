using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Archive;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="ArchiveTissuesModel"/>.</summary>
public class ArchiveTissuesModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    public ArchiveTissuesModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
    }

    private ArchiveTissuesModel CreateSut() =>
        new(_session.Object, _submissions.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnGetAsync_NoBatchIdInSession_LeavesAnimalsEmpty()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Empty(sut.Animals);
        _submissions.Verify(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_BatchIdInSession_LoadsAnimals()
    {
        _session.Object.BatchID = 5;
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "S1" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Single(sut.Animals);
    }
}
