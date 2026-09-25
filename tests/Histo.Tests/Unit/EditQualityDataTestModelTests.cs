using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.QualityControl.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Web.Pages.QC;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="EditQualityDataTestModel"/>, focused on the field-keyed
/// <see cref="EditQualityDataTestModel.Errors"/> dictionary that backs the clickable
/// GOV.UK error summary (previously a single plain-text <c>Error</c> string with no
/// link target).
/// </summary>
public class EditQualityDataTestModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockTestService> _tests = new();
    private readonly Mock<IQCNoteService> _qc = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<IBatchService> _batches = new();

    public EditQualityDataTestModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.Object.BatchID = 42;
        _session.Setup(s => s.UserID).Returns(7);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        // Default: no tests on the batch, so the completion check is a no-op unless a test overrides it.
        _tests.Setup(t => t.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[]);
    }

    private EditQualityDataTestModel CreateSut() =>
        new(_session.Object, _tests.Object, _qc.Object, _lookups.Object, _users.Object, _batches.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    private static BlockTest MakeTest() => new()
    {
        ID = 1,
        BlockID = 1,
        TestType = BlockTestType.Histology,
        Code = "1",
        RowStamp = [1, 2, 3],
    };

    [Fact]
    public async Task OnPostAsync_FailedResultWithoutQCCode_SetsFieldKeyedError()
    {
        _tests.Setup(t => t.GetByIdAsync(42, 1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeTest());
        var sut = CreateSut();
        sut.TestId = 1;
        sut.Result = BlockTestResult.Failed;

        await sut.OnPostAsync();

        Assert.True(sut.Errors.ContainsKey("QCCode"));
        Assert.False(string.IsNullOrEmpty(sut.Errors["QCCode"]));
    }

    [Fact]
    public async Task OnPostAsync_DispatchedWithoutRequiredFields_SetsAllThreeFieldKeyedErrors()
    {
        _tests.Setup(t => t.GetByIdAsync(42, 1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeTest());
        var sut = CreateSut();
        sut.TestId = 1;
        sut.RemedialAction = "1"; // avoid the unrelated RemedialAction error for this test
        sut.Dispatched = true;

        await sut.OnPostAsync();

        Assert.True(sut.Errors.ContainsKey("DispatchedDate"));
        Assert.True(sut.Errors.ContainsKey("DispatchedBy"));
        Assert.True(sut.Errors.ContainsKey("DispatchedTo"));
    }

    [Fact]
    public async Task OnPostAsync_ArchiveLocationWithoutDate_SetsArchivedDateError()
    {
        _tests.Setup(t => t.GetByIdAsync(42, 1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeTest());
        var sut = CreateSut();
        sut.TestId = 1;
        sut.RemedialAction = "1";
        sut.ArchiveLocation = "STORE-A";

        await sut.OnPostAsync();

        Assert.True(sut.Errors.ContainsKey("ArchivedDate"));
        Assert.False(sut.Errors.ContainsKey("ArchiveLocation"));
    }

    [Fact]
    public async Task OnPostAsync_ValidSubmission_SavesAndRedirectsWithNoErrors()
    {
        _tests.Setup(t => t.GetByIdAsync(42, 1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeTest());
        var sut = CreateSut();
        sut.TestId = 1;
        sut.RemedialAction = "1";

        var result = await sut.OnPostAsync();

        Assert.Empty(sut.Errors);
        _tests.Verify(t => t.UpdateAsync(It.IsAny<BlockTest>(), 7, It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
    }

    // ── Batch completion — legacy QualityData.aspx.vb::UpdateSessionWithQualityData ──────────

    private static BlockTest DispatchedTest(int id, DateTime dispatched) => new()
    {
        ID = id,
        BlockID = 1,
        TestType = BlockTestType.Histology,
        Code = "1",
        Dispatched = true,
        DispatchedDate = dispatched,
        RowStamp = [1, 2, 3],
    };

    [Fact]
    public async Task OnPostAsync_AllTestsDispatched_CompletesBatchWithLatestDispatchDate()
    {
        _tests.Setup(t => t.GetByIdAsync(42, 1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeTest());
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)
            [
                DispatchedTest(1, new DateTime(2026, 5, 1)),
                DispatchedTest(2, new DateTime(2026, 5, 9)),
                DispatchedTest(3, new DateTime(2026, 5, 4)),
            ]);
        var sut = CreateSut();
        sut.TestId = 1;
        sut.RemedialAction = "1";

        await sut.OnPostAsync();

        _batches.Verify(b => b.SetCompletedAsync(42, new DateTime(2026, 5, 9), 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_OneTestNotDispatched_DoesNotCompleteBatch()
    {
        _tests.Setup(t => t.GetByIdAsync(42, 1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeTest());
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)
            [
                DispatchedTest(1, new DateTime(2026, 5, 1)),
                MakeTest(), // not dispatched
            ]);
        var sut = CreateSut();
        sut.TestId = 1;
        sut.RemedialAction = "1";

        await sut.OnPostAsync();

        _batches.Verify(b => b.SetCompletedAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_DispatchedFlagSetButNoDispatchDate_DoesNotCompleteBatch()
    {
        _tests.Setup(t => t.GetByIdAsync(42, 1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeTest());
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)
            [
                new BlockTest { ID = 1, BlockID = 1, TestType = BlockTestType.Histology, Code = "1", Dispatched = true, RowStamp = [1, 2, 3] },
            ]);
        var sut = CreateSut();
        sut.TestId = 1;
        sut.RemedialAction = "1";

        await sut.OnPostAsync();

        _batches.Verify(b => b.SetCompletedAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ConcurrencyException_SetsConcurrencyErrorNotFieldErrors()
    {
        _tests.Setup(t => t.GetByIdAsync(42, 1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeTest());
        _tests.Setup(t => t.UpdateAsync(It.IsAny<BlockTest>(), 7, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BlockTestConcurrencyException());
        var sut = CreateSut();
        sut.TestId = 1;
        sut.RemedialAction = "1";

        await sut.OnPostAsync();

        Assert.False(string.IsNullOrEmpty(sut.ConcurrencyError));
        Assert.Empty(sut.Errors);
    }
}
