using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for the Search module date-range validation restored from the legacy
/// <c>IsDateRangeValid</c> checks, and for the lookup-populated filters on
/// <see cref="SearchSubmissionsModel"/> (legacy <c>ddlProject</c>, <c>ddlContact</c>,
/// <c>ddlSpecies</c>, <c>ddlFixation</c>, <c>ddlUserArea</c>).
/// </summary>
public class SearchValidationTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();

    public SearchValidationTests()
    {
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<User>)[]);
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[]);
        _submissions.Setup(s => s.GetByPmDateRangeAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PmDateSearchResult>)[]);
        _blocks.Setup(b => b.GetUsedBlockRefsByHistologyRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        _blocks.Setup(b => b.GetUsedBlockRefsBySenderRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
    }

    private static PageContext NewPageContext() =>
        new()
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            // GridPageModel.SortBase reads Request.Query, so an HttpContext must be present.
            HttpContext = new DefaultHttpContext(),
        };

    private SearchSubmissionsModel CreateSearchSubmissions() =>
        new(_session.Object, _batches.Object, _users.Object, _lookups.Object) { PageContext = NewPageContext() };

    private SearchPMDatesModel CreateSearchPmDates() =>
        new(_session.Object, _submissions.Object) { PageContext = NewPageContext() };

    private static DateParts Parts(int day, int month, int year) =>
        new() { Day = day.ToString(), Month = month.ToString(), Year = year.ToString() };

    // ── SearchPMDates ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchPMDates_ReversedRange_AddsErrorAndDoesNotQuery()
    {
        var sut = CreateSearchPmDates();
        sut.StartDate = Parts(10, 5, 2026);
        sut.EndDate = Parts(1, 5, 2026);

        await sut.OnPostSearchAsync();

        Assert.True(sut.Errors.ContainsKey("StartDate-day"));
        Assert.False(sut.Searched);
        _submissions.Verify(s => s.GetByPmDateRangeAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchPMDates_EqualDates_IsValidAndQueries()
    {
        var sut = CreateSearchPmDates();
        sut.StartDate = Parts(1, 5, 2026);
        sut.EndDate = Parts(1, 5, 2026);

        await sut.OnPostSearchAsync();

        Assert.Empty(sut.Errors);
        Assert.True(sut.Searched);
        var day = new DateTime(2026, 5, 1);
        _submissions.Verify(s => s.GetByPmDateRangeAsync(day, day, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchPMDates_MissingDates_TreatedAsOpenRangeAndSearches()
    {
        var sut = CreateSearchPmDates();

        await sut.OnPostSearchAsync();

        Assert.Empty(sut.Errors);
        Assert.True(sut.Searched);
        _submissions.Verify(s => s.GetByPmDateRangeAsync(null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchPMDates_ImpossibleDate_IsRejected()
    {
        var sut = CreateSearchPmDates();
        sut.StartDate = Parts(31, 2, 2026);
        sut.EndDate = Parts(1, 5, 2026);

        await sut.OnPostSearchAsync();

        Assert.Equal("PM from date must be a real date.", sut.Errors["StartDate-day"]);
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task SearchPMDates_PartiallyEnteredDate_IsRejected()
    {
        var sut = CreateSearchPmDates();
        sut.StartDate = new DateParts { Day = "1", Month = "5" };
        sut.EndDate = Parts(1, 5, 2026);

        await sut.OnPostSearchAsync();

        Assert.Equal("PM from date must be a real date.", sut.Errors["StartDate-day"]);
        Assert.False(sut.Searched);
    }

    // ── SearchSubmissions ────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchSubmissions_ReversedSubmittedRange_AddsErrorAndDoesNotQuery()
    {
        var sut = CreateSearchSubmissions();
        sut.SubmittedDateFrom = Parts(10, 5, 2026);
        sut.SubmittedDateTo = Parts(1, 5, 2026);

        await sut.OnPostSearchAsync();

        Assert.True(sut.Errors.ContainsKey("SubmittedDateFrom-day"));
        Assert.False(sut.Searched);
        _batches.Verify(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchSubmissions_ReversedReceivedRange_AddsError()
    {
        var sut = CreateSearchSubmissions();
        sut.ReceivedDateFrom = Parts(10, 5, 2026);
        sut.ReceivedDateTo = Parts(1, 5, 2026);

        await sut.OnPostSearchAsync();

        Assert.True(sut.Errors.ContainsKey("ReceivedDateFrom-day"));
        Assert.False(sut.Searched);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task SearchSubmissions_NonPositiveSubmissionNumber_AddsError(int number)
    {
        var sut = CreateSearchSubmissions();
        sut.SubmissionNumber = number;

        await sut.OnPostSearchAsync();

        Assert.True(sut.Errors.ContainsKey(nameof(sut.SubmissionNumber)));
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task SearchSubmissions_OneSidedDateRange_IsValid()
    {
        var sut = CreateSearchSubmissions();
        sut.SubmittedDateFrom = Parts(10, 5, 2026);

        await sut.OnPostSearchAsync();

        Assert.Empty(sut.Errors);
        Assert.True(sut.Searched);
    }

    [Fact]
    public async Task SearchSubmissions_AllDatesBlank_IsValidBecauseDateFiltersAreOptional()
    {
        var sut = CreateSearchSubmissions();

        await sut.OnPostSearchAsync();

        Assert.Empty(sut.Errors);
        Assert.True(sut.Searched);
    }

    [Fact]
    public async Task SearchSubmissions_OnGet_PopulatesLookupBackedFilters()
    {
        var sut = CreateSearchSubmissions();

        await sut.OnGetAsync();

        // Legacy LoadLookupLists() bound Project(19), Contacts(18), Fixation(10) and UserArea(13).
        _lookups.Verify(l => l.GetLookupDataAsync(19, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _lookups.Verify(l => l.GetLookupDataAsync(18, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _lookups.Verify(l => l.GetLookupDataAsync(10, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _lookups.Verify(l => l.GetLookupDataAsync(13, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _lookups.Verify(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchSubmissions_BlankTextFilters_AreSentAsNullSoTheSpAppliesNoFilter()
    {
        var sut = CreateSearchSubmissions();
        sut.ProjectContractCode = "";
        sut.SubmittedArea = "   ";
        BatchSearchCriteria? captured = null;
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .Callback<BatchSearchCriteria, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[]);

        await sut.OnPostSearchAsync();

        Assert.NotNull(captured);
        Assert.Null(captured!.ProjectContractCode);
        Assert.Null(captured.SubmittedArea);
    }

    [Fact]
    public async Task SearchSubmissions_DefaultSort_IsMostRecentSubmissionFirst()
    {
        // Legacy FillSearchGrid set dvBatchesView.Sort = "ID DESC" when no sort was chosen.
        var sut = CreateSearchSubmissions();
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)
            [
                new BatchSearchResult { ID = 1 }, new BatchSearchResult { ID = 3 }, new BatchSearchResult { ID = 2 }
            ]);

        await sut.OnPostSearchAsync();

        Assert.Equal([3, 2, 1], sut.PagedResults.Select(r => r.ID));
    }

    [Fact]
    public async Task SearchSubmissions_PagesResultsRatherThanRenderingEverything()
    {
        var sut = CreateSearchSubmissions();
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)
                [.. Enumerable.Range(1, 25).Select(i => new BatchSearchResult { ID = i })]);

        await sut.OnPostSearchAsync();

        Assert.Equal(25, sut.Results.Count);
        Assert.Equal(10, sut.PagedResults.Count);
    }

    // ── SearchBlockRefs ──────────────────────────────────────────────────────────

    private SearchBlockRefsModel CreateSearchBlockRefs() =>
        new(_session.Object, _blocks.Object) { PageContext = NewPageContext() };

    [Fact]
    public async Task SearchBlockRefs_QueryStringHistologyRef_AutoRunsSearch()
    {
        // Legacy Page_Load read Request.QueryString("HistologyRef") and searched immediately.
        var sut = CreateSearchBlockRefs();
        sut.HistologyRef = "H123";

        await sut.OnGetAsync();

        Assert.Empty(sut.Errors);
        Assert.True(sut.Searched);
        _blocks.Verify(b => b.GetUsedBlockRefsByHistologyRefAsync("H123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchBlockRefs_BothRefsSupplied_SearchesBySenderRefPrecedence()
    {
        // GetBlocksForHistoRef/GetBlocksForSenderRef tolerate both being supplied — Sender ref
        // takes precedence, matching the same relaxed rule applied to SearchArchiveLocation.
        var sut = CreateSearchBlockRefs();
        sut.SenderRef = "S1";
        sut.HistologyRef = "H1";

        await sut.OnGetAsync();

        Assert.Empty(sut.Errors);
        Assert.True(sut.Searched);
        _blocks.Verify(b => b.GetUsedBlockRefsBySenderRefAsync("S1", It.IsAny<CancellationToken>()), Times.Once);
        _blocks.Verify(b => b.GetUsedBlockRefsByHistologyRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchBlockRefs_FirstVisitWithNoCriteria_ShowsEmptyFormWithoutError()
    {
        var sut = CreateSearchBlockRefs();

        await sut.OnGetAsync();

        Assert.Empty(sut.Errors);
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task SearchBlockRefs_SubmittedWithNoCriteria_ShowsValidationError()
    {
        var sut = CreateSearchBlockRefs();
        sut.Submitted = true;

        await sut.OnGetAsync();

        Assert.True(sut.Errors.ContainsKey(nameof(sut.SenderRef)));
        Assert.False(sut.Searched);
    }

    // ── ViewSamples ──────────────────────────────────────────────────────────────

    private ViewSamplesModel CreateViewSamples() =>
        new(_session.Object, _submissions.Object, _lookups.Object) { PageContext = NewPageContext() };

    [Fact]
    public async Task ViewSamples_BothRefsSupplied_SearchesSuccessfully()
    {
        // GetAnimalBatchTissues/GetAnimalBlockTissues tolerate both being supplied — each
        // branches internally on one ref and ignores the other, no error either way.
        _submissions.Setup(s => s.GetAnimalTissuesAsync("S1", "H1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AnimalTissueSearchResult>)[]);
        var sut = CreateViewSamples();
        sut.SenderRef = "S1";
        sut.HistologyRef = "H1";

        await sut.OnPostSearchAsync();

        Assert.Empty(sut.Errors);
        Assert.True(sut.Searched);
    }

    [Fact]
    public async Task ViewSamples_NoCriteria_ShowsValidationError()
    {
        var sut = CreateViewSamples();

        await sut.OnPostSearchAsync();

        Assert.True(sut.Errors.ContainsKey(nameof(sut.SenderRef)));
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task ViewSamples_BlankSenderRefWithHistologyRef_PassesNullNotEmptyString()
    {
        // Blank text input posts "", not null — must be converted before the SP call or the
        // "@SenderRef IS NULL" branch check silently fails to match.
        _submissions.Setup(s => s.GetAnimalTissuesAsync(null, "H1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AnimalTissueSearchResult>)[]);
        var sut = CreateViewSamples();
        sut.SenderRef = "";
        sut.HistologyRef = "H1";

        await sut.OnPostSearchAsync();

        Assert.Empty(sut.Errors);
        _submissions.Verify(s => s.GetAnimalTissuesAsync(null, "H1", null, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
