using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>
/// Replaces <c>SearchPMDates.aspx</c>.
///
/// GET-based (all filters/sort/page bound via <c>SupportsGet</c>): results, sort order and
/// paging all live in the query string, so they're restored correctly by the browser Back
/// button and are bookmarkable/shareable — matching the established pattern already used by
/// <see cref="SearchSubmissionsModel"/>/<see cref="SearchBlockRefsModel"/>. Previously this
/// page used a POST form, which loses all of this on Back (browser history only caches GET
/// responses).
/// </summary>
public class SearchPMDatesModel : GridPageModel
{
    private readonly ISubmissionService _submissions;

    public SearchPMDatesModel(ISessionService session, ISubmissionService submissions)
        : base(session) => _submissions = submissions;

    // Legacy ctlFromDate/ctlToDate were CalendarDate controls and both were mandatory
    // (IsDateRangeValid). Blank on first load, matching the legacy page.
    [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }

    // Distinguishes "user clicked Search with nothing filled in" from a first, bare page
    // visit — both would otherwise look identical (no query string at all).
    [BindProperty(SupportsGet = true)] public bool Submitted { get; set; }

    public IReadOnlyList<PmDateSearchResult> Results { get; private set; } = [];
    public bool Searched { get; private set; }

    /// <summary>Field id → message, rendered by the GDS error summary and inline field errors.</summary>
    public Dictionary<string, string> Errors { get; } = [];

    [BindProperty(SupportsGet = true)] public string? ReturnPage { get; set; }

    /// <summary>Only ever redirect to a path inside this application — blocks open-redirect abuse.</summary>
    public string BackLinkPage =>
        !string.IsNullOrWhiteSpace(ReturnPage) && Url.IsLocalUrl(ReturnPage) ? ReturnPage : "/Search/SearchMenu";

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

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Search PM Dates";
        ViewData["PageTitle"] = "Search by PM Date";

        // A bare first visit (no query string) shows the empty form only — matches the
        // previous GET/POST split, now distinguished by the Submitted marker instead.
        if (!Submitted)
        {
            PopulateGridViewData(0);
            return;
        }

        if (!TryBuildRange(out var from, out var to))
        {
            PopulateGridViewData(0);
            return;
        }

        // Default to Submission number descending until the user explicitly picks a column.
        if (string.IsNullOrEmpty(SortColumn))
        {
            SortColumn = "ID";
            SortDesc = true;
        }

        Results = await _submissions.GetByPmDateRangeAsync(from, to);
        Searched = true;
        PopulateGridViewData(Results.Count);
    }

    /// <summary>Replaces the legacy <c>hlbExcel</c> link. Exports every result row, not just the current page.</summary>
    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        if (!TryBuildRange(out var from, out var to))
            return RedirectToPage("/Search/SearchPMDates");

        var results = await _submissions.GetByPmDateRangeAsync(from, to);
        return ExcelExportHelper.BuildXlsx(
            "search-pm-dates.xlsx",
            ["Sub. number", "Sender ref", "PM date", "Date submitted", "Date received / rejected",
             "Time received / rejected", "Date completed", "Customer received date"],
            results.Select(r => (IReadOnlyList<object?>)new object?[]
            {
                r.ID,
                r.SenderRef,
                r.PMDate,
                r.BatchDate,
                r.DateReceived,
                r.TimeReceived,
                r.CompletedDate,
                r.CustomerReceivedDate
            }));
    }

    /// <summary>Both dates are optional — an unselected date is treated as an open bound, matching
    /// GetSearchPMDates' own NULL-safe defaults. Only from ≤ to is enforced (the browser date
    /// picker only ever submits a real date or nothing).</summary>
    private bool TryBuildRange(out DateTime? from, out DateTime? to)
    {
        from = StartDate;
        to = EndDate;

        if (ModelState[nameof(StartDate)]?.Errors.Count > 0)
            Errors[nameof(StartDate)] = "PM from date must be a real date.";
        if (ModelState[nameof(EndDate)]?.Errors.Count > 0)
            Errors[nameof(EndDate)] = "PM to date must be a real date.";
        if (Errors.Count > 0) return false;

        if (from.HasValue && to.HasValue && from > to)
        {
            Errors["StartDate"] = "PM from date must not be later than PM to date.";
            return false;
        }

        return true;
    }
}
