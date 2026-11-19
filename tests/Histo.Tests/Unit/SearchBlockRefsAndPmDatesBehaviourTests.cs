using Histo.Histology.Interfaces;
using Histo.Histology.Models;
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

public class SearchBlockRefsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockService> _blocks = new();

    public SearchBlockRefsModelTests()
    {
        _blocks.Setup(b => b.GetUsedBlockRefsBySenderRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[new UsedBlockRef { BlockRef = 1, Status = 1 }, new UsedBlockRef { BlockRef = 4, Status = 2 }]);
        _blocks.Setup(b => b.GetUsedBlockRefsByHistologyRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[new UsedBlockRef { BlockRef = 2, Status = 1 }]);
    }

    private SearchBlockRefsModel CreateSut()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns<string>(s => s?.StartsWith('/') == true);
        return new(_session.Object, _blocks.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
            Url = url.Object,
        };
    }

    [Theory]
    [InlineData(null, "/Search/SearchMenu")]
    [InlineData("http://evil.test", "/Search/SearchMenu")]
    [InlineData("/Bookings/BookBlockRef?x=1", "/Bookings/BookBlockRef?x=1")]
    public void BackLinkPage_OnlyAllowsLocalReturnPages(string? returnPage, string expected)
    {
        var sut = CreateSut();
        sut.ReturnPage = returnPage;

        Assert.Equal(expected, sut.BackLinkPage);
    }

    [Theory]
    [InlineData(null, null, "")]
    [InlineData(null, " H1 ", "H1")]
    [InlineData(" S1 ", null, "S1")]
    [InlineData(" S1 ", " H1 ", "S1 H1")]
    public void ViewImportedDataFilter_CombinesSenderAndHistologyRefs(string? senderRef, string? histologyRef, string expected)
    {
        var sut = CreateSut();
        sut.SenderRef = senderRef;
        sut.HistologyRef = histologyRef;

        Assert.Equal(expected, sut.ViewImportedDataFilter);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task OnGetAsync_NoRefs_ShowsErrorOnlyWhenSubmitted(bool submitted, bool expectError)
    {
        var sut = CreateSut();
        sut.Submitted = submitted;

        await sut.OnGetAsync();

        Assert.Equal(expectError, sut.Errors.ContainsKey(nameof(sut.SenderRef)));
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task OnGetAsync_SenderRef_SearchesBySenderRefAndDefaultsSortToUsedDescending()
    {
        var sut = CreateSut();
        sut.SenderRef = "S1";
        sut.HistologyRef = "H1";

        await sut.OnGetAsync();

        Assert.True(sut.Searched);
        Assert.Equal("UsedBlockRefs", sut.SortColumn);
        Assert.True(sut.SortDesc);
        _blocks.Verify(b => b.GetUsedBlockRefsBySenderRefAsync("S1", It.IsAny<CancellationToken>()), Times.Once);
        _blocks.Verify(b => b.GetUsedBlockRefsByHistologyRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(sut.Results.Count, sut.TotalCount);
    }

    [Fact]
    public async Task OnGetAsync_OnlyHistologyRef_SearchesByHistologyRefAndKeepsChosenSort()
    {
        var sut = CreateSut();
        sut.HistologyRef = "H1";
        sut.SortColumn = "UnusedBlockRefs";

        await sut.OnGetAsync();

        Assert.Equal("UnusedBlockRefs", sut.SortColumn);
        _blocks.Verify(b => b.GetUsedBlockRefsByHistologyRefAsync("H1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("UsedBlockRefs")]
    [InlineData("UnusedBlockRefs")]
    [InlineData("PreBookedBlockRefs")]
    [InlineData("Other")]
    public async Task GetPagedResults_EachSortColumnReturnsAllRowsInBothDirections(string column)
    {
        var sut = CreateSut();
        sut.SenderRef = "S1";
        sut.SortColumn = column;
        await sut.OnGetAsync();
        var total = sut.Results.Count;

        sut.SortDesc = false;
        var ascending = sut.GetPagedResults();
        sut.SortDesc = true;
        var descending = sut.GetPagedResults();

        Assert.Equal(total, ascending.Count);
        Assert.Equal(ascending.ToHashSet(), descending.ToHashSet());
    }

    [Fact]
    public async Task OnGetExportExcelAsync_NoRefs_RedirectsBackToSearch()
    {
        var result = await CreateSut().OnGetExportExcelAsync();

        Assert.Equal("/Search/SearchBlockRefs", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Theory]
    [InlineData("S1", null)]
    [InlineData(null, "H1")]
    public async Task OnGetExportExcelAsync_WithRef_ReturnsWorkbook(string? senderRef, string? histologyRef)
    {
        var sut = CreateSut();
        sut.SenderRef = senderRef;
        sut.HistologyRef = histologyRef;

        var result = await sut.OnGetExportExcelAsync();

        Assert.Equal("search-block-refs.xlsx", Assert.IsType<FileContentResult>(result).FileDownloadName);
    }
}

public class SearchPMDatesModelBehaviourTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    public SearchPMDatesModelBehaviourTests()
    {
        _submissions.Setup(s => s.GetByPmDateRangeAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PmDateSearchResult>)[
                new PmDateSearchResult { ID = 1, SenderRef = "B", PMDate = new DateTime(2026, 1, 3), BatchDate = new DateTime(2026, 2, 1), TimeReceived = "09:00" },
                new PmDateSearchResult { ID = 2, SenderRef = "A", PMDate = new DateTime(2026, 1, 2), BatchDate = new DateTime(2026, 2, 3), TimeReceived = "08:00" },
                new PmDateSearchResult { ID = 3, SenderRef = "C", PMDate = new DateTime(2026, 1, 1), BatchDate = new DateTime(2026, 2, 2), TimeReceived = "10:00" }]);
    }

    private SearchPMDatesModel CreateSut()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns<string>(s => s?.StartsWith('/') == true);
        return new(_session.Object, _submissions.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
            Url = url.Object,
        };
    }

    [Theory]
    [InlineData(null, "/Search/SearchMenu")]
    [InlineData("http://evil.test", "/Search/SearchMenu")]
    [InlineData("/Home", "/Home")]
    public void BackLinkPage_OnlyAllowsLocalReturnPages(string? returnPage, string expected)
    {
        var sut = CreateSut();
        sut.ReturnPage = returnPage;

        Assert.Equal(expected, sut.BackLinkPage);
    }

    [Fact]
    public async Task OnGetAsync_NotSubmitted_DoesNotSearch()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.False(sut.Searched);
        _submissions.Verify(s => s.GetByPmDateRangeAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_UnparseableDates_ReportsBothFieldsAndDoesNotSearch()
    {
        var sut = CreateSut();
        sut.ModelState.AddModelError("StartDate", "bad");
        sut.ModelState.AddModelError("EndDate", "bad");
        sut.Submitted = true;

        await sut.OnGetAsync();

        Assert.Equal(2, sut.Errors.Count);
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task OnGetAsync_FromAfterTo_ReportsRangeError()
    {
        var sut = CreateSut();
        sut.Submitted = true;
        sut.StartDate = new DateTime(2026, 3, 2);
        sut.EndDate = new DateTime(2026, 3, 1);

        await sut.OnGetAsync();

        Assert.True(sut.Errors.ContainsKey("StartDate"));
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task OnGetAsync_ValidRange_SearchesAndDefaultsSortToIdDescending()
    {
        var sut = CreateSut();
        sut.Submitted = true;
        sut.StartDate = new DateTime(2026, 1, 1);
        sut.EndDate = new DateTime(2026, 1, 31);

        await sut.OnGetAsync();

        Assert.True(sut.Searched);
        Assert.Equal("ID", sut.SortColumn);
        Assert.Equal([3, 2, 1], sut.GetPagedResults().Select(r => r.ID));
    }

    [Theory]
    [InlineData("SenderRef", "2,1,3")]
    [InlineData("PMDate", "3,2,1")]
    [InlineData("BatchDate", "1,3,2")]
    [InlineData("TimeReceived", "2,1,3")]
    [InlineData("DateReceived", "1,2,3")]
    [InlineData("CompletedDate", "1,2,3")]
    [InlineData("CustomerReceivedDate", "1,2,3")]
    public async Task GetPagedResults_SortsAscendingByRequestedColumn(string column, string expectedIds)
    {
        var sut = CreateSut();
        sut.Submitted = true;
        sut.SortColumn = column;
        sut.SortDesc = false;
        await sut.OnGetAsync();

        Assert.Equal(expectedIds, string.Join(',', sut.GetPagedResults().Select(r => r.ID)));
    }

    [Fact]
    public async Task OnGetExportExcelAsync_InvalidRange_RedirectsBackToSearch()
    {
        var sut = CreateSut();
        sut.StartDate = new DateTime(2026, 3, 2);
        sut.EndDate = new DateTime(2026, 3, 1);

        var result = await sut.OnGetExportExcelAsync();

        Assert.Equal("/Search/SearchPMDates", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetExportExcelAsync_ValidRange_ReturnsWorkbook()
    {
        var result = await CreateSut().OnGetExportExcelAsync();

        Assert.Equal("search-pm-dates.xlsx", Assert.IsType<FileContentResult>(result).FileDownloadName);
    }
}
