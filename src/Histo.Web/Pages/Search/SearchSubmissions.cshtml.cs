using Histo.Administration.Interfaces;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>
/// Replaces <c>SearchSubmissions.aspx</c>.
///
/// Filters mirror the legacy page: Status is rendered from the fixed
/// <see cref="BatchStatus"/> constants, Submitted/Entered By are populated from
/// <see cref="UserService.GetAllUsersAsync"/>, and Project, Pathologist, Species,
/// Fixation and Submitted Area are lookup-populated drop-downs matching legacy's
/// <c>ddlProject</c>, <c>ddlContact</c>, <c>ddlSpecies</c>, <c>ddlFixation</c> and
/// <c>ddlUserArea</c>.
///
/// Row selection and action panel: the legacy page enabled 6 action buttons
/// (Print, Edit, View, Quality Data, Archive, Receipt) when a grid row was
/// selected. <see cref="OnPostSelectAsync"/> reproduces this behaviour —
/// it stores <see cref="ISessionService.BatchID"/>, re-runs the search, and
/// returns <c>Page()</c> so the action panel renders with availability driven
/// by batch status, mirroring <c>grdSearchResults_SelectedIndexChanged</c>.
/// </summary>
public class SearchSubmissionsModel : HistoPageModel
{
    private readonly IBatchService _batches;
    private readonly IUserService _users;
    private readonly ILookupService _lookups;

    // Constants matching Common.vb
    private const int LookupFixative = 10;
    private const int LookupUserArea = 13;
    private const int LookupContacts = 18;
    private const int LookupProjects = 19;

    public SearchSubmissionsModel(ISessionService session, IBatchService batches, IUserService users, ILookupService lookups)
        : base(session)
    {
        _batches = batches;
        _users = users;
        _lookups = lookups;
    }

    [BindProperty] public int? SubmissionNumber { get; set; }
    [BindProperty] public string? Status { get; set; }
    [BindProperty] public string? ProjectContractCode { get; set; }
    [BindProperty] public string? ContactName { get; set; }
    [BindProperty] public string? Species { get; set; }
    [BindProperty] public string? Fixation { get; set; }
    [BindProperty] public string? SubmittedArea { get; set; }
    [BindProperty] public int? SubmittedBy { get; set; }
    [BindProperty] public int? EnteredBy { get; set; }
    [BindProperty] public string? HistologyRef { get; set; }
    [BindProperty] public string? SenderRef { get; set; }
    [BindProperty] public DateParts SubmittedDateFrom { get; set; } = new();
    [BindProperty] public DateParts SubmittedDateTo { get; set; } = new();
    [BindProperty] public DateParts ReceivedDateFrom { get; set; } = new();
    [BindProperty] public DateParts ReceivedDateTo { get; set; } = new();

    /// <summary>
    /// ID of the currently selected search result row.
    /// Bound from the "Select" button value in <see cref="OnPostSelectAsync"/>.
    /// Mirrors the legacy <c>grdSearchResults.DataKeys(SelectedIndex)</c> pattern.
    /// </summary>
    [BindProperty] public int SelectedBatchId { get; set; }

    // Sort/page state is bound the same way as the filter criteria — [BindProperty] binds from
    // route/query/form on any non-GET request, so these survive the POST-based sort/page buttons
    // (see _SortableHeaderPost/_PaginationPost) without needing GridPageModel's GET-oriented
    // SupportsGet mechanism, which this POST-only search page cannot use.
    private const int PageSize = 10;
    [BindProperty] public string? SortColumn { get; set; }
    // Legacy FillSearchGrid defaulted the view to "ID DESC" when no sort had been chosen.
    [BindProperty] public bool SortDesc { get; set; } = true;
    [BindProperty] public int PageNumber { get; set; } = 1;

    public IReadOnlyList<BatchSearchResult> PagedResults =>
        (SortColumn switch
        {
            "ProjectDescription" => SortDesc ? Results.OrderByDescending(r => r.ProjectDescription) : Results.OrderBy(r => r.ProjectDescription),
            "ContactDescription" => SortDesc ? Results.OrderByDescending(r => r.ContactDescription) : Results.OrderBy(r => r.ContactDescription),
            "Species"            => SortDesc ? Results.OrderByDescending(r => r.Species)            : Results.OrderBy(r => r.Species),
            "BatchDate"          => SortDesc ? Results.OrderByDescending(r => r.BatchDate)          : Results.OrderBy(r => r.BatchDate),
            "Status"             => SortDesc ? Results.OrderByDescending(r => r.Status)             : Results.OrderBy(r => r.Status),
            _                    => SortDesc ? Results.OrderByDescending(r => r.ID)                 : Results.OrderBy(r => r.ID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    /// <summary>
    /// formaction for each row's Select button. SortColumn/PageNumber are POST-bound (user
    /// controllable), so they're percent-encoded before being embedded in the query string —
    /// otherwise a value containing '&amp;' could inject extra query parameters.
    /// </summary>
    public string SelectFormAction =>
        $"?handler=Select&SortColumn={Uri.EscapeDataString(SortColumn ?? string.Empty)}&SortDesc={(SortDesc ? "true" : "false")}&PageNumber={PageNumber}";

    private void PopulateGridViewData()
    {
        var totalPages = Results.Count == 0 ? 1 : (int)Math.Ceiling(Results.Count / (double)PageSize);
        if (PageNumber < 1) PageNumber = 1;
        else if (PageNumber > totalPages) PageNumber = totalPages;
        ViewData["SortColumn"] = SortColumn;
        ViewData["SortDesc"] = SortDesc;
        ViewData["CurrentPage"] = PageNumber;
        ViewData["TotalPages"] = totalPages;
        ViewData["FormId"] = "submission-action-form";
        ViewData["Handler"] = "Search";
    }

    public IReadOnlyList<Administration.Models.User> Users { get; private set; } = [];
    public IReadOnlyList<Administration.Models.LookupItem> Projects { get; private set; } = [];
    public IReadOnlyList<Administration.Models.LookupItem> Contacts { get; private set; } = [];
    public IReadOnlyList<Administration.Models.LookupItem> SpeciesList { get; private set; } = [];
    public IReadOnlyList<Administration.Models.LookupItem> Fixations { get; private set; } = [];
    public IReadOnlyList<Administration.Models.LookupItem> UserAreas { get; private set; } = [];
    public IReadOnlyList<BatchSearchResult> Results { get; private set; } = [];
    public bool Searched { get; private set; }

    /// <summary>Field id → message, rendered by the GDS error summary and inline field errors.</summary>
    public Dictionary<string, string> Errors { get; } = [];

    /// <summary>
    /// <see cref="BatchStatus"/> code of the selected row, or <c>null</c> when no row is selected.
    /// Evaluated from <see cref="Results"/> after the search re-runs in <see cref="OnPostSelectAsync"/>.
    /// </summary>
    public string? SelectedBatchStatus => Results.FirstOrDefault(r => r.ID == SelectedBatchId)?.Status;

    // ── Action-button availability — mirrors grdSearchResults_SelectedIndexChanged ──────────
    // Submitted("1"): Print only.
    // Completed("4"): Print + View + Quality + Archive + Receipt.
    // Rejected("3"):  View + Receipt.
    // Received/OnHold/InProgress: all six buttons enabled.

    public bool CanPrintSubmission => SelectedBatchStatus is not null && SelectedBatchStatus != BatchStatus.Rejected;
    // Legacy grdSearchResults_SelectedIndexChanged: Edit is disabled for Submitted, Completed and
    // Rejected, and enabled only for the remaining statuses (Received / On Hold / In Progress).
    public bool CanEditSubmission  => SelectedBatchStatus == BatchStatus.Received
                                   || SelectedBatchStatus == BatchStatus.OnHold
                                   || SelectedBatchStatus == BatchStatus.InProgress;
    public bool CanViewSubmission  => SelectedBatchStatus is not null && SelectedBatchStatus != BatchStatus.Submitted;
    public bool CanViewQualityData => SelectedBatchStatus == BatchStatus.Completed
                                   || SelectedBatchStatus == BatchStatus.Received
                                   || SelectedBatchStatus == BatchStatus.OnHold
                                   || SelectedBatchStatus == BatchStatus.InProgress;
    public bool CanViewArchiveData => CanViewQualityData;
    public bool CanViewReceipt     => SelectedBatchStatus is not null && SelectedBatchStatus != BatchStatus.Submitted;

    private async Task LoadLookupsAsync()
    {
        var usersTask = _users.GetAllUsersAsync();
        var projectsTask = _lookups.GetLookupDataAsync(LookupProjects);
        var contactsTask = _lookups.GetLookupDataAsync(LookupContacts);
        var speciesTask = _lookups.GetSpeciesLookupAsync();
        var fixationsTask = _lookups.GetLookupDataAsync(LookupFixative);
        var userAreasTask = _lookups.GetLookupDataAsync(LookupUserArea);

        await Task.WhenAll(usersTask, projectsTask, contactsTask, speciesTask, fixationsTask, userAreasTask);

        Users = await usersTask;
        Projects = await projectsTask;
        Contacts = await contactsTask;
        SpeciesList = await speciesTask;
        Fixations = await fixationsTask;
        UserAreas = await userAreasTask;
    }

    /// <summary>
    /// Reproduces the legacy pre-search checks: <c>revSubmissionNumber</c>
    /// (<c>^[1-9]+[0-9]*$</c>) and the two <c>IsDateRangeValid</c> calls guarding the
    /// Submitted and Received date ranges. All four dates are optional filters.
    /// </summary>
    private bool Validate()
    {
        if (SubmissionNumber is <= 0)
            Errors[nameof(SubmissionNumber)] = "Submission number must be a whole number greater than zero.";

        var submittedFrom = ParseDate(SubmittedDateFrom, "SubmittedDateFrom-day", "Submitted date from");
        var submittedTo = ParseDate(SubmittedDateTo, "SubmittedDateTo-day", "Submitted date to");
        var receivedFrom = ParseDate(ReceivedDateFrom, "ReceivedDateFrom-day", "Received date from");
        var receivedTo = ParseDate(ReceivedDateTo, "ReceivedDateTo-day", "Received date to");

        if (submittedFrom.HasValue && submittedTo.HasValue && submittedFrom > submittedTo)
            Errors["SubmittedDateFrom-day"] = "Submitted date from must not be later than submitted date to.";

        if (receivedFrom.HasValue && receivedTo.HasValue && receivedFrom > receivedTo)
            Errors["ReceivedDateFrom-day"] = "Received date from must not be later than received date to.";

        return Errors.Count == 0;
    }

    private DateTime? ParseDate(DateParts parts, string errorKey, string label)
    {
        if (parts.TryGetDate(out var value)) return value;
        Errors[errorKey] = $"{label} must be a real date.";
        return null;
    }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Search Submissions";
        ViewData["PageTitle"] = "Search Submissions";
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        ViewData["Title"] = "Search Submissions";
        ViewData["PageTitle"] = "Search Submissions";
        await LoadLookupsAsync();
        SelectedBatchId = 0;

        if (!Validate()) return Page();

        Results = await _batches.SearchAsync(BuildCriteria());
        Searched = true;
        PopulateGridViewData();
        return Page();
    }

    /// <summary>
    /// Row selection handler. Persists the selected batch ID in the session so that
    /// downstream pages (BatchDetails, EditBatch, ReceiveBatch, QualityData, ArchiveMenu)
    /// can load the correct batch on their subsequent GET request. Re-runs the search so
    /// the results table and action panel render together in the same response.
    /// </summary>
    public async Task<IActionResult> OnPostSelectAsync()
    {
        ViewData["Title"] = "Search Submissions";
        ViewData["PageTitle"] = "Search Submissions";
        await LoadLookupsAsync();

        if (SelectedBatchId > 0)
        {
            Session.BatchID    = SelectedBatchId;
            Session.ReturnPage = "/Search/SearchSubmissions";  // GAP-3: context-aware back link on BatchDetails
            Session.IsViewSubmissionMode = true;
        }

        Results = await _batches.SearchAsync(BuildCriteria());
        Searched = true;
        PopulateGridViewData();
        return Page();
    }

    /// <summary>
    /// Exports the current search results as a CSV download.
    /// Replaces the legacy <c>lbExportExcel_Click</c> → <c>ExcelExport.aspx</c> pattern.
    /// </summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var results = await _batches.SearchAsync(BuildCriteria());
        return CsvExportHelper.BuildCsv(
            "search-submissions.csv",
            ["Submission number", "Project/Contract", "Pathologist", "Species", "Submitted date", "Status"],
            results.Select(r => (IReadOnlyList<string?>)new string?[]
            {
                r.ID.ToString(),
                r.ProjectDescription,
                r.ContactDescription,
                r.Species,
                r.BatchDate?.ToShortDateString(),
                r.Status
            }));
    }

    private BatchSearchCriteria BuildCriteria() => new()
    {
        SubmissionNumber    = SubmissionNumber,
        Status              = NullIfEmpty(Status),
        ProjectContractCode = NullIfEmpty(ProjectContractCode),
        ContactName         = NullIfEmpty(ContactName),
        Species             = NullIfEmpty(Species),
        Fixation            = NullIfEmpty(Fixation),
        SubmittedArea       = NullIfEmpty(SubmittedArea),
        SubmittedBy         = SubmittedBy,
        EnteredBy           = EnteredBy,
        HistologyRef        = NullIfEmpty(HistologyRef),
        SenderRef           = NullIfEmpty(SenderRef),
        SubmittedDateFrom   = ToDate(SubmittedDateFrom),
        SubmittedDateTo     = ToDate(SubmittedDateTo),
        ReceivedDateFrom    = ToDate(ReceivedDateFrom),
        ReceivedDateTo      = ToDate(ReceivedDateTo),
    };

    private static DateTime? ToDate(DateParts parts) => parts.TryGetDate(out var value) ? value : null;

    // Hidden form sends empty string for null-valued fields; the SP treats "" as a real
    // filter value and returns 0 rows. Convert to null so the SP applies no filter.
    private static string? NullIfEmpty(string? v) => string.IsNullOrWhiteSpace(v) ? null : v;
}
