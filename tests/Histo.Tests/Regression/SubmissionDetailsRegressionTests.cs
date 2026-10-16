using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for Submission details (Wet Tissue sample tissue list) — previously had zero
/// test coverage of any kind (Tier 1 gap). Backed by a stateful <see cref="FakeSubmissionService"/>
/// so Add→Update→Delete tissue chains are genuinely exercised against the same animal/tissue data.
/// </summary>
public class SubmissionDetailsRegressionTests
{
    private readonly FakeSubmissionService _submissions = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();

    public SubmissionDetailsRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { Code = "AGAR", Name = "Agar" }]);
    }

    private SubmissionDetailsModel CreateSut(int? batchId, int? animalId)
    {
        _session.Setup(s => s.BatchID).Returns(batchId);
        _session.Setup(s => s.AnimalID).Returns(animalId);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return new(_session.Object, _submissions, _batches.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            BatchId = batchId,
            AnimalId = animalId,
        };
    }

    [Fact]
    public async Task OnGetAsync_NoBatchSelected_RedirectsToIndex()
    {
        var sut = CreateSut(batchId: null, animalId: null);

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_BatchButNoAnimal_RedirectsToSampleSummary()
    {
        var sut = CreateSut(batchId: 1, animalId: null);

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SampleSummary", redirect.PageName);
    }

    [Fact]
    public async Task AddThenList_NewTissueAppearsAndAllTissuesAssignedIsRefreshed()
    {
        var animalId = _submissions.SeedAnimal(batchId: 1, submissionId: 100);
        var sut = CreateSut(batchId: 1, animalId: animalId);
        await sut.OnGetAsync();

        sut.TissueCode = "AGAR";
        sut.NoPieces = 2;
        sut.Comment = "First piece";
        await sut.OnPostAddTissueAsync();

        var reload = CreateSut(batchId: 1, animalId: animalId);
        await reload.OnGetAsync();
        var tissue = Assert.Single(reload.Tissues);
        Assert.Equal("AGAR", tissue.TissueCode);
        Assert.Equal(2, tissue.NoPieces);
        _batches.Verify(b => b.RefreshAllTissuesAssignedAsync(1, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTissue_ChangesFieldsButPreservesArchiveData()
    {
        var animalId = _submissions.SeedAnimal(batchId: 2, submissionId: 200);
        var tissueId = _submissions.SeedTissue(submissionId: 200, code: "AGAR", pieces: 1, archiveLocation: "STORE-A");
        var sut = CreateSut(batchId: 2, animalId: animalId);
        await sut.OnGetAsync();

        sut.TissueId = tissueId;
        sut.EditTissueCode = "OTHER";
        sut.EditNoPieces = 3;
        sut.EditComment = "Changed";
        await sut.OnPostUpdateTissueAsync();

        var updated = _submissions.GetTissue(tissueId);
        Assert.Equal("OTHER", updated.TissueCode);
        Assert.Equal(3, updated.NoPieces);
        Assert.Equal("STORE-A", updated.ArchiveLocation); // archive fields carried through untouched
    }

    [Fact]
    public async Task DeleteTissue_RemovesItFromTheList()
    {
        var animalId = _submissions.SeedAnimal(batchId: 3, submissionId: 300);
        var tissueId = _submissions.SeedTissue(submissionId: 300, code: "AGAR", pieces: 1);
        var sut = CreateSut(batchId: 3, animalId: animalId);
        await sut.OnGetAsync();
        Assert.Single(sut.Tissues);

        await sut.OnPostDeleteTissueAsync(tissueId);

        var reload = CreateSut(batchId: 3, animalId: animalId);
        await reload.OnGetAsync();
        Assert.Empty(reload.Tissues);
    }

    [Fact]
    public async Task SaveDetails_UpdatesPMDateOnTheAnimal()
    {
        var animalId = _submissions.SeedAnimal(batchId: 4, submissionId: 400);
        var sut = CreateSut(batchId: 4, animalId: animalId);
        await sut.OnGetAsync();

        sut.PMDate = "2026-05-01";
        await sut.OnPostSaveDetailsAsync();

        var animal = _submissions.GetAnimal(animalId);
        Assert.True(animal.PMDateSet);
        Assert.NotNull(animal.PMDate);
    }

    /// <summary>In-memory stand-in for the subset of submission/tissue stored procedures SubmissionDetailsModel calls.</summary>
    private sealed class FakeSubmissionService : ISubmissionService
    {
        private int _nextAnimalId = 1;
        private int _nextTissueId = 1;
        private readonly Dictionary<int, Animal> _animals = [];
        private readonly Dictionary<int, BatchSubmission> _submissionRows = [];
        private readonly Dictionary<int, Tissue> _tissues = [];

        public int SeedAnimal(int batchId, int submissionId)
        {
            var id = _nextAnimalId++;
            _animals[id] = new Animal { ID = id, BatchSubmissionID = submissionId, SenderRef = $"S{id}" };
            _submissionRows[submissionId] = new BatchSubmission { ID = submissionId, BatchID = batchId, AnimalID = id };
            return id;
        }

        public int SeedTissue(int submissionId, string code, short pieces, string? archiveLocation = null)
        {
            var id = _nextTissueId++;
            _tissues[id] = new Tissue { ID = id, OwnerID = submissionId, Owner = TissueOwner.Submission, TissueCode = code, NoPieces = pieces, ArchiveLocation = archiveLocation };
            return id;
        }

        public Animal GetAnimal(int id) => _animals[id];
        public Tissue GetTissue(int id) => _tissues[id];

        public Task<IReadOnlyList<Animal>> GetBlockAnimalsByBatchAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<Animal>)new List<Animal>());

        public Task<IReadOnlyList<Animal>> GetAnimalsByBatchAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<Animal>)_animals.Values.Where(a => _submissionRows.Values.Any(s => s.AnimalID == a.ID && s.BatchID == batchId)).ToList());

        public Task<IReadOnlyList<BatchSubmission>> GetSubmissionsByBatchAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<BatchSubmission>)_submissionRows.Values.Where(s => s.BatchID == batchId).ToList());

        public Task<IReadOnlyList<Tissue>> GetTissuesBySubmissionAsync(int batchId, int submissionId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<Tissue>)_tissues.Values.Where(t => t.OwnerID == submissionId && t.Owner == TissueOwner.Submission).ToList());

        public Task<int> AddTissueAsync(Tissue tissue, int userId, CancellationToken ct = default)
        {
            var id = _nextTissueId++;
            _tissues[id] = new Tissue { ID = id, OwnerID = tissue.OwnerID, Owner = tissue.Owner, TissueCode = tissue.TissueCode, NoPieces = tissue.NoPieces, Comment = tissue.Comment };
            return Task.FromResult(id);
        }

        public Task<bool> UpdateTissueAsync(Tissue tissue, int userId, CancellationToken ct = default)
        {
            if (!_tissues.ContainsKey(tissue.ID)) return Task.FromResult(false);
            _tissues[tissue.ID] = tissue;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteTissueAsync(int tissueId, TissueOwner owner, int userId, CancellationToken ct = default) =>
            Task.FromResult(_tissues.Remove(tissueId));

        public Task<bool> UpdateAnimalAsync(Animal animal, int userId, CancellationToken ct = default)
        {
            if (!_animals.ContainsKey(animal.ID)) return Task.FromResult(false);
            _animals[animal.ID] = animal;
            return Task.FromResult(true);
        }

        // ── Unused by SubmissionDetailsModel — stubs only ───────────────────────────
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
        public Task<IReadOnlyList<Tissue>> GetBatchSubmissionTissuesAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Tissue>)new List<Tissue>());
        public Task<IReadOnlyList<Tissue>> GetTissuesByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Tissue>)new List<Tissue>());
        public Task<IReadOnlyList<Tissue>> GetTissuesByBlockAsync(int batchId, int blockId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Tissue>)new List<Tissue>());
        public Task<int> CopyTissueAsync(Tissue source, int newOwnerId, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<IReadOnlyList<PmDateSearchResult>> GetByPmDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<PmDateSearchResult>)new List<PmDateSearchResult>());
        public Task<IReadOnlyList<SenderSearchResult>> GetAnimalsBySenderRefAsync(string senderRef, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<SenderSearchResult>)new List<SenderSearchResult>());
        public Task<IReadOnlyList<TissueArchiveInfo>> GetTissueArchiveAsync(string? senderRef, string? histologyRef, string? archiveLocation, string? tissueCode, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<TissueArchiveInfo>)new List<TissueArchiveInfo>());
        public Task<IReadOnlyList<ImportedDataRow>> GetImportedDataAsync(string? selectedTable, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<ImportedDataRow>)new List<ImportedDataRow>());
        public Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalTissuesAsync(string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AnimalTissueSearchResult>)new List<AnimalTissueSearchResult>());
        public Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalBlockTissuesAsync(string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<AnimalTissueSearchResult>)new List<AnimalTissueSearchResult>());
    }
}
