using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>Replaces <c>SearchPMDates.aspx</c>.</summary>
public class SearchPMDatesModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;

    public SearchPMDatesModel(ISessionService session, ISubmissionService submissions)
        : base(session) => _submissions = submissions;

    // Legacy ctlFromDate/ctlToDate were CalendarDate controls and both were mandatory
    // (IsDateRangeValid). Blank on first load, matching the legacy page.
    [BindProperty] public DateParts StartDate { get; set; } = new();
    [BindProperty] public DateParts EndDate { get; set; } = new();

    public IReadOnlyList<PmDateSearchResult> Results { get; private set; } = [];
    public bool Searched { get; private set; }

    /// <summary>Field id → message, rendered by the GDS error summary and inline field errors.</summary>
    public Dictionary<string, string> Errors { get; } = [];

    // Sort/page state binds from the query string appended by the POST sort/page buttons,
    // so the POST-bound date criteria are preserved (see _SortableHeaderPost/_PaginationPost).
    private const int PageSize = 10;
    [BindProperty] public string? SortColumn { get; set; }
    [BindProperty] public bool SortDesc { get; set; }
    [BindProperty] public int PageNumber { get; set; } = 1;

    public IReadOnlyList<PmDateSearchResult> PagedResults =>
        (SortColumn switch
        {
            "SenderRef"            => SortDesc ? Results.OrderByDescending(r => r.SenderRef)            : Results.OrderBy(r => r.SenderRef),
            "PMDate"               => SortDesc ? Results.OrderByDescending(r => r.PMDate)               : Results.OrderBy(r => r.PMDate),
            "BatchDate"            => SortDesc ? Results.OrderByDescending(r => r.BatchDate)            : Results.OrderBy(r => r.BatchDate),
            "DateReceived"         => SortDesc ? Results.OrderByDescending(r => r.DateReceived)         : Results.OrderBy(r => r.DateReceived),
            "TimeReceived"         => SortDesc ? Results.OrderByDescending(r => r.TimeReceived)         : Results.OrderBy(r => r.TimeReceived),
            "CompletedDate"        => SortDesc ? Results.OrderByDescending(r => r.CompletedDate)        : Results.OrderBy(r => r.CompletedDate),
            "CustomerReceivedDate" => SortDesc ? Results.OrderByDescending(r => r.CustomerReceivedDate) : Results.OrderBy(r => r.CustomerReceivedDate),
            _                      => SortDesc ? Results.OrderByDescending(r => r.ID)                   : Results.OrderBy(r => r.ID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    private void PopulateGridViewData()
    {
        var totalPages = Results.Count == 0 ? 1 : (int)Math.Ceiling(Results.Count / (double)PageSize);
        if (PageNumber < 1) PageNumber = 1;
        else if (PageNumber > totalPages) PageNumber = totalPages;
        ViewData["SortColumn"] = SortColumn;
        ViewData["SortDesc"] = SortDesc;
        ViewData["CurrentPage"] = PageNumber;
        ViewData["TotalPages"] = totalPages;
        ViewData["FormId"] = "pmdates-action-form";
        ViewData["Handler"] = "Search";
    }

    public void OnGet()
    {
        ViewData["Title"] = "Search PM Dates";
        ViewData["PageTitle"] = "Search by PM Date";
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        ViewData["Title"] = "Search PM Dates";
        ViewData["PageTitle"] = "Search by PM Date";

        if (!TryBuildRange(out var from, out var to)) return Page();

        // Default to Submission number descending until the user explicitly picks a column.
        if (string.IsNullOrEmpty(SortColumn))
        {
            SortColumn = "ID";
            SortDesc = true;
        }

        Results = await _submissions.GetByPmDateRangeAsync(from, to);
        Searched = true;
        PopulateGridViewData();
        return Page();
    }

    /// <summary>Replaces the legacy <c>hlbExcel</c> link. Exports every result row, not just the current page.</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        if (!TryBuildRange(out var from, out var to)) return RedirectToPage("/Search/SearchPMDates");

        var results = await _submissions.GetByPmDateRangeAsync(from, to);
        return CsvExportHelper.BuildCsv(
            "search-pm-dates.csv",
            ["Sub. number", "Sender ref", "PM date", "Date submitted", "Date received / rejected",
             "Time received / rejected", "Date completed", "Customer received date"],
            results.Select(r => (IReadOnlyList<string?>)new string?[]
            {
                r.ID.ToString(),
                r.SenderRef,
                r.PMDate?.ToShortDateString(),
                r.BatchDate?.ToShortDateString(),
                r.DateReceived?.ToShortDateString(),
                r.TimeReceived,
                r.CompletedDate?.ToShortDateString(),
                r.CustomerReceivedDate?.ToShortDateString()
            }));
    }

    /// <summary>Both dates are optional — an unselected date is treated as an open bound, matching
    /// GetSearchPMDates' own NULL-safe defaults. Only real-date and from ≤ to are enforced.</summary>
    private bool TryBuildRange(out DateTime? from, out DateTime? to)
    {
        from = null;
        to = null;

        if (!StartDate.TryGetDate(out var fromValue))
            Errors["StartDate-day"] = "PM from date must be a real date.";

        if (!EndDate.TryGetDate(out var toValue))
            Errors["EndDate-day"] = "PM to date must be a real date.";

        if (Errors.Count > 0) return false;

        if (fromValue.HasValue && toValue.HasValue && fromValue > toValue)
        {
            Errors["StartDate-day"] = "PM from date must not be later than PM to date.";
            return false;
        }

        from = fromValue;
        to = toValue;
        return true;
    }
}
