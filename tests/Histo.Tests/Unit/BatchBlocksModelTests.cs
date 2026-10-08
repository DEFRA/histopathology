using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Covers the "Assign tissues to blocks" delete-sample journey on <see cref="BatchBlocksModel"/>.
///
/// Regression: the handler used to call <c>DeleteAnimalAsync</c>, whose stored procedure only
/// removes the shared <c>Animal</c> row. With no FK cascades in this schema, the sample's blocks,
/// block tissues, block test selections, batch submissions and submission tissues were all left
/// behind — the sample was only partially deleted.
/// </summary>
public class BatchBlocksModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockTestService> _blockTests = new();
    private readonly Mock<IUserService> _users = new();

    public BatchBlocksModelTests()
    {
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _session.Setup(s => s.UserID).Returns(7);
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Histo.Administration.Models.User>)[]);
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
    }

    private BatchBlocksModel CreateSut() =>
        new(_session.Object, _submissions.Object, _blocks.Object, _batches.Object,
            _lookups.Object, _blockTests.Object, _users.Object, Mock.Of<ILogger<BatchBlocksModel>>())
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            BatchId = 5,
        };

    [Fact]
    public async Task OnPostDeleteSampleAsync_RemovesTheWholeSampleFromTheSubmission()
    {
        _submissions.Setup(s => s.DeleteSampleFromBatchAsync(5, 42, 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateSut().OnPostDeleteSampleAsync(42);

        Assert.IsType<RedirectToPageResult>(result);
        _submissions.Verify(s => s.DeleteSampleFromBatchAsync(5, 42, 7, It.IsAny<CancellationToken>()), Times.Once);
        // Deleting the shared Animal row would orphan any other submission that references it.
        _submissions.Verify(s => s.DeleteAnimalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostDeleteSampleAsync_DeleteFails_ShowsErrorAndStaysOnPage()
    {
        _submissions.Setup(s => s.DeleteSampleFromBatchAsync(5, 42, 7, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.OnPostDeleteSampleAsync(42);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Could not delete this sample. No changes were made.", sut.ErrorMessage);
    }
}
