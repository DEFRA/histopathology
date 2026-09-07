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
}
