using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SearchSubmissionsModel"/>'s "Print submission notes" button —
/// mirrors <c>ViewSubmissionsModelTests</c>'s equivalent coverage, since both pages inline the
/// same two print buttons previously only reachable via legacy's <c>FinalPrintBatch.aspx</c>.
/// </summary>
public class SearchSubmissionsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockTestService> _tests = new();

    public SearchSubmissionsModelTests()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[]);
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _tests.Setup(t => t.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
    }

    private SearchSubmissionsModel CreateSut() =>
        new(_session.Object, _batches.Object, _users.Object, _lookups.Object, _blocks.Object, _submissions.Object, _tests.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnPostSelectAsync_SelectedBatchId_SetsSessionBatchId()
    {
        var sut = CreateSut();
        sut.SelectedBatchId = 42;

        await sut.OnPostSelectAsync();

        _session.VerifySet(s => s.BatchID = 42, Times.Once);
    }

    [Fact]
    public async Task OnPostSelectAsync_HeaderCommentOnly_EnablesPrintSubmissionNotes()
    {
        _batches.Setup(b => b.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 7, Comments = "Header note" });
        var sut = CreateSut();
        sut.SelectedBatchId = 7;

        await sut.OnPostSelectAsync();

        Assert.True(sut.HasNotes);
    }

    [Fact]
    public async Task OnPostSelectAsync_NoCommentsAnywhere_PrintSubmissionNotesStaysDisabled()
    {
        var sut = CreateSut();
        sut.SelectedBatchId = 7;

        await sut.OnPostSelectAsync();

        Assert.False(sut.HasNotes);
    }
}
