using Microsoft.AspNetCore.Mvc;
using Histo.Web.Services;

namespace Histo.Web.Pages;

/// <summary>
/// Base class for Razor PageModels backing GDS-styled sortable, paginated grids.
///
/// Provides the common sort/paging bind properties (<see cref="SortColumn"/>,
/// <see cref="SortDesc"/>, <see cref="PageNumber"/>) and <see cref="SortBase"/>, the
/// current query string with those three parameters removed — used by the
/// _SortableHeader and _Pagination partials to build links that preserve any other
/// filters/route values already on the request.
///
/// Derived PageModels sort/page their already-loaded in-memory collection (via a
/// switch on <see cref="SortColumn"/>) unless the grid already does server-side/DB
/// paging, in which case sort/paging should be pushed into the query instead.
/// </summary>
public abstract class GridPageModel : HistoPageModel
{
    protected const int PageSize = 10;

    [BindProperty(SupportsGet = true)]
    public string? SortColumn { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SortDesc { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    protected GridPageModel(ISessionService session) : base(session)
    {
    }

    /// <summary>
    /// Current query string minus SortColumn/SortDesc/PageNumber, for building
    /// sort/paging links that preserve any other filters already applied.
    /// </summary>
    public string SortBase =>
        string.Join("&", Request.Query
            .Where(kv => !string.Equals(kv.Key, nameof(SortColumn), StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(kv.Key, nameof(SortDesc), StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(kv.Key, nameof(PageNumber), StringComparison.OrdinalIgnoreCase))
            .SelectMany(kv => kv.Value.Select(v => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(v ?? string.Empty)}")));

    protected static int CalculateTotalPages(int totalCount) =>
        totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PageSize);

    /// <summary>
    /// Populates the ViewData keys read by the _SortableHeader and _Pagination
    /// partials. Call once, before rendering the table, from OnGet/OnGetAsync.
    /// </summary>
    protected void PopulateGridViewData(int totalCount)
    {
        ViewData["SortColumn"] = SortColumn;
        ViewData["SortDesc"]   = SortDesc;
        ViewData["SortBase"]   = SortBase;
        ViewData["CurrentPage"] = PageNumber < 1 ? 1 : PageNumber;
        ViewData["TotalPages"]  = CalculateTotalPages(totalCount);
    }
}
