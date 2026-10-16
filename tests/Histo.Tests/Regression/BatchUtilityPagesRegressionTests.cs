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
/// Regression suite for three small Batches utility pages that previously had zero test
/// coverage of any kind (Tier 1 gaps): BatchesNotReceived (the entry list for Receive Submission),
/// DateReturned (records the customer-received date on a Completed batch), and SubmissionsOnHold
/// (toggles the On Hold flag per sample).
/// </summary>
public class BatchUtilityPagesRegressionTests
{
    private readonly FakeBatchService _batches = new();
    private readonly FakeSubmissionService _submissions = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public BatchUtilityPagesRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.IsHistoUser).Returns(true); // bypasses area-restricted access checks
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private BatchesNotReceivedModel CreateNotReceived() => new(_session.Object, _batches)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private DateReturnedModel CreateDateReturned() => new(_session.Object, _batches, _lookups.Object)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private SubmissionsOnHoldModel CreateOnHold(int? batchId)
    {
        _session.Setup(s => s.BatchID).Returns(batchId);
        return new(_session.Object, _batches, _submissions)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
            BatchId = batchId,
        };
    }

    // ── BatchesNotReceived ───────────────────────────────────────────────────────

    [Fact]
    public async Task BatchesNotReceived_ListsOnlySubmittedBatches()
    {
        _batches.Seed(new Batch { Status = BatchStatus.Submitted });
        _batches.Seed(new Batch { Status = BatchStatus.Completed }); // must not appear
        var sut = CreateNotReceived();

        await sut.OnGetAsync();

        var row = Assert.Single(sut.PagedEntries);
        Assert.Equal(BatchStatus.Submitted, row.Status);
    }

    [Fact]
    public async Task BatchesNotReceived_Receive_SetsSessionBatchIdAndRedirectsToReceiveBatch()
    {
        var sut = CreateNotReceived();

        var result = await sut.OnPostReceiveAsync(42);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/ReceiveBatch", redirect.PageName);
        _session.VerifySet(s => s.BatchID = 42, Times.Once);
    }

    // ── DateReturned ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DateReturned_NoBatchSelected_RedirectsToViewSubmissions()
    {
        _session.Setup(s => s.BatchID).Returns((int?)null);
        var sut = CreateDateReturned();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/ViewSubmissions", redirect.PageName);
    }

    [Fact]
    public async Task DateReturned_BatchNotCompleted_ShowsErrorInsteadOfTheForm()
    {
        var batchId = _batches.Seed(new Batch { Status = BatchStatus.InProgress });
        _session.Setup(s => s.BatchID).Returns(batchId);
        var sut = CreateDateReturned();

        await sut.OnGetAsync();

        Assert.Equal("The date returned can only be recorded for a completed submission.", sut.Error);
    }

    [Fact]
    public async Task DateReturned_FutureDate_IsRejected()
    {
        var batchId = _batches.Seed(new Batch { Status = BatchStatus.Completed });
        _session.Setup(s => s.BatchID).Returns(batchId);
        var sut = CreateDateReturned();
        sut.CustomerReceivedDate = DateTime.Today.AddDays(1);

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The date returned cannot be in the future.", sut.Error);
    }

    [Fact]
    public async Task DateReturned_ValidDate_SavesAndRedirectsToViewSubmissions()
    {
        var batchId = _batches.Seed(new Batch { Status = BatchStatus.Completed });
        _session.Setup(s => s.BatchID).Returns(batchId);
        var sut = CreateDateReturned();
        sut.CustomerReceivedDate = DateTime.Today;

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/ViewSubmissions", redirect.PageName);
        Assert.Equal(DateTime.Today, _batches.Get(batchId).CustomerReceivedDate);
    }

    // ── SubmissionsOnHold ────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmissionsOnHold_SaveTogglesOnHoldFlagsPerSample()
    {
        var batchId = _batches.Seed(new Batch());
        var keepOffId = _submissions.SeedAnimal(batchId, onHold: false);
        var turnOnId = _submissions.SeedAnimal(batchId, onHold: false);
        var turnOffId = _submissions.SeedAnimal(batchId, onHold: true);
        var sut = CreateOnHold(batchId);
        await sut.OnGetAsync();
        Assert.Equal(3, sut.Animals.Count);

        sut.OnHoldAnimalIds = [turnOnId]; // only this one should end up checked

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.False(_submissions.GetAnimal(keepOffId).OnHold);
        Assert.True(_submissions.GetAnimal(turnOnId).OnHold);
        Assert.False(_submissions.GetAnimal(turnOffId).OnHold);
    }

    [Fact]
    public async Task SubmissionsOnHold_NoBatchSelected_RedirectsToIndex()
    {
        var sut = CreateOnHold(batchId: null);

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    /// <summary>In-memory stand-in for the subset of batch stored procedures these three pages call.</summary>
    private sealed class FakeBatchService : IBatchService
    {
        private int _nextId = 1;
        private readonly Dictionary<int, Batch> _store = [];

        public int Seed(Batch batch)
        {
            var id = _nextId++;
            _store[id] = new Batch { ID = id, Status = batch.Status, RowStamp = [1] };
            return id;
        }

        public Batch Get(int id) => _store[id];

        public Task<Batch?> GetByIdAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult(_store.TryGetValue(batchId, out var b) ? b : null);

        public Task<IReadOnlyList<BatchListResult>> GetNotReceivedAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<BatchListResult>)_store.Values.Where(b => b.Status == BatchStatus.Submitted)
                .Select(b => new BatchListResult { ID = b.ID, Status = b.Status }).ToList());

        public Task<bool> SetCustomerReceivedDateAsync(int batchId, DateTime? date, byte[] rowStamp, int userId, CancellationToken ct = default)
        {
            if (!_store.TryGetValue(batchId, out var b)) return Task.FromResult(false);
            _store[batchId] = new Batch { ID = b.ID, Status = b.Status, CustomerReceivedDate = date, RowStamp = [1] };
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<BatchListResult>> GetReceivedAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetInProgressAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetOnHoldAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetCompletedAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetAllBatchesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<IReadOnlyList<BatchListResult>> GetForDispatchAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchListResult>)new List<BatchListResult>());
        public Task<int> AddAsync(Batch batch, int userId, CancellationToken ct = default) => Task.FromResult(Seed(batch));
        public Task<bool> UpdateAsync(Batch batch, int userId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<int> CopyBatchHeaderAsync(Batch source, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> SetByPassSortAsync(int batchId, bool byPassSort, int userId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<IReadOnlyList<BatchSearchResult>> SearchAsync(BatchSearchCriteria criteria, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchSearchResult>)new List<BatchSearchResult>());
        public Task<IReadOnlyList<TestItemRow>> GetTestItemRowsAsync(string? projectDesc, int batchType, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TestItemRow>)new List<TestItemRow>());
        public Task<IReadOnlyList<TestPremiumChargeCount>> GetTestPremiumChargeCountsAsync(string? projectDesc, int batchType, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TestPremiumChargeCount>)new List<TestPremiumChargeCount>());
        public Task<IReadOnlyList<TestPremiumChargeBatchRef>> GetTestPremiumChargeBatchesAsync(string? projectDesc, int batchType, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TestPremiumChargeBatchRef>)new List<TestPremiumChargeBatchRef>());
        public Task<int> FixCompletedDatesAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task<IReadOnlyCollection<int>> GetAnimalsWithUnassignedTissuesAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyCollection<int>)new List<int>());
        public Task RefreshAllTissuesAssignedAsync(int batchId, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> CompleteBlockAssignmentAsync(int batchId, bool allTissuesAssigned, int userId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> SetCompletedAsync(int batchId, DateTime completedDate, int userId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<BatchTestSelections> GetBatchTestSelectionsAsync(int batchId, CancellationToken ct = default) => Task.FromResult(new BatchTestSelections());
        public Task<bool> SaveBatchTestSelectionsAsync(int batchId, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, int userId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<string?> GetSubmittedAsCodeAsync(int batchId, CancellationToken ct = default) => Task.FromResult((string?)null);
        public Task SaveSubmittedAsAsync(int batchId, string code, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> GetPostFixationCodesAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<string>)new List<string>());
        public Task<bool> SavePostFixationCodesAsync(int batchId, IReadOnlyList<string> codes, int userId, CancellationToken ct = default) => Task.FromResult(true);
    }

    /// <summary>In-memory stand-in for the subset of submission/animal stored procedures SubmissionsOnHoldModel calls.</summary>
    private sealed class FakeSubmissionService : ISubmissionService
    {
        private int _nextId = 1;
        private readonly Dictionary<int, Animal> _animals = [];
        private readonly Dictionary<int, int> _batchByAnimal = [];

        public int SeedAnimal(int batchId, bool onHold)
        {
            var id = _nextId++;
            _animals[id] = new Animal { ID = id, OnHold = onHold };
            _batchByAnimal[id] = batchId;
            return id;
        }

        public Animal GetAnimal(int id) => _animals[id];

        public Task<IReadOnlyList<Animal>> GetBlockAnimalsByBatchAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<Animal>)new List<Animal>());

        public Task<IReadOnlyList<Animal>> GetAnimalsByBatchAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<Animal>)_animals.Values.Where(a => _batchByAnimal[a.ID] == batchId).ToList());

        public Task<bool> UpdateAnimalAsync(Animal animal, int userId, CancellationToken ct = default)
        {
            if (!_animals.ContainsKey(animal.ID)) return Task.FromResult(false);
            _animals[animal.ID] = animal;
            return Task.FromResult(true);
        }

        // ── Unused by SubmissionsOnHoldModel — stubs only ───────────────────────────
        public Task<IReadOnlyList<BatchSubmission>> GetSubmissionsByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchSubmission>)new List<BatchSubmission>());
        public Task<int> AddSubmissionAsync(BatchSubmission submission, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> CreateMouseRangeAsync(int batchId, int? sourceAnimalId, string mouseNumberFrom, string mouseNumberTo, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> CreateBatchWithCopiedSamplesAsync(Batch batch, SelectedTestCodes testCodes, string? submittedAsCode, IReadOnlyList<CopiedSamplePlan> plan, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CopySubmissionAsync(BatchSubmission source, int newBatchId, int userId, int animalId = 0, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> AddAnimalAsync(int batchSubmissionId, string senderRef, int userId, string? pmDate = null, bool pmDateSet = false, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CopyAnimalAsync(Animal source, int newBatchSubmissionId, string newSenderRef, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> DeleteAnimalAsync(int animalId, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> DeleteSampleFromBatchAsync(int batchId, int animalId, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> AdvanceNextBlockRefAsync(int animalId, string nextBlockRef, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyList<SenderSearchResult>> GetAnimalBySenderAsync(string senderRef, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<SenderSearchResult>)new List<SenderSearchResult>());
        public Task<IReadOnlyList<string>> GetExistingSenderRefsAsync(IEnumerable<string> senderRefs, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<string>)new List<string>());
        public Task UpdateAnimalSenderRefAsync(string senderRef, string newSenderRef, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAnimalHistologyRefAsync(string senderRef, string? newHistologyRef, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Tissue>> GetTissuesBySubmissionAsync(int batchId, int submissionId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Tissue>)new List<Tissue>());
        public Task<IReadOnlyList<Tissue>> GetBatchSubmissionTissuesAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Tissue>)new List<Tissue>());
        public Task<IReadOnlyList<Tissue>> GetTissuesByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Tissue>)new List<Tissue>());
        public Task<IReadOnlyList<Tissue>> GetTissuesByBlockAsync(int batchId, int blockId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Tissue>)new List<Tissue>());
        public Task<int> AddTissueAsync(Tissue tissue, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CopyTissueAsync(Tissue source, int newOwnerId, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> UpdateTissueAsync(Tissue tissue, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> DeleteTissueAsync(int tissueId, TissueOwner owner, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyList<PmDateSearchResult>> GetByPmDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<PmDateSearchResult>)new List<PmDateSearchResult>());
        public Task<IReadOnlyList<SenderSearchResult>> GetAnimalsBySenderRefAsync(string senderRef, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<SenderSearchResult>)new List<SenderSearchResult>());
        public Task<IReadOnlyList<TissueArchiveInfo>> GetTissueArchiveAsync(string? senderRef, string? histologyRef, string? archiveLocation, string? tissueCode, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TissueArchiveInfo>)new List<TissueArchiveInfo>());
        public Task<IReadOnlyList<ImportedDataRow>> GetImportedDataAsync(string? selectedTable, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<ImportedDataRow>)new List<ImportedDataRow>());
        public Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalTissuesAsync(string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AnimalTissueSearchResult>)new List<AnimalTissueSearchResult>());
        public Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalBlockTissuesAsync(string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AnimalTissueSearchResult>)new List<AnimalTissueSearchResult>());
    }
}
