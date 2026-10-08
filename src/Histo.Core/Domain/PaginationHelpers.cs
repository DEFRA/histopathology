namespace Histo.Core.Domain;

/// <summary>
/// Builds the GOV.UK Design System pagination "items" list — current page, at least one
/// neighbouring page either side, first and last page, with ellipses for any gaps.
/// https://design-system.service.gov.uk/components/pagination/
/// </summary>
public static class PaginationHelpers
{
    /// <summary>A page number entry, or an ellipsis when <see cref="Page"/> is null.</summary>
    public readonly record struct PaginationItem(int? Page)
    {
        public bool IsEllipsis => Page is null;
    }

    public static IReadOnlyList<PaginationItem> BuildPageItems(int currentPage, int totalPages)
    {
        if (totalPages <= 1) return [new PaginationItem(1)];

        var current = Math.Clamp(currentPage, 1, totalPages);
        var shown = new SortedSet<int> { 1, totalPages, current };
        if (current - 1 >= 1) shown.Add(current - 1);
        if (current + 1 <= totalPages) shown.Add(current + 1);

        var items = new List<PaginationItem>();
        var previous = 0;
        foreach (var page in shown)
        {
            if (previous != 0)
            {
                // Exactly one skipped page — show it rather than an ellipsis that would only
                // ever replace a single page. Two or more skipped — collapse to an ellipsis.
                if (page - previous == 2)
                    items.Add(new PaginationItem(previous + 1));
                else if (page - previous > 2)
                    items.Add(new PaginationItem(null));
            }

            items.Add(new PaginationItem(page));
            previous = page;
        }

        return items;
    }

    /// <summary>
    /// Builds the sliding window of page numbers shown between the First/Previous and Next/Last
    /// controls: always <paramref name="windowSize"/> consecutive pages (or every page, when there
    /// are fewer), positioned so the current page has <paramref name="pagesAfterCurrent"/> pages
    /// visible ahead of it — e.g. page 10 of 21 shows 4-13. The window is clamped at both ends, so
    /// the first and last pages show 1-10 and 12-21 rather than running past the page count.
    /// </summary>
    /// <param name="currentPage">The current 1-based page number.</param>
    /// <param name="totalPages">The total number of pages.</param>
    /// <param name="windowSize">How many page numbers to show (default 10).</param>
    /// <param name="pagesAfterCurrent">How many pages to keep visible ahead of the current page (default 3).</param>
    public static IReadOnlyList<int> BuildSlidingWindow(int currentPage, int totalPages, int windowSize = 10, int pagesAfterCurrent = 3)
    {
        if (totalPages <= 1) return [1];

        var current = Math.Clamp(currentPage, 1, totalPages);
        var size = Math.Min(windowSize, totalPages);
        var start = Math.Clamp(current + pagesAfterCurrent - size + 1, 1, totalPages - size + 1);

        var items = new List<int>(size);
        for (var i = 0; i < size; i++)
            items.Add(start + i);
        return items;
    }
}
