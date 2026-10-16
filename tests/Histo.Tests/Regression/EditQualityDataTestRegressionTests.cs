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
/// Tier 2 gap closure: regression suite for Edit test (EditQualityDataTest) — the single-test
/// deep-link editor behind the QualityData worklist, which previously only had atomic Unit/
/// coverage. Backed by a stateful <see cref="FakeBlockTestService"/> so save-then-reload,
/// concurrency, and the "batch auto-completes once every test is dispatched" business rule are
/// genuinely exercised.
/// </summary>
public class EditQualityDataTestRegressionTests
{
    private readonly FakeBlockTestService _tests = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IQCNoteService> _qc = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<IBatchService> _batches = new();

    public EditQualityDataTestRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.BatchID).Returns(1);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
    }

    private EditQualityDataTestModel CreateSut(int testId)
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return new(_session.Object, _tests, _qc.Object, _lookups.Object, _users.Object, _batches.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TestId = testId,
        };
    }

    [Fact]
    public async Task OnPostAsync_FailedResultWithoutQcCode_IsRejected()
    {
        var testId = _tests.Seed(batchId: 1, result: BlockTestResult.Passed);
        var sut = CreateSut(testId);
        await sut.OnGetAsync();
        sut.Result = BlockTestResult.Failed;
        sut.QCCode = null;
        sut.RemedialAction = "RA1";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a QC code when the test result is Failed.", sut.Errors["QCCode"]);
    }

    [Fact]
    public async Task OnPostAsync_DispatchedWithoutDate_IsRejected()
    {
        var testId = _tests.Seed(batchId: 1);
        var sut = CreateSut(testId);
        await sut.OnGetAsync();
        sut.RemedialAction = "RA1";
        sut.Dispatched = true;
        sut.DispatchedDate = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a dispatched date.", sut.Errors["DispatchedDate"]);
    }

    [Fact]
    public async Task OnPostAsync_ArchiveLocationWithoutDate_IsRejected()
    {
        var testId = _tests.Seed(batchId: 1);
        var sut = CreateSut(testId);
        await sut.OnGetAsync();
        sut.RemedialAction = "RA1";
        sut.ArchiveLocation = "LOC-A";
        sut.ArchivedDate = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter an archive date when an archive location is selected.", sut.Errors["ArchivedDate"]);
    }

    [Fact]
    public async Task OnPostAsync_ValidSave_PersistsAndRedirectsToQualityData()
    {
        var testId = _tests.Seed(batchId: 1);
        var sut = CreateSut(testId);
        await sut.OnGetAsync();
        sut.RemedialAction = "RA1";
        sut.Result = BlockTestResult.Passed;
        sut.NumberOfSlides = 4;

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/QC/QualityData", redirect.PageName);
        Assert.Equal(4, _tests.Get(testId).NumberOfSlides);
    }

    [Fact]
    public async Task OnPostAsync_TickingQcNoteFirstTime_CreatesQcNoteAndStampsReference()
    {
        var testId = _tests.Seed(batchId: 1);
        _qc.Setup(q => q.AddAsync(1, 7, It.IsAny<CancellationToken>())).ReturnsAsync(55);
        var sut = CreateSut(testId);
        await sut.OnGetAsync();
        sut.RemedialAction = "RA1";
        sut.QCNote = true;

        await sut.OnPostAsync();

        Assert.Equal(55, _tests.Get(testId).QCNoteRef);
    }

    [Fact]
    public async Task OnPostAsync_AllBatchTestsDispatched_CompletesTheBatch()
    {
        var testId = _tests.Seed(batchId: 1, dispatched: true, dispatchedDate: new DateTime(2026, 1, 10));
        var sut = CreateSut(testId);
        await sut.OnGetAsync();
        sut.RemedialAction = "RA1";
        sut.Dispatched = true;
        sut.DispatchedDate = new DateTime(2026, 2, 1);
        sut.DispatchedBy = "Alice";
        sut.DispatchedTo = "Lab";

        await sut.OnPostAsync();

        _batches.Verify(b => b.SetCompletedAsync(1, new DateTime(2026, 2, 1), 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ConcurrentModification_ShowsConcurrencyError()
    {
        var testId = _tests.Seed(batchId: 1);
        _tests.ThrowConcurrencyOnNextUpdate = true;
        var sut = CreateSut(testId);
        await sut.OnGetAsync();
        sut.RemedialAction = "RA1";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Another user has modified this test. Please reload and try again.", sut.ConcurrencyError);
    }

    /// <summary>In-memory stand-in for the subset of block-test stored procedures EditQualityDataTestModel calls.</summary>
    private sealed class FakeBlockTestService : IBlockTestService
    {
        private int _nextId = 1;
        private readonly Dictionary<int, BlockTest> _store = [];
        private readonly Dictionary<int, int> _batchByTestId = [];

        public bool ThrowConcurrencyOnNextUpdate { get; set; }

        public int Seed(int batchId, string? result = null, bool dispatched = false, DateTime? dispatchedDate = null)
        {
            var id = _nextId++;
            _store[id] = new BlockTest { ID = id, Result = result, Dispatched = dispatched, DispatchedDate = dispatchedDate, RowStamp = [1] };
            _batchByTestId[id] = batchId;
            return id;
        }

        public BlockTest Get(int id) => _store[id];

        public Task<BlockTest?> GetByIdAsync(int batchId, int testId, CancellationToken ct = default) =>
            Task.FromResult(_store.TryGetValue(testId, out var t) && _batchByTestId[testId] == batchId ? t : null);

        public Task<IReadOnlyList<BlockTest>> GetByBatchAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<BlockTest>)_store.Values.Where(t => _batchByTestId[t.ID] == batchId).ToList());

        public Task UpdateAsync(BlockTest test, int userId, CancellationToken ct = default)
        {
            if (ThrowConcurrencyOnNextUpdate)
            {
                ThrowConcurrencyOnNextUpdate = false;
                throw new BlockTestConcurrencyException();
            }
            _store[test.ID] = test;
            return Task.CompletedTask;
        }

        public Task SaveTCCodesAsync(int batchId, int testId, string testType, IReadOnlyList<TcCode> existing, IReadOnlyList<string> selected, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveTestSelectionsAsync(int batchId, int blockId, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<BlockTest>> GetAllSelectionsByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BlockTest>)new List<BlockTest>());
    }
}
