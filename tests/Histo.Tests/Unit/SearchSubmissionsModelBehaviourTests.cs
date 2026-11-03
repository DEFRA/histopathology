using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Sorting, validation, selection state and action-button availability of <see cref="SearchSubmissionsModel"/>.</summary>
public class SearchSubmissionsModelBehaviourTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockTestService> _tests = new();
    private IReadOnlyList<BatchSearchResult> _results = [];

    public SearchSubmissionsModelBehaviourTests()
    {
        _session.SetupAllProperties();
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => _results);
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Histo.Histology.Models.Block>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _tests.Setup(t => t.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Histo.Histology.Models.BlockTest>)[]);
    }

    private SearchSubmissionsModel CreateSut()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns<string>(s => s?.StartsWith('/') == true);
        return new(_session.Object, _batches.Object, _users.Object, _lookups.Object, _blocks.Object, _submissions.Object, _tests.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
            Url = url.Object,
        };
    }

    private static BatchSearchResult Row(int id, string project, string contact, string species, int dayOffset, string status) => new()
    {
        ID = id, ProjectDescription = project, ContactDescription = contact, Species = species,
        BatchDate = new DateTime(2026, 1, 1).AddDays(dayOffset), Status = status,
    };

    private static IReadOnlyList<BatchSearchResult> ThreeRows() =>
    [
        Row(2, "B", "Y", "Ovine", 2, BatchStatus.Received),
        Row(1, "A", "Z", "Bovine", 3, BatchStatus.Completed),
        Row(3, "C", "X", "Caprine", 1, BatchStatus.Submitted),
    ];

    [Theory]
    [InlineData("ProjectDescription", false, "1,2,3")]
    [InlineData("ProjectDescription", true, "3,2,1")]
    [InlineData("ContactDescription", false, "3,2,1")]
    [InlineData("ContactDescription", true, "1,2,3")]
    [InlineData("Species", false, "1,3,2")]
    [InlineData("Species", true, "2,3,1")]
    [InlineData("BatchDate", false, "3,2,1")]
    [InlineData("BatchDate", true, "1,2,3")]
    [InlineData("Status", false, "3,2,1")]
    [InlineData("Status", true, "1,2,3")]
    [InlineData(null, false, "1,2,3")]
    [InlineData(null, true, "3,2,1")]
    public async Task GetPagedResults_SortsByRequestedColumnAndDirection(string? column, bool desc, string expectedIds)
    {
        _results = ThreeRows();
        var sut = CreateSut();
        sut.SortColumn = column;
        sut.SortDesc = desc;
        await sut.OnGetAsync();

        var ids = string.Join(',', sut.GetPagedResults().Select(r => r.ID));

        Assert.Equal(expectedIds, ids);
    }

    [Fact]
    public async Task GetPagedResults_SecondPage_ReturnsRemainingRows()
    {
        _results = Enumerable.Range(1, 12).Select(i => Row(i, "P", "C", "S", i, BatchStatus.Received)).ToList();
        var sut = CreateSut();
        sut.SortDesc = false;
        sut.PageNumber = 2;
        await sut.OnGetAsync();

        Assert.Equal([11, 12], sut.GetPagedResults().Select(r => r.ID));
    }

    [Theory]
    [InlineData(null, "/Search/SearchMenu")]
    [InlineData("https://evil.example.com", "/Search/SearchMenu")]
    [InlineData("/Home", "/Home")]
    public void BackLinkPage_OnlyAllowsLocalReturnPages(string? returnPage, string expected)
    {
        var sut = CreateSut();
        sut.ReturnPage = returnPage;

        Assert.Equal(expected, sut.BackLinkPage);
    }

    [Fact]
    public void SelectFormAction_EncodesSortColumn()
    {
        var sut = CreateSut();
        sut.SortColumn = "a&b";

        Assert.DoesNotContain("a&b", sut.SelectFormAction);
        Assert.Contains("a%26b", sut.SelectFormAction);
    }

    [Theory]
    [InlineData(null, false, false, false, false, false)]
    [InlineData(BatchStatus.Submitted, true, false, false, false, false)]
    [InlineData(BatchStatus.Rejected, false, false, true, false, true)]
    [InlineData(BatchStatus.Completed, true, false, true, true, true)]
    [InlineData(BatchStatus.Received, true, true, true, true, true)]
    [InlineData(BatchStatus.OnHold, true, true, true, true, true)]
    [InlineData(BatchStatus.InProgress, true, true, true, true, true)]
    public async Task SelectedBatchStatus_DrivesActionButtonAvailability(string? status, bool print, bool edit, bool view, bool quality, bool receipt)
    {
        _results = status is null ? [] : [Row(7, "P", "C", "S", 1, status)];
        var sut = CreateSut();
        sut.SelectedBatchId = 7;

        await sut.OnPostSelectAsync();

        Assert.Equal(status, sut.SelectedBatchStatus);
        Assert.Equal(print, sut.CanPrintSubmission);
        Assert.Equal(edit, sut.CanEditSubmission);
        Assert.Equal(view, sut.CanViewSubmission);
        Assert.Equal(quality, sut.CanViewQualityData);
        Assert.Equal(quality, sut.CanViewArchiveData);
        Assert.Equal(receipt, sut.CanViewReceipt);
    }

    [Fact]
    public async Task OnPostSearchAsync_InvalidSubmissionNumber_ReturnsPageWithErrorAndSkipsSearch()
    {
        var sut = CreateSut();
        sut.SubmissionNumber = 0;

        var result = await sut.OnPostSearchAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.Errors.ContainsKey(nameof(sut.SubmissionNumber)));
        Assert.False(sut.Searched);
        _batches.Verify(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostSearchAsync_UnparseableDates_ReportsEachDateField()
    {
        var sut = CreateSut();
        foreach (var field in new[] { "SubmittedDateFrom", "SubmittedDateTo", "ReceivedDateFrom", "ReceivedDateTo" })
            sut.ModelState.AddModelError(field, "bad");

        await sut.OnPostSearchAsync();

        Assert.Equal(4, sut.Errors.Count);
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task OnPostSearchAsync_DateRangesReversed_ReportsBothRanges()
    {
        var sut = CreateSut();
        sut.SubmittedDateFrom = new DateTime(2026, 5, 2);
        sut.SubmittedDateTo = new DateTime(2026, 5, 1);
        sut.ReceivedDateFrom = new DateTime(2026, 6, 2);
        sut.ReceivedDateTo = new DateTime(2026, 6, 1);

        await sut.OnPostSearchAsync();

        Assert.True(sut.Errors.ContainsKey("SubmittedDateFrom"));
        Assert.True(sut.Errors.ContainsKey("ReceivedDateFrom"));
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task OnPostSearchAsync_ValidCriteria_SearchesWithBlankStringsAsNull()
    {
        var sut = CreateSut();
        sut.Status = " ";
        sut.SenderRef = "C1";
        sut.SubmissionNumber = 12;

        await sut.OnPostSearchAsync();

        Assert.True(sut.Searched);
        _batches.Verify(b => b.SearchAsync(
            It.Is<BatchSearchCriteria>(c => c.Status == null && c.SenderRef == "C1" && c.SubmissionNumber == 12),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null, "/Search/SearchSubmissions")]
    [InlineData("/Home", "/Search/SearchSubmissions?ReturnPage=%2FHome")]
    public async Task OnPostSelectAsync_SetsSessionNavigationState(string? returnPage, string expectedReturn)
    {
        var sut = CreateSut();
        sut.SelectedBatchId = 9;
        sut.ReturnPage = returnPage;

        await sut.OnPostSelectAsync();

        Assert.Equal(9, _session.Object.BatchID);
        Assert.Equal(expectedReturn, _session.Object.ReturnPage);
        Assert.True(_session.Object.IsViewSubmissionMode);
        Assert.Null(_session.Object.ReturnPageQuery);
    }

    [Fact]
    public async Task OnPostSelectAsync_NoSelection_LeavesSessionBatchUntouched()
    {
        var sut = CreateSut();
        sut.SelectedBatchId = 0;

        await sut.OnPostSelectAsync();

        Assert.Null(_session.Object.BatchID);
        Assert.False(sut.HasNotes);
    }

    [Fact]
    public async Task OnPostExportExcelAsync_ReturnsSearchSubmissionsWorkbook()
    {
        _results = ThreeRows();

        var result = await CreateSut().OnPostExportExcelAsync();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("search-submissions.xlsx", file.FileDownloadName);
    }
}
