using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Archive;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Tier 2 gap closure: regression suite for Archive tissues — the Wet Tissue twin of
/// <see cref="Histo.Web.Pages.Archive.ArchiveBlocksModel"/> (see ArchiveRegressionTests.cs),
/// which previously only had atomic Unit/ coverage. Backed by a stateful
/// <see cref="FakeTissueArchiveService"/> for the part under test (tissue archive fields).
/// </summary>
public class ArchiveTissuesRegressionTests
{
    private readonly FakeTissueArchiveService _submissions = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public ArchiveTissuesRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 1, ReceivedDate = new DateTime(2026, 1, 1) });
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
    }

    private ArchiveTissuesModel CreateSut(int batchId)
    {
        _session.Setup(s => s.BatchID).Returns(batchId);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return new(_session.Object, _submissions, _batches.Object, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
        };
    }

    [Fact]
    public async Task UpdateSingleTissue_SetsLocationDateAndComment()
    {
        var batchId = 1;
        var tissueId = _submissions.Seed(batchId, "AGAR");
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();

        sut.SelectedIds = [tissueId];
        sut.ArchiveLocationCode = "STORE-A";
        sut.ArchivedDate = new DateTime(2026, 2, 1);
        sut.Comment = "Boxed up";

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Archive information updated for 1 tissue(s).", sut.SuccessMessage);
        var tissue = _submissions.Get(tissueId);
        Assert.Equal("STORE-A", tissue.ArchiveLocation);
        Assert.Equal(new DateTime(2026, 2, 1), tissue.ArchivedDate);
        Assert.Equal("Boxed up", tissue.ArchiveComment);
    }

    [Fact]
    public async Task BulkUpdate_BlankFieldsLeaveExistingValuesUnchanged()
    {
        var batchId = 2;
        var t1 = _submissions.Seed(batchId, "AGAR", archiveLocation: "OLD-LOC", archiveComment: "Keep me");
        var t2 = _submissions.Seed(batchId, "OTHER", archiveLocation: "OLD-LOC-2", archiveComment: "Keep me too");
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();

        sut.SelectedIds = [t1, t2];
        sut.ArchivedDate = new DateTime(2026, 3, 1);

        await sut.OnPostUpdateAsync();

        Assert.Equal("OLD-LOC", _submissions.Get(t1).ArchiveLocation);
        Assert.Equal("Keep me", _submissions.Get(t1).ArchiveComment);
        Assert.Equal(new DateTime(2026, 3, 1), _submissions.Get(t1).ArchivedDate);
        Assert.Equal("OLD-LOC-2", _submissions.Get(t2).ArchiveLocation);
    }

    [Fact]
    public async Task NoRowsSelected_IsRejected()
    {
        var batchId = 3;
        _submissions.Seed(batchId, "AGAR");
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();
        sut.ArchiveLocationCode = "STORE-A";

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select at least one tissue to update.", sut.Error);
    }

    [Fact]
    public async Task DateBeforeSubmissionReceivedDate_IsRejected()
    {
        var batchId = 4;
        var tissueId = _submissions.Seed(batchId, "AGAR");
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();
        sut.SelectedIds = [tissueId];
        sut.ArchivedDate = new DateTime(2025, 12, 1);

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("must be the same or later than the Submission received date", sut.Error);
        Assert.Null(_submissions.Get(tissueId).ArchivedDate);
    }

    [Fact]
    public async Task DateInTheFuture_IsRejected()
    {
        var batchId = 5;
        var tissueId = _submissions.Seed(batchId, "AGAR");
        var sut = CreateSut(batchId);
        await sut.OnGetAsync();
        sut.SelectedIds = [tissueId];
        sut.ArchivedDate = DateTime.Today.AddDays(1);

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Archive date must be today or earlier.", sut.Error);
    }

    /// <summary>In-memory stand-in for the subset of submission/tissue stored procedures ArchiveTissuesModel calls.</summary>
    private sealed class FakeTissueArchiveService : ISubmissionService
    {
        private int _nextId = 1;
        private readonly Dictionary<int, Tissue> _store = [];
        private readonly Dictionary<int, int> _batchByTissue = [];

        public int Seed(int batchId, string code, string? archiveLocation = null, string? archiveComment = null)
        {
            var id = _nextId++;
            _store[id] = new Tissue { ID = id, OwnerID = 100, Owner = TissueOwner.Submission, TissueCode = code, ArchiveLocation = archiveLocation, ArchiveComment = archiveComment, RowStamp = [1] };
            _batchByTissue[id] = batchId;
            return id;
        }

        public Tissue Get(int id) => _store[id];

        public Task<IReadOnlyList<Tissue>> GetBatchSubmissionTissuesAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<Tissue>)_store.Values.Where(t => _batchByTissue[t.ID] == batchId).ToList());

        public Task<bool> UpdateTissueAsync(Tissue tissue, int userId, CancellationToken ct = default)
        {
            if (!_store.ContainsKey(tissue.ID)) return Task.FromResult(false);
            _store[tissue.ID] = tissue;
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Animal>> GetBlockAnimalsByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Animal>)new List<Animal>());

        // ── Unused by ArchiveTissuesModel — stubs only ──────────────────────────────
        public Task<IReadOnlyList<BatchSubmission>> GetSubmissionsByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchSubmission>)new List<BatchSubmission>());
        public Task<int> AddSubmissionAsync(BatchSubmission submission, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> CreateMouseRangeAsync(int batchId, int? sourceAnimalId, string mouseNumberFrom, string mouseNumberTo, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> CreateBatchWithCopiedSamplesAsync(Batch batch, SelectedTestCodes testCodes, string? submittedAsCode, IReadOnlyList<CopiedSamplePlan> plan, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CopySubmissionAsync(BatchSubmission source, int newBatchId, int userId, int animalId = 0, CancellationToken ct = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Animal>> GetAnimalsByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Animal>)new List<Animal>());
        public Task<int> AddAnimalAsync(int batchSubmissionId, string senderRef, int userId, string? pmDate = null, bool pmDateSet = false, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CopyAnimalAsync(Animal source, int newBatchSubmissionId, string newSenderRef, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> UpdateAnimalAsync(Animal animal, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> DeleteAnimalAsync(int animalId, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> DeleteSampleFromBatchAsync(int batchId, int animalId, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> AdvanceNextBlockRefAsync(int animalId, string nextBlockRef, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyList<SenderSearchResult>> GetAnimalBySenderAsync(string senderRef, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<SenderSearchResult>)new List<SenderSearchResult>());
        public Task<IReadOnlyList<string>> GetExistingSenderRefsAsync(IEnumerable<string> senderRefs, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<string>)new List<string>());
        public Task UpdateAnimalSenderRefAsync(string senderRef, string newSenderRef, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAnimalHistologyRefAsync(string senderRef, string? newHistologyRef, int userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Tissue>> GetTissuesBySubmissionAsync(int batchId, int submissionId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Tissue>)new List<Tissue>());
        public Task<IReadOnlyList<Tissue>> GetTissuesByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Tissue>)new List<Tissue>());
        public Task<IReadOnlyList<Tissue>> GetTissuesByBlockAsync(int batchId, int blockId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Tissue>)new List<Tissue>());
        public Task<int> AddTissueAsync(Tissue tissue, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CopyTissueAsync(Tissue source, int newOwnerId, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> DeleteTissueAsync(int tissueId, TissueOwner owner, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyList<PmDateSearchResult>> GetByPmDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<PmDateSearchResult>)new List<PmDateSearchResult>());
        public Task<IReadOnlyList<SenderSearchResult>> GetAnimalsBySenderRefAsync(string senderRef, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<SenderSearchResult>)new List<SenderSearchResult>());
        public Task<IReadOnlyList<TissueArchiveInfo>> GetTissueArchiveAsync(string? senderRef, string? histologyRef, string? archiveLocation, string? tissueCode, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TissueArchiveInfo>)new List<TissueArchiveInfo>());
        public Task<IReadOnlyList<ImportedDataRow>> GetImportedDataAsync(string? selectedTable, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<ImportedDataRow>)new List<ImportedDataRow>());
        public Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalTissuesAsync(string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AnimalTissueSearchResult>)new List<AnimalTissueSearchResult>());
        public Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalBlockTissuesAsync(string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AnimalTissueSearchResult>)new List<AnimalTissueSearchResult>());
    }
}
