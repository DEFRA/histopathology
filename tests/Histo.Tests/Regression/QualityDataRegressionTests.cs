using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.QualityControl.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.QC;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Tier 2 gap closure: regression suite for the Quality data worklist (QualityData) — the
/// batch-wide bulk-edit grid behind <see cref="EditQualityDataTestModel"/>, which previously
/// only had atomic Unit/ coverage. Backed by a stateful <see cref="FakeBlockTestService"/> so
/// single-vs-bulk save semantics (blank = clear vs blank = leave unchanged) and the batch
/// access check are genuinely exercised.
/// </summary>
public class QualityDataRegressionTests
{
    private readonly FakeBlockTestService _tests = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<IQCNoteService> _qc = new();

    public QualityDataRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _session.Setup(s => s.BatchID).Returns(1);
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 1 });
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
    }

    private QualityDataModel CreateSut()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return new(_session.Object, _tests, _batches.Object, _lookups.Object, _users.Object, _qc.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
        };
    }

    [Fact]
    public async Task OnGetAsync_NoBatchInSession_RedirectsToIndex()
    {
        _session.Setup(s => s.BatchID).Returns((int?)null);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_LoadsTestsForTheBatch()
    {
        _tests.Seed(1, "H&E", "ABC/1");
        _tests.Seed(1, "H&E", "ABC/2");
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(2, sut.Tests.Count);
    }

    [Fact]
    public async Task OnGetAsync_ByUrlBatchId_OtherAreaUser_IsForbidden()
    {
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.IsMaintenance).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(99);
        _batches.Setup(b => b.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 2, UserAreaCode = 1 });
        var sut = CreateSut();
        sut.BatchId = 2;

        var result = await sut.OnGetAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostUpdateAsync_NoRowsSelected_IsRejected()
    {
        _tests.Seed(1, "H&E", "ABC/1");
        var sut = CreateSut();
        await sut.OnGetAsync();
        sut.SelectedIds = [];

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select at least one test to update.", sut.Error);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SingleRow_BlankResultClearsIt()
    {
        var testId = _tests.Seed(1, "H&E", "ABC/1", result: BlockTestResult.Passed);
        var sut = CreateSut();
        await sut.OnGetAsync();
        sut.SelectedIds = [testId];
        sut.Result = null; // single-row save: blank legitimately means "clear"
        sut.NumberOfSlides = 4; // mandatory on a single-row save

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Null(_tests.Get(testId).Result);
        Assert.Equal("Updated 1 test(s).", sut.SuccessMessage);
    }

    [Fact]
    public async Task OnPostUpdateAsync_BulkRows_BlankCommentLeavesExistingValuesUnchanged()
    {
        var id1 = _tests.Seed(1, "H&E", "ABC/1", comment: "Keep me");
        var id2 = _tests.Seed(1, "H&E", "ABC/2", comment: "Keep me too");
        var sut = CreateSut();
        await sut.OnGetAsync();
        sut.SelectedIds = [id1, id2];
        sut.NumberOfSlides = 3; // satisfies "at least one field entered" for bulk mode
        sut.Comment = null;

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Keep me", _tests.Get(id1).Comment);
        Assert.Equal(3, _tests.Get(id1).NumberOfSlides);
        Assert.Equal("Keep me too", _tests.Get(id2).Comment);
    }

    [Fact]
    public async Task OnPostUpdateAsync_BulkRows_NoFieldEntered_IsRejected()
    {
        var id1 = _tests.Seed(1, "H&E", "ABC/1");
        var id2 = _tests.Seed(1, "H&E", "ABC/2");
        var sut = CreateSut();
        await sut.OnGetAsync();
        sut.SelectedIds = [id1, id2];

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter at least one field to apply to the selected tests.", sut.Error);
    }

    [Fact]
    public async Task OnPostUpdateAsync_ConcurrentModification_ReportsCountAndSkipsCompletion()
    {
        var testId = _tests.Seed(1, "H&E", "ABC/1");
        _tests.ThrowConcurrencyFor.Add(testId);
        var sut = CreateSut();
        await sut.OnGetAsync();
        sut.SelectedIds = [testId];
        sut.NumberOfSlides = 2;

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("1 test(s) were modified by another user and were not updated. Please reload and try again.", sut.Error);
        Assert.Null(sut.SuccessMessage);
    }

    /// <summary>In-memory stand-in for the subset of block-test stored procedures QualityDataModel calls.</summary>
    private sealed class FakeBlockTestService : IBlockTestService
    {
        private int _nextId = 1;
        private readonly Dictionary<int, BlockTest> _store = [];
        private readonly Dictionary<int, int> _batchByTestId = [];

        public HashSet<int> ThrowConcurrencyFor { get; } = [];

        public int Seed(int batchId, string testType, string histologyRef, string? result = null, string? comment = null)
        {
            var id = _nextId++;
            _store[id] = new BlockTest { ID = id, TestType = testType, HistologyRef = histologyRef, Result = result, Comment = comment, RowStamp = [1] };
            _batchByTestId[id] = batchId;
            return id;
        }

        public BlockTest Get(int id) => _store[id];

        public Task<IReadOnlyList<BlockTest>> GetByBatchAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<BlockTest>)_store.Values.Where(t => _batchByTestId[t.ID] == batchId).ToList());

        public Task<IReadOnlyList<BlockTest>> GetAllSelectionsByBatchAsync(int batchId, CancellationToken ct = default) => GetByBatchAsync(batchId, ct);

        public Task<BlockTest?> GetByIdAsync(int batchId, int testId, CancellationToken ct = default) =>
            Task.FromResult(_store.TryGetValue(testId, out var t) ? t : null);

        public Task UpdateAsync(BlockTest test, int userId, CancellationToken ct = default)
        {
            if (ThrowConcurrencyFor.Remove(test.ID)) throw new BlockTestConcurrencyException();
            _store[test.ID] = test;
            return Task.CompletedTask;
        }

        public Task SaveTCCodesAsync(int batchId, int testId, string testType, IReadOnlyList<TcCode> existing, IReadOnlyList<string> selected, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveTestSelectionsAsync(int batchId, int blockId, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, int userId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
