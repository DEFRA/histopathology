using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
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
/// Tier 2 gap closure: regression suite for Edit submission (EditBatch) — the main batch-header
/// edit page, which previously only had atomic Unit/ coverage. Backed by a stateful
/// <see cref="FakeBatchService"/> so save-then-reload and the "Rejected reverts to Submitted"
/// business rule are genuinely exercised.
/// </summary>
public class EditBatchRegressionTests
{
    private readonly FakeBatchService _batches = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public EditBatchRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = HistologyCode.HAndE, Name = "H&E" },
        ]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
    }

    private EditBatchModel CreateSut(int batchId)
    {
        _session.Setup(s => s.BatchID).Returns(batchId);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return new(_session.Object, _batches, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    [Fact]
    public async Task OnGetAsync_NoBatchInSession_RedirectsToIndex()
    {
        var sut = CreateSut(batchId: 0);

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_PrePopulatesEditableFieldsFromTheBatch()
    {
        var batchId = _batches.Seed(new Batch { Comments = "Existing comment", Fixation = "F1" });
        var sut = CreateSut(batchId);

        await sut.OnGetAsync();

        Assert.Equal("Existing comment", sut.Comments);
        Assert.Equal("F1", sut.Fixation);
    }

    [Fact]
    public async Task OnPostAsync_SafeToHandleNotSelected_IsRejected()
    {
        var batchId = _batches.Seed(new Batch());
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select whether the submission is adequately fixed.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_NoHistologySelected_IsRejected()
    {
        var batchId = _batches.Seed(new Batch());
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();
        sut.SafeToHandle = true;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select at least one histology type.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_InvalidDateFormat_IsRejected()
    {
        var batchId = _batches.Seed(new Batch());
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = true;
        sut.BatchDateStr = "not-a-date";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a valid date of submission in DD/MM/YYYY format.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_RejectedBatch_RevertsStatusToSubmittedOnSave()
    {
        var batchId = _batches.Seed(new Batch { Status = BatchStatus.Rejected });
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = true;

        var result = await sut.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(BatchStatus.Submitted, _batches.Get(batchId).Status);
    }

    [Fact]
    public async Task OnPostAsync_CraftedBatchTypeChange_IsIgnored_OriginalBatchTypePersisted()
    {
        var batchId = _batches.Seed(new Batch { BatchType = BatchTypeConstants.Tse });
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = true;
        sut.BatchTypeField = BatchTypeConstants.NonTse; // crafted — submission category is fixed at creation

        await sut.OnPostAsync();

        Assert.Equal(BatchTypeConstants.Tse, _batches.Get(batchId).BatchType);
    }

    [Fact]
    public async Task OnPostAsync_ValidSave_PersistsCommentsAndRedirectsToReturnPage()
    {
        var batchId = _batches.Seed(new Batch());
        _session.Setup(s => s.ReturnPage).Returns("/Batches/BatchesForEditing");
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = true;
        sut.Comments = "Updated comment";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesForEditing", redirect.PageName);
        Assert.Equal("Updated comment", _batches.Get(batchId).Comments);
    }

    /// <summary>In-memory stand-in for the subset of batch stored procedures EditBatchModel calls.</summary>
    private sealed class FakeBatchService : IBatchService
    {
        private int _nextId = 1;
        private readonly Dictionary<int, Batch> _store = [];
        private readonly Dictionary<int, BatchTestSelections> _selections = [];

        public int Seed(Batch batch)
        {
            var id = _nextId++;
            _store[id] = new Batch
            {
                ID = id,
                Status = batch.Status,
                Comments = batch.Comments,
                Fixation = batch.Fixation,
                BatchType = batch.BatchType,
                RowStamp = [1],
            };
            return id;
        }

        public Batch Get(int id) => _store[id];

        public Task<Batch?> GetByIdAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult(_store.TryGetValue(batchId, out var b) ? b : null);

        public Task<bool> UpdateAsync(Batch batch, int userId, CancellationToken ct = default)
        {
            if (!_store.ContainsKey(batch.ID)) throw new InvalidOperationException($"Batch {batch.ID} not found.");
            _store[batch.ID] = batch;
            return Task.FromResult(true);
        }

        public Task<BatchTestSelections> GetBatchTestSelectionsAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult(_selections.TryGetValue(batchId, out var s) ? s : new BatchTestSelections());

        public Task<bool> SaveBatchTestSelectionsAsync(int batchId, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, int userId, CancellationToken ct = default)
        {
            _selections[batchId] = new BatchTestSelections
            {
                Histology = histologyCodes.Select(c => new BatchTestSelectionRow { Code = c }).ToList(),
                Antibodies = antibodyCodes.Select(c => new BatchTestSelectionRow { Code = c }).ToList(),
                Stains = stainCodes.Select(c => new BatchTestSelectionRow { Code = c }).ToList(),
            };
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<BatchListResult>> GetReceivedAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetInProgressAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetOnHoldAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetCompletedAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetNotReceivedAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetAllBatchesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetForDispatchAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<int> AddAsync(Batch batch, int userId, CancellationToken ct = default) => Task.FromResult(Seed(batch));
        public Task<int> CopyBatchHeaderAsync(Batch source, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> SetByPassSortAsync(int batchId, bool byPassSort, int userId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<IReadOnlyList<BatchSearchResult>> SearchAsync(BatchSearchCriteria criteria, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchSearchResult>)new List<BatchSearchResult>());
        public Task<IReadOnlyList<TestItemRow>> GetTestItemRowsAsync(string? projectDesc, int batchType, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TestItemRow>)new List<TestItemRow>());
        public Task<IReadOnlyList<TestPremiumChargeCount>> GetTestPremiumChargeCountsAsync(string? projectDesc, int batchType, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TestPremiumChargeCount>)new List<TestPremiumChargeCount>());
        public Task<IReadOnlyList<TestPremiumChargeBatchRef>> GetTestPremiumChargeBatchesAsync(string? projectDesc, int batchType, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TestPremiumChargeBatchRef>)new List<TestPremiumChargeBatchRef>());
        public Task<int> FixCompletedDatesAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> SetCustomerReceivedDateAsync(int batchId, DateTime? date, byte[] rowStamp, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyCollection<int>> GetAnimalsWithUnassignedTissuesAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyCollection<int>)new List<int>());
        public Task RefreshAllTissuesAssignedAsync(int batchId, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> CompleteBlockAssignmentAsync(int batchId, bool allTissuesAssigned, int userId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> SetCompletedAsync(int batchId, DateTime completedDate, int userId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<string?> GetSubmittedAsCodeAsync(int batchId, CancellationToken ct = default) => Task.FromResult((string?)null);
        public Task SaveSubmittedAsAsync(int batchId, string code, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> GetPostFixationCodesAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<string>)new List<string>());
        public Task<bool> SavePostFixationCodesAsync(int batchId, IReadOnlyList<string> codes, int userId, CancellationToken ct = default) => Task.FromResult(true);
    }
}
