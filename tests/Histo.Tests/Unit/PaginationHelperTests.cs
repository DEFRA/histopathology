using Histo.Core.Domain;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="PaginationHelpers.BuildPageItems"/> — the GOV.UK Design System
/// "at least one neighbour either side, first and last, ellipses for gaps" pagination rule.
/// https://design-system.service.gov.uk/components/pagination/
/// </summary>
public class PaginationHelperTests
{
    [Fact]
    public void BuildPageItems_SinglePage_ReturnsJustPageOne()
    {
        var items = PaginationHelpers.BuildPageItems(1, 1);
        Assert.Equal([1], items.Select(i => i.Page));
    }

    [Fact]
    public void BuildPageItems_FewPages_ShowsEveryPageNoEllipsis()
    {
        var items = PaginationHelpers.BuildPageItems(2, 4);
        Assert.Equal([1, 2, 3, 4], items.Select(i => i.Page));
        Assert.DoesNotContain(items, i => i.IsEllipsis);
    }

    [Theory]
    [InlineData(1, 100, "1,2,E,100")]
    [InlineData(2, 100, "1,2,3,E,100")]
    [InlineData(3, 100, "1,2,3,4,E,100")]
    [InlineData(4, 100, "1,2,3,4,5,E,100")]
    [InlineData(5, 100, "1,E,4,5,6,E,100")]
    [InlineData(98, 100, "1,E,97,98,99,100")]
    [InlineData(99, 100, "1,E,98,99,100")]
    [InlineData(100, 100, "1,E,99,100")]
    public void BuildPageItems_ManyPages_MatchesGdsExamples(int currentPage, int totalPages, string expectedCsv)
    {
        // Matches the worked examples on the GDS pagination page exactly, e.g. "1 [2] 3 … 100".
        var expected = expectedCsv.Split(',').Select(s => s == "E" ? (int?)null : int.Parse(s));
        var items = PaginationHelpers.BuildPageItems(currentPage, totalPages);
        Assert.Equal(expected, items.Select(i => i.Page));
    }

    [Fact]
    public void BuildPageItems_SingleSkippedPage_ShowsPageInsteadOfEllipsis()
    {
        // Between page 1 and page 3 only page 2 is skipped — show it rather than an ellipsis
        // that would only ever replace a single page.
        var items = PaginationHelpers.BuildPageItems(currentPage: 4, totalPages: 5);
        Assert.Equal([1, 2, 3, 4, 5], items.Select(i => i.Page));
    }

    [Fact]
    public void BuildPageItems_OutOfRangeCurrentPage_ClampsWithoutThrowing()
    {
        var items = PaginationHelpers.BuildPageItems(currentPage: 999, totalPages: 5);
        Assert.Equal([1, null, 4, 5], items.Select(i => i.Page));
    }

    [Fact]
    public void BuildSlidingWindow_SinglePage_ReturnsJustPageOne()
    {
        var window = PaginationHelpers.BuildSlidingWindow(1, 1);
        Assert.Equal([1], window);
    }

    [Theory]
    // Current page keeps 3 pages visible ahead of it, so the window slides one step per page...
    [InlineData(10, 23, 4)]
    [InlineData(11, 23, 5)]
    [InlineData(15, 23, 9)]
    // ...until it hits either end, where it clamps instead of running past the page count.
    [InlineData(1, 23, 1)]
    [InlineData(7, 23, 1)]
    [InlineData(8, 23, 2)]
    [InlineData(20, 23, 14)]
    [InlineData(23, 23, 14)]
    public void BuildSlidingWindow_PositionsCurrentPageWithThreeAhead(int currentPage, int totalPages, int expectedFirst)
    {
        var window = PaginationHelpers.BuildSlidingWindow(currentPage, totalPages);
        Assert.Equal(Enumerable.Range(expectedFirst, 10), window);
    }

    [Fact]
    public void BuildSlidingWindow_FewerPagesThanWindow_ReturnsEveryPage()
    {
        var window = PaginationHelpers.BuildSlidingWindow(currentPage: 3, totalPages: 4);
        Assert.Equal(Enumerable.Range(1, 4), window);
    }

    [Fact]
    public void BuildSlidingWindow_OutOfRangePage_ClampsToLastWindow()
    {
        var window = PaginationHelpers.BuildSlidingWindow(currentPage: 999, totalPages: 23);
        Assert.Equal(Enumerable.Range(14, 10), window);
    }

    [Fact]
    public void BuildSlidingWindow_CustomWindowSize_RespectsSize()
    {
        var window = PaginationHelpers.BuildSlidingWindow(currentPage: 1, totalPages: 10, windowSize: 5);
        Assert.Equal(Enumerable.Range(1, 5), window);
    }
}
