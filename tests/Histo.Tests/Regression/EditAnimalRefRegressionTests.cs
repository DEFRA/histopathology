using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the per-animal Sender Ref / Histology Ref rename utility (ISS-022) —
/// previously had zero test coverage of any kind (Tier 1 gap). Backed by a stateful
/// <see cref="FakeAnimalRefService"/> so a lookup-then-rename-then-lookup-again chain is
/// genuinely exercised.
/// </summary>
public class EditAnimalRefRegressionTests
{
    private readonly FakeAnimalRefService _submissions = new();
    private readonly Mock<ISessionService> _session = new();

    public EditAnimalRefRegressionTests() => _session.Setup(s => s.UserID).Returns(7);

    private EditAnimalRefModel CreateSut() => new(_session.Object, _submissions)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() },
    };

    [Fact]
    public async Task GetHistologyRef_UnknownSenderRef_IsRejected()
    {
        var sut = CreateSut();
        sut.OriginalSenderRef = "NOPE";

        var result = await sut.OnPostGetHistologyRefAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Sample Ref. not found.", sut.Error);
    }

    [Fact]
    public async Task GetHistologyRef_KnownSenderRef_ReturnsItsCurrentHistologyRef()
    {
        _submissions.Seed("S001", "23/00100");
        var sut = CreateSut();
        sut.OriginalSenderRef = "S001";

        await sut.OnPostGetHistologyRefAsync();

        Assert.Equal("23/00100", sut.CurrentHistologyRef);
    }

    [Fact]
    public async Task RenameSenderRef_ThenLookupAgain_ReflectsTheNewRef()
    {
        _submissions.Seed("S002", "23/00200");
        var rename = CreateSut();
        rename.OriginalSenderRef = "S002";
        rename.NewSenderRef = "S002-NEW";

        var result = await rename.OnPostEditSenderRefAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The new Sample Ref has been saved", rename.SuccessMessage);

        var lookup = CreateSut();
        lookup.OriginalSenderRef = "S002-NEW";
        await lookup.OnPostGetHistologyRefAsync();
        Assert.Equal("23/00200", lookup.CurrentHistologyRef);
    }

    [Fact]
    public async Task RenameSenderRef_ConflictingNewRef_SurfacesTheDomainErrorMessage()
    {
        _submissions.Seed("S003", "23/00300");
        _submissions.Seed("S004", "23/00400"); // target ref already belongs to another animal
        var sut = CreateSut();
        sut.OriginalSenderRef = "S003";
        sut.NewSenderRef = "S004";

        await sut.OnPostEditSenderRefAsync();

        Assert.Equal("The Sample Ref was not updated because: that sender ref is already in use.", sut.Error);
    }

    [Fact]
    public async Task SaveHistologyRef_InvalidFormat_IsRejectedWithoutCallingTheService()
    {
        var sut = CreateSut();
        sut.OriginalSenderRef = "S005";
        sut.NewHistologyRef = "not-a-ref";

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("You must enter a valid Histology Ref.", sut.Error);
        Assert.Equal("NewHistologyRef", sut.ErrorField);
    }

    [Fact]
    public async Task SaveHistologyRef_ValidFormat_Saves()
    {
        _submissions.Seed("S006", null);
        var sut = CreateSut();
        sut.OriginalSenderRef = "S006";
        sut.NewHistologyRef = "26/00123";

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The new Histology Ref has been saved", sut.SuccessMessage);
        Assert.Equal("26/00123", _submissions.GetHistologyRef("S006"));
    }

    [Fact]
    public async Task SaveHistologyRef_BlankValue_ClearsIt()
    {
        _submissions.Seed("S007", "26/00124");
        var sut = CreateSut();
        sut.OriginalSenderRef = "S007";
        sut.NewHistologyRef = "";

        await sut.OnPostSaveHistologyRefAsync();

        Assert.Equal("The old Histology Ref has been removed. You may now enter a new Histology Ref.", sut.SuccessMessage);
        Assert.Null(_submissions.GetHistologyRef("S007"));
    }

    /// <summary>In-memory stand-in for the subset of animal-ref stored procedures EditAnimalRefModel calls.</summary>
    private sealed class FakeAnimalRefService : ISubmissionService
    {
        private readonly Dictionary<string, string?> _histologyRefBySenderRef = [];

        public void Seed(string senderRef, string? histologyRef) => _histologyRefBySenderRef[senderRef] = histologyRef;
        public string? GetHistologyRef(string senderRef) => _histologyRefBySenderRef.GetValueOrDefault(senderRef);

        public Task<IReadOnlyList<SenderSearchResult>> GetAnimalBySenderAsync(string senderRef, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<SenderSearchResult>)(_histologyRefBySenderRef.ContainsKey(senderRef)
                ? [new SenderSearchResult { SenderRef = senderRef, HistologyRef = _histologyRefBySenderRef[senderRef] }]
                : []));

        public Task UpdateAnimalSenderRefAsync(string senderRef, string newSenderRef, int userId, CancellationToken ct = default)
        {
            if (!_histologyRefBySenderRef.ContainsKey(senderRef))
                throw new AnimalRefUpdateException("that sender ref was not found.");
            if (_histologyRefBySenderRef.ContainsKey(newSenderRef))
                throw new AnimalRefUpdateException("that sender ref is already in use.");

            var histologyRef = _histologyRefBySenderRef[senderRef];
            _histologyRefBySenderRef.Remove(senderRef);
            _histologyRefBySenderRef[newSenderRef] = histologyRef;
            return Task.CompletedTask;
        }

        public Task UpdateAnimalHistologyRefAsync(string senderRef, string? newHistologyRef, int userId, CancellationToken ct = default)
        {
            if (!_histologyRefBySenderRef.ContainsKey(senderRef))
                throw new AnimalRefUpdateException("that sender ref was not found.");
            _histologyRefBySenderRef[senderRef] = string.IsNullOrEmpty(newHistologyRef) ? null : newHistologyRef;
            return Task.CompletedTask;
        }

        // ── Unused by EditAnimalRefModel — stubs only ───────────────────────────────
        public Task<IReadOnlyList<BatchSubmission>> GetSubmissionsByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BatchSubmission>)new List<BatchSubmission>());
        public Task<int> AddSubmissionAsync(BatchSubmission submission, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> CreateMouseRangeAsync(int batchId, int? sourceAnimalId, string mouseNumberFrom, string mouseNumberTo, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> CreateBatchWithCopiedSamplesAsync(Batch batch, SelectedTestCodes testCodes, string? submittedAsCode, IReadOnlyList<CopiedSamplePlan> plan, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CopySubmissionAsync(BatchSubmission source, int newBatchId, int userId, int animalId = 0, CancellationToken ct = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Animal>> GetAnimalsByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Animal>)new List<Animal>());
        public Task<IReadOnlyList<Animal>> GetBlockAnimalsByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Animal>)new List<Animal>());
        public Task<int> AddAnimalAsync(int batchSubmissionId, string senderRef, int userId, string? pmDate = null, bool pmDateSet = false, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> CopyAnimalAsync(Animal source, int newBatchSubmissionId, string newSenderRef, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> UpdateAnimalAsync(Animal animal, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> DeleteAnimalAsync(int animalId, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> DeleteSampleFromBatchAsync(int batchId, int animalId, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> AdvanceNextBlockRefAsync(int animalId, string nextBlockRef, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyList<string>> GetExistingSenderRefsAsync(IEnumerable<string> senderRefs, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<string>)new List<string>());
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
