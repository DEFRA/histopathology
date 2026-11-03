using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

public class SearchPMDatesModelTests
{
    private readonly Mock<ISubmissionService> _submissions = new();

    private SearchPMDatesModel CreateSut() =>
        new(new Mock<ISessionService>().Object, _submissions.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    private static PmDateSearchResult Row(int n) => new()
    {
        ID = n,
        SenderRef = $"S{n}",
        PMDate = new DateTime(2024, 1, n),
        BatchDate = new DateTime(2024, 2, n),
        DateReceived = new DateTime(2024, 3, n),
        TimeReceived = $"0{n}:00",
        CompletedDate = new DateTime(2024, 4, n),
        CustomerReceivedDate = new DateTime(2024, 5, n),
    };

    [Theory]
    [InlineData("SenderRef")]
    [InlineData("PMDate")]
    [InlineData("BatchDate")]
    [InlineData("DateReceived")]
    [InlineData("TimeReceived")]
    [InlineData("CompletedDate")]
    [InlineData("CustomerReceivedDate")]
    [InlineData("ID")]
    public async Task GetPagedResults_SortsAscendingByRequestedColumn(string column)
    {
        _submissions.Setup(s => s.GetByPmDateRangeAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PmDateSearchResult>)[Row(2), Row(3), Row(1)]);
        var sut = CreateSut();
        sut.Submitted = true;
        sut.SortColumn = column;
        sut.SortDesc = false;

        await sut.OnGetAsync();

        Assert.Equal([1, 2, 3], sut.GetPagedResults().Select(r => r.ID));
    }

    [Fact]
    public async Task OnGetAsync_NoSortColumn_DefaultsToSubmissionNumberDescending()
    {
        _submissions.Setup(s => s.GetByPmDateRangeAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PmDateSearchResult>)[Row(2), Row(3), Row(1)]);
        var sut = CreateSut();
        sut.Submitted = true;

        await sut.OnGetAsync();

        Assert.Equal([3, 2, 1], sut.GetPagedResults().Select(r => r.ID));
    }
}
