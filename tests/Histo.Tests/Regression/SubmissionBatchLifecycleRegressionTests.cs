using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
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
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the batch/submission status lifecycle — the highest-risk cross-cutting
/// workflow identified in .github/agents/histo-testing-agent.agent.md §2.4/§5 (SUBMISSION group).
///
/// Chains ReceiveBatch → EditSubmissionStatus → BatchesForDispatch → BatchesForArchiving against a
/// single shared <see cref="FakeBatchService"/> instance, so a regression in how one page's status
/// write is read back by the next list/page is caught even without a real database. Peripheral,
/// read-only lookups (<see cref="ILookupService"/>, <see cref="IUserService"/>, <see cref="IBlockService"/>)
/// are mocked since they are not the focus of this chain.
/// </summary>
public class SubmissionBatchLifecycleRegressionTests
{
    private readonly FakeBatchService _batches = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<IBlockService> _blocks = new();

    public SubmissionBatchLifecycleRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
    }

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private ReceiveBatchModel CreateReceiveBatch(int batchId)
    {
        _session.Setup(s => s.BatchID).Returns(batchId);
        var httpContext = NewHttpContext();
        return new(_session.Object, _batches, _blocks.Object, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    private EditSubmissionStatusModel CreateEditStatus(int batchId)
    {
        _session.Setup(s => s.BatchID).Returns(batchId);
        var httpContext = NewHttpContext();
        return new(_session.Object, _batches, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    private BatchesForDispatchModel CreateForDispatch()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _batches)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
        };
    }

    private BatchesForArchivingModel CreateForArchiving()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _batches)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
        };
    }

    [Fact]
    public async Task FullLifecycle_SubmittedToCompleted_VisibleInEachDownstreamListAtTheRightStage()
    {
        // A batch arrives already Submitted (as it would after Cassetted → BatchDetails create).
        var batchId = _batches.Seed(new Batch
        {
            Status = BatchStatus.Submitted,
            BatchDate = DateTime.Today.AddDays(-2),
            UserAreaCode = 5,
        });

        // 1. Receive it.
        var receive = CreateReceiveBatch(batchId);
        await receive.OnGetAsync();
        Assert.False(receive.IsReadOnly);

        receive.Status = BatchStatus.Received;
        receive.DateReceived = DateTime.Today;
        receive.TimeReceived = "AM";
        receive.ReceivedByUserId = 7;

        var receiveResult = await receive.OnPostSaveAsync();
        Assert.IsType<RedirectToPageResult>(receiveResult);
        Assert.Equal(BatchStatus.Received, _batches.Get(batchId).Status);

        // 2. Move Received → In progress via Edit submission status.
        var toInProgress = CreateEditStatus(batchId);
        await toInProgress.OnGetAsync();
        toInProgress.OriginalStatus = BatchStatus.Received;
        toInProgress.Status = BatchStatus.InProgress;

        var toInProgressResult = await toInProgress.OnPostSaveAsync();
        Assert.IsType<RedirectToPageResult>(toInProgressResult);
        Assert.Equal(BatchStatus.InProgress, _batches.Get(batchId).Status);

        // 3. Now it shows up on the dispatch (quality-data) list...
        var dispatch = CreateForDispatch();
        await dispatch.OnGetAsync();
        Assert.Contains(dispatch.PagedEntries, b => b.ID == batchId);

        // ...but not yet on the archiving list (not Completed).
        var archivingBefore = CreateForArchiving();
        await archivingBefore.OnGetAsync();
        Assert.DoesNotContain(archivingBefore.PagedEntries, b => b.ID == batchId);

        // 4. Complete it.
        var toCompleted = CreateEditStatus(batchId);
        await toCompleted.OnGetAsync();
        toCompleted.OriginalStatus = BatchStatus.InProgress;
        toCompleted.Status = BatchStatus.Completed;

        var toCompletedResult = await toCompleted.OnPostSaveAsync();
        Assert.IsType<RedirectToPageResult>(toCompletedResult);
        var completed = _batches.Get(batchId);
        Assert.Equal(BatchStatus.Completed, completed.Status);
        Assert.Equal(DateTime.Today, completed.CompletedDate);

        // 5. It leaves the dispatch list and now appears for archiving.
        var dispatchAfter = CreateForDispatch();
        await dispatchAfter.OnGetAsync();
        Assert.DoesNotContain(dispatchAfter.PagedEntries, b => b.ID == batchId);

        var archivingAfter = CreateForArchiving();
        await archivingAfter.OnGetAsync();
        Assert.Contains(archivingAfter.PagedEntries, b => b.ID == batchId);
    }

    [Fact]
    public async Task EditSubmissionStatus_DirectSubmittedToInProgress_IsRejected()
    {
        var batchId = _batches.Seed(new Batch { Status = BatchStatus.Submitted, BatchDate = DateTime.Today });

        var edit = CreateEditStatus(batchId);
        await edit.OnGetAsync();
        edit.OriginalStatus = BatchStatus.Submitted;
        edit.Status = BatchStatus.InProgress;

        var result = await edit.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The submission cannot be set to In Progress while still Submitted. Receive it first.", edit.SaveError);
        Assert.Equal(BatchStatus.Submitted, _batches.Get(batchId).Status);
    }

    [Fact]
    public async Task EditSubmissionStatus_SettingReceivedDirectly_IsRejected()
    {
        // Mirrors the legacy rule: Received must go through the dedicated Receive workflow.
        var batchId = _batches.Seed(new Batch { Status = BatchStatus.Submitted, BatchDate = DateTime.Today });

        var edit = CreateEditStatus(batchId);
        await edit.OnGetAsync();
        edit.OriginalStatus = BatchStatus.Submitted;
        edit.Status = BatchStatus.Received;

        var result = await edit.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Mark a submission as Received using the Receive Submissions workflow.", edit.SaveError);
        Assert.Equal(BatchStatus.Submitted, _batches.Get(batchId).Status);
    }

    [Fact]
    public async Task ReceiveBatch_MissingReceivedDate_IsRejectedAndStatusUnchanged()
    {
        var batchId = _batches.Seed(new Batch { Status = BatchStatus.Submitted, BatchDate = DateTime.Today });

        var receive = CreateReceiveBatch(batchId);
        await receive.OnGetAsync();
        receive.Status = BatchStatus.Received;
        receive.TimeReceived = "AM";
        receive.ReceivedByUserId = 7;
        receive.DateReceived = null; // missing — required for Received/Rejected

        var result = await receive.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter the date the submission was received or rejected.", receive.Error);
        Assert.Equal(BatchStatus.Submitted, _batches.Get(batchId).Status);
    }

    [Fact]
    public async Task ReceiveBatch_DateReceivedBeforeSubmissionDate_IsRejected()
    {
        var batchId = _batches.Seed(new Batch { Status = BatchStatus.Submitted, BatchDate = DateTime.Today });

        var receive = CreateReceiveBatch(batchId);
        await receive.OnGetAsync();
        receive.Status = BatchStatus.Received;
        receive.TimeReceived = "AM";
        receive.ReceivedByUserId = 7;
        receive.DateReceived = DateTime.Today.AddDays(-1); // before BatchDate

        var result = await receive.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("must be the same as or later than the submission date", receive.Error);
        Assert.Equal(BatchStatus.Submitted, _batches.Get(batchId).Status);
    }

    /// <summary>In-memory stand-in for the batch-header stored procedures, shared across page-model instances within a test.</summary>
    private sealed class FakeBatchService : IBatchService
    {
        private int _nextId = 1;
        private readonly Dictionary<int, Batch> _store = [];
        private readonly Dictionary<int, List<string>> _postFixationCodes = [];

        /// <summary>Test-only seeding helper — not part of <see cref="IBatchService"/>.</summary>
        public int Seed(Batch batch)
        {
            var id = _nextId++;
            _store[id] = Copy(batch, id: id, rowStamp: [1]);
            return id;
        }

        /// <summary>Test-only read helper — not part of <see cref="IBatchService"/>.</summary>
        public Batch Get(int id) => _store[id];

        public Task<Batch?> GetByIdAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult(_store.TryGetValue(batchId, out var b) ? b : null);

        public Task<IReadOnlyList<BatchListResult>> GetReceivedAsync(CancellationToken ct = default) => ListWhere(BatchStatus.Received);
        public Task<IReadOnlyList<BatchListResult>> GetInProgressAsync(CancellationToken ct = default) => ListWhere(BatchStatus.InProgress);
        public Task<IReadOnlyList<BatchListResult>> GetOnHoldAsync(CancellationToken ct = default) => ListWhere(BatchStatus.OnHold);
        public Task<IReadOnlyList<BatchListResult>> GetCompletedAsync(CancellationToken ct = default) => ListWhere(BatchStatus.Completed);
        public Task<IReadOnlyList<BatchListResult>> GetNotReceivedAsync(CancellationToken ct = default) => ListWhere(BatchStatus.Submitted);
        // "Ready for dispatch" (quality data entry) == actively being processed, matching this chain's intent.
        public Task<IReadOnlyList<BatchListResult>> GetForDispatchAsync(CancellationToken ct = default) => ListWhere(BatchStatus.InProgress);

        public Task<IReadOnlyList<BatchListResult>> GetAllBatchesAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<BatchListResult>)_store.Values.Select(ToListResult).ToList());

        public Task<int> AddAsync(Batch batch, int userId, CancellationToken ct = default) => Task.FromResult(Seed(batch));

        public Task<bool> UpdateAsync(Batch batch, int userId, CancellationToken ct = default)
        {
            if (!_store.ContainsKey(batch.ID)) throw new InvalidOperationException($"Batch {batch.ID} not found.");
            _store[batch.ID] = Copy(batch, rowStamp: [1]);
            return Task.FromResult(true);
        }

        public Task<int> CopyBatchHeaderAsync(Batch source, int userId, CancellationToken ct = default) => Task.FromResult(Seed(source));
        public Task<bool> SetByPassSortAsync(int batchId, bool byPassSort, int userId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<IReadOnlyList<BatchSearchResult>> SearchAsync(BatchSearchCriteria criteria, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchSearchResult>)new List<BatchSearchResult>());
        public Task<IReadOnlyList<TestItemRow>> GetTestItemRowsAsync(string? projectDesc, int batchType, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TestItemRow>)new List<TestItemRow>());

        public Task<IReadOnlyList<TestPremiumChargeCount>> GetTestPremiumChargeCountsAsync(string? projectDesc, int batchType, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<TestPremiumChargeCount>)new List<TestPremiumChargeCount>());

        public Task<IReadOnlyList<TestPremiumChargeBatchRef>> GetTestPremiumChargeBatchesAsync(string? projectDesc, int batchType, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<TestPremiumChargeBatchRef>)new List<TestPremiumChargeBatchRef>());

        public Task<int> FixCompletedDatesAsync(CancellationToken ct = default) => Task.FromResult(0);

        public Task<bool> SetCustomerReceivedDateAsync(int batchId, DateTime? date, byte[] rowStamp, int userId, CancellationToken ct = default)
        {
            if (!_store.TryGetValue(batchId, out var b)) return Task.FromResult(false);
            _store[batchId] = Copy(b, customerReceivedDate: date, setCustomerReceivedDate: true, rowStamp: [1]);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<int>> GetAnimalsWithUnassignedTissuesAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyCollection<int>)new List<int>());
        public Task RefreshAllTissuesAssignedAsync(int batchId, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> CompleteBlockAssignmentAsync(int batchId, bool allTissuesAssigned, int userId, CancellationToken ct = default) => Task.FromResult(true);

        public Task<bool> SetCompletedAsync(int batchId, DateTime completedDate, int userId, CancellationToken ct = default)
        {
            if (!_store.TryGetValue(batchId, out var b)) return Task.FromResult(false);
            _store[batchId] = Copy(b, status: BatchStatus.Completed, completedDate: completedDate, setCompletedDate: true, rowStamp: [1]);
            return Task.FromResult(true);
        }

        public Task<BatchTestSelections> GetBatchTestSelectionsAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult(new BatchTestSelections());

        public Task<bool> SaveBatchTestSelectionsAsync(int batchId, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, int userId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<string?> GetSubmittedAsCodeAsync(int batchId, CancellationToken ct = default) => Task.FromResult((string?)null);
        public Task SaveSubmittedAsAsync(int batchId, string code, int userId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<string>> GetPostFixationCodesAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<string>)(_postFixationCodes.TryGetValue(batchId, out var codes) ? codes : []));

        public Task<bool> SavePostFixationCodesAsync(int batchId, IReadOnlyList<string> codes, int userId, CancellationToken ct = default)
        {
            _postFixationCodes[batchId] = codes.ToList();
            return Task.FromResult(true);
        }

        private Task<IReadOnlyList<BatchListResult>> ListWhere(string status) =>
            Task.FromResult((IReadOnlyList<BatchListResult>)_store.Values.Where(b => b.Status == status).Select(ToListResult).ToList());

        private static BatchListResult ToListResult(Batch b) => new()
        {
            ID = b.ID,
            Species = b.Species,
            BatchDate = b.BatchDate,
            ReceivedDate = b.ReceivedDate,
            CompletedDate = b.CompletedDate,
            Status = b.Status,
        };

        /// <summary>
        /// <see cref="Batch"/> is a plain class (not a record), so there is no `with` expression —
        /// this reconstructs a full copy, optionally overriding the given fields.
        /// </summary>
        private static Batch Copy(
            Batch b, int? id = null, string? status = null, byte[]? rowStamp = null,
            DateTime? completedDate = null, bool setCompletedDate = false,
            DateTime? customerReceivedDate = null, bool setCustomerReceivedDate = false) => new()
        {
            ID = id ?? b.ID,
            Status = status ?? b.Status,
            CustomerRef = b.CustomerRef,
            Comments = b.Comments,
            StatusComments = b.StatusComments,
            BatchDate = b.BatchDate,
            ReceivedDate = b.ReceivedDate,
            CompletedDate = setCompletedDate ? completedDate : b.CompletedDate,
            SubmittedByUserID = b.SubmittedByUserID,
            UserAreaCode = b.UserAreaCode,
            IsPreCassetted = b.IsPreCassetted,
            ByPassSort = b.ByPassSort,
            RowStamp = rowStamp ?? b.RowStamp,
            BatchType = b.BatchType,
            ProjectContractCode = b.ProjectContractCode,
            ContactName = b.ContactName,
            Species = b.Species,
            Fixation = b.Fixation,
            CustomerReceivedDate = setCustomerReceivedDate ? customerReceivedDate : b.CustomerReceivedDate,
            SubmittedBy = b.SubmittedBy,
            SubmittedArea = b.SubmittedArea,
            OtherSubmittedBy = b.OtherSubmittedBy,
            OtherSubmittedArea = b.OtherSubmittedArea,
            SafeToHandle = b.SafeToHandle,
            AllTissuesAssigned = b.AllTissuesAssigned,
            SampleSameProjects = b.SampleSameProjects,
            IsBlocked = b.IsBlocked,
            TimeReceived = b.TimeReceived,
            ReceivedBy = b.ReceivedBy,
            PostFixationOther = b.PostFixationOther,
        };
    }
}
