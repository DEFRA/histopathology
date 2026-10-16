using Histo.Administration.Interfaces;
using Histo.Administration.Models;
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
/// Regression suite for Edit batch tests — the "Select Histology and required tests" page split
/// out of legacy BatchDetails.aspx. Previously had zero test coverage of any kind (Tier 1 gap).
/// Backed by a <see cref="FakeBatchTestSelectionService"/> so the GET→POST→GET round trip (load
/// existing selections, save new ones, reload and see them persisted) is genuinely exercised.
/// </summary>
public class EditBatchTestsRegressionTests
{
    private readonly FakeBatchTestSelectionService _batches = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public EditBatchTestsRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { Code = HistologyCode.HAndE, Name = "H&E" },
                new LookupItem { Code = HistologyCode.SpecialStain, Name = "Special Stain" },
                new LookupItem { Code = HistologyCode.IhcPrp, Name = "IHC-PrP" },
                new LookupItem { Code = HistologyCode.IhcOther, Name = "IHC-Other" },
                new LookupItem { Code = HistologyCode.HeBse, Name = "H&E (BSE)" },
            ]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { Code = "A1", Name = "Antibody 1" },
                new LookupItem { Code = "S1", Name = "Stain 1" },
            ]);
    }

    private EditBatchTestsModel CreateSut(int batchId)
    {
        _session.Setup(s => s.BatchID).Returns(batchId);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return new(_session.Object, _batches, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
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
    public async Task OnGetAsync_ExistingSelections_PrePopulatesCheckboxes()
    {
        var batchId = _batches.Seed(new Batch { BatchType = BatchTypeConstants.Tse });
        _batches.SaveSelections(batchId, [HistologyCode.HAndE], [], []);
        var sut = CreateSut(batchId);

        await sut.OnGetAsync();

        Assert.Contains(HistologyCode.HAndE, sut.SelectedHistologyCodes);
    }

    [Fact]
    public async Task OnPostAsync_NoHistologySelected_IsRejected()
    {
        var batchId = _batches.Seed(new Batch { BatchType = BatchTypeConstants.Tse });
        var sut = CreateSut(batchId);

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select at least one histology type.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_ArchiveCombinedWithOtherTypes_IsRejected()
    {
        var batchId = _batches.Seed(new Batch { BatchType = BatchTypeConstants.Tse });
        var sut = CreateSut(batchId);
        sut.SelectedHistologyCodes = [HistologyCode.Archive, HistologyCode.HAndE];

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Archive cannot be combined with other histology types.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_SpecialStainWithNoStainsChosen_IsRejected()
    {
        var batchId = _batches.Seed(new Batch { BatchType = BatchTypeConstants.Tse });
        var sut = CreateSut(batchId);
        sut.SelectedHistologyCodes = [HistologyCode.SpecialStain];

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Special Stain is selected — you must also select at least one special stain.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_IhcWithNoAntibodiesChosen_IsRejected()
    {
        var batchId = _batches.Seed(new Batch { BatchType = BatchTypeConstants.Tse });
        var sut = CreateSut(batchId);
        sut.SelectedHistologyCodes = [HistologyCode.IhcPrp];

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("IHC is selected — you must also select at least one antibody.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_ValidSelection_SavesAndRedirectsAndClearsUnrelatedStainAndAntibodyCodes()
    {
        var batchId = _batches.Seed(new Batch { BatchType = BatchTypeConstants.Tse });
        var sut = CreateSut(batchId);
        // H&E only — but the form posted stray Stain/Antibody codes left over from an earlier
        // selection that's no longer applicable. They must be cleaned, not saved verbatim.
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SelectedStainCodes = ["S1"];
        sut.SelectedAntibodyCodes = ["A1"];

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchDetails", redirect.PageName);
        var saved = _batches.GetSelections(batchId);
        Assert.Equal([HistologyCode.HAndE], saved.Histology.Select(r => r.Code));
        Assert.Empty(saved.Stains);
        Assert.Empty(saved.Antibodies);
    }

    [Fact]
    public async Task LoadLookupOptions_NonTseBatch_HidesIhcPrpAndHeBse()
    {
        var batchId = _batches.Seed(new Batch { BatchType = BatchTypeConstants.NonTse });
        var sut = CreateSut(batchId);

        await sut.OnGetAsync();

        Assert.DoesNotContain(sut.HistologyOptions, o => o.Code == HistologyCode.IhcPrp);
        Assert.DoesNotContain(sut.HistologyOptions, o => o.Code == HistologyCode.HeBse);
        Assert.Contains(sut.HistologyOptions, o => o.Code == HistologyCode.IhcOther);
    }

    [Fact]
    public async Task LoadLookupOptions_TseBatch_HidesIhcOther()
    {
        var batchId = _batches.Seed(new Batch { BatchType = BatchTypeConstants.Tse });
        var sut = CreateSut(batchId);

        await sut.OnGetAsync();

        Assert.DoesNotContain(sut.HistologyOptions, o => o.Code == HistologyCode.IhcOther);
        Assert.Contains(sut.HistologyOptions, o => o.Code == HistologyCode.IhcPrp);
    }

    /// <summary>In-memory stand-in for the subset of batch stored procedures EditBatchTestsModel calls.</summary>
    private sealed class FakeBatchTestSelectionService : IBatchService
    {
        private int _nextId = 1;
        private readonly Dictionary<int, Batch> _batchStore = [];
        private readonly Dictionary<int, BatchTestSelections> _selections = [];

        public int Seed(Batch batch)
        {
            var id = _nextId++;
            _batchStore[id] = new Batch { ID = id, BatchType = batch.BatchType, Status = batch.Status };
            return id;
        }

        public void SaveSelections(int batchId, IReadOnlyList<string> histology, IReadOnlyList<string> antibodies, IReadOnlyList<string> stains) =>
            _selections[batchId] = new BatchTestSelections
            {
                Histology = histology.Select(c => new BatchTestSelectionRow { Code = c }).ToList(),
                Antibodies = antibodies.Select(c => new BatchTestSelectionRow { Code = c }).ToList(),
                Stains = stains.Select(c => new BatchTestSelectionRow { Code = c }).ToList(),
            };

        public BatchTestSelections GetSelections(int batchId) => _selections.TryGetValue(batchId, out var s) ? s : new BatchTestSelections();

        public Task<Batch?> GetByIdAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult(_batchStore.TryGetValue(batchId, out var b) ? b : null);

        public Task<BatchTestSelections> GetBatchTestSelectionsAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult(GetSelections(batchId));

        public Task<bool> SaveBatchTestSelectionsAsync(int batchId, IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes, int userId, CancellationToken ct = default)
        {
            SaveSelections(batchId, histologyCodes, antibodyCodes, stainCodes);
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
        public Task<bool> UpdateAsync(Batch batch, int userId, CancellationToken ct = default) => Task.FromResult(true);
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
