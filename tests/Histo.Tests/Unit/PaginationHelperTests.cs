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
}
