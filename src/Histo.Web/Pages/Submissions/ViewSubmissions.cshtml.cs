using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>
/// Submission search page — replaces <c>ViewSubmissions.aspx</c>.
/// Provides the full multi-field batch search with dropdown-populated filters
/// matching the legacy page (Project, Pathologist, Species, Fixation dropdowns
/// from lookup SPs; Users dropdown from UserService; Clear Search link).
///
/// Row selection and action panel: the legacy page enabled 6 action buttons
/// (Print Submission, Print Submission Notes, Copy Submission, Edit Submission,
/// View Submission, Date Returned) on row click, with availability gated by
/// batch status (<c>grdviewResults_SelectedIndexChanged</c>).
/// <see cref="OnPostSelectAsync"/> reproduces this behaviour — it stores
/// <see cref="ISessionService.BatchID"/>, re-runs the search and returns
/// <c>Page()</c> so the action panel renders below the results table.
/// </summary>
public class ViewSubmissionsModel : HistoPageModel
{
    private readonly IBatchService   _batches;
    private readonly IUserService    _users;
    private readonly ILookupService  _lookups;

    // Constants matching Common.vb
    private const int LookupFixative = 10;
    private const int LookupContacts = 18;
    private const int LookupProjects = 19;

    public ViewSubmissionsModel(ISessionService session, IBatchService batches, IUserService users, ILookupService lookups)
        : base(session)
    {
        _batches = batches;
        _users   = users;
        _lookups = lookups;
    }

    [BindProperty] public int?      SubmissionNumber    { get; set; }
    [BindProperty] public string?   Status              { get; set; }
    [BindProperty] public string?   ProjectContractCode { get; set; }
    [BindProperty] public string?   ContactName         { get; set; }
    [BindProperty] public string?   Species             { get; set; }
    [BindProperty] public string?   Fixation            { get; set; }
    [BindProperty] public int?      SubmittedBy         { get; set; }
    [BindProperty] public int?      EnteredBy           { get; set; }
    [BindProperty] public string?   HistologyRef        { get; set; }
    [BindProperty] public string?   SenderRef           { get; set; }
    [BindProperty] public DateTime? SubmittedDateFrom   { get; set; }
    [BindProperty] public DateTime? SubmittedDateTo     { get; set; }
    [BindProperty] public DateTime? ReceivedDateFrom    { get; set; }
    [BindProperty] public DateTime? ReceivedDateTo      { get; set; }

    // Sort/page state is bound the same way as the filter criteria above — [BindProperty]
    // binds from route/query/form on any non-GET request, so these survive the POST-based
    // sort/page buttons (see _SortableHeaderPost/_PaginationPost) without needing GridPageModel's
    // GET-oriented SupportsGet mechanism, which this POST-only search page cannot use.
    private const int PageSize = 10;
    [BindProperty] public string? SortColumn { get; set; }
    [BindProperty] public bool    SortDesc   { get; set; }
    [BindProperty] public int     PageNumber { get; set; } = 1;

    public IReadOnlyList<BatchSearchResult> PagedResults =>
        (SortColumn switch
        {
            "ID"                 => SortDesc ? Results.OrderByDescending(r => r.ID)                  : Results.OrderBy(r => r.ID),
            "ProjectDescription" => SortDesc ? Results.OrderByDescending(r => r.ProjectDescription) : Results.OrderBy(r => r.ProjectDescription),
            "ContactDescription" => SortDesc ? Results.OrderByDescending(r => r.ContactDescription) : Results.OrderBy(r => r.ContactDescription),
            "Species"            => SortDesc ? Results.OrderByDescending(r => r.Species)            : Results.OrderBy(r => r.Species),
            "BatchDate"          => SortDesc ? Results.OrderByDescending(r => r.BatchDate)           : Results.OrderBy(r => r.BatchDate),
            "DateReceived"       => SortDesc ? Results.OrderByDescending(r => r.DateReceived)        : Results.OrderBy(r => r.DateReceived),
            "DateCompleted"      => SortDesc ? Results.OrderByDescending(r => r.DateCompleted)       : Results.OrderBy(r => r.DateCompleted),
            "Status"             => SortDesc ? Results.OrderByDescending(r => r.Status)              : Results.OrderBy(r => r.Status),
            _                    => SortDesc ? Results.OrderByDescending(r => r.ID)                  : Results.OrderBy(r => r.ID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    private void PopulateGridViewData()
    {
        ViewData["SortColumn"]  = SortColumn;
        ViewData["SortDesc"]    = SortDesc;
        ViewData["CurrentPage"] = PageNumber < 1 ? 1 : PageNumber;
        ViewData["TotalPages"]  = Results.Count == 0 ? 1 : (int)Math.Ceiling(Results.Count / (double)PageSize);
        ViewData["FormId"]      = "view-action-form";
        ViewData["Handler"]     = "Search";
    }

    /// <summary>
    /// ID of the currently selected result row.
    /// Bound from the per-row Select button value via <see cref="OnPostSelectAsync"/>.
    /// Mirrors legacy <c>grdviewResults.DataKeys(SelectedIndex)</c>.
    /// </summary>
    [BindProperty] public int SelectedBatchId { get; set; }

    public IReadOnlyList<User>              Users       { get; private set; } = [];
    public IReadOnlyList<LookupItem>        Projects    { get; private set; } = [];
    public IReadOnlyList<LookupItem>        Contacts    { get; private set; } = [];
    public IReadOnlyList<LookupItem>        SpeciesList { get; private set; } = [];
    public IReadOnlyList<LookupItem>        Fixations   { get; private set; } = [];
    public IReadOnlyList<BatchSearchResult> Results     { get; private set; } = [];
    public bool Searched { get; private set; }

    /// <summary>
    /// <see cref="BatchStatus"/> code of the selected row, or <c>null</c> when no row selected.
    /// Evaluated after search re-runs in <see cref="OnPostSelectAsync"/>.
    /// </summary>
    public string? SelectedBatchStatus => Results.FirstOrDefault(r => r.ID == SelectedBatchId)?.Status;

    // ── Action-button availability — mirrors grdviewResults_SelectedIndexChanged ──────────────
    // Submitted("1") or Rejected("3"): Edit ✓, View ✓, Copy ✓, DateReturned ✗
    // Completed("4"):                  Edit ✗, View ✓, Copy ✓, DateReturned ✓
    // Received/OnHold/InProgress:      Edit ✗, View ✓, Copy ✓, DateReturned ✗

    public bool CanEditSubmission  => SelectedBatchStatus == BatchStatus.Submitted
                                   || SelectedBatchStatus == BatchStatus.Rejected;
    public bool CanViewSubmission  => SelectedBatchStatus is not null;
    public bool CanCopySubmission  => SelectedBatchStatus is not null;
    public bool CanDateReturned    => SelectedBatchStatus == BatchStatus.Completed;
    // Edit test types — Submitted, Received, or InProgress only (matches CanEditTestTypes on BatchDetails).
    public bool CanEditTestTypes   => SelectedBatchStatus == BatchStatus.Submitted
                                   || SelectedBatchStatus == BatchStatus.Received
                                   || SelectedBatchStatus == BatchStatus.InProgress;

    private async Task LoadLookupsAsync()
    {
        Users       = await _users.GetAllUsersAsync();
        Projects    = await _lookups.GetLookupDataAsync(LookupProjects);
        Contacts    = await _lookups.GetLookupDataAsync(LookupContacts);
        SpeciesList = await _lookups.GetSpeciesLookupAsync();
        Fixations   = await _lookups.GetLookupDataAsync(LookupFixative);
    }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "View submissions";
        ViewData["PageTitle"] = "View submissions";
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        ViewData["Title"] = "View submissions";
        ViewData["PageTitle"] = "View submissions";
        await LoadLookupsAsync();
        SelectedBatchId = 0;
        Results  = await _batches.SearchAsync(BuildCriteria());
        Searched = true;
        PopulateGridViewData();
        return Page();
    }

    /// <summary>
    /// Row selection handler. Stores the selected batch ID in session so that
    /// downstream pages (BatchDetails, EditBatch, CopyBatch, ReceiveBatch) load
    /// the correct batch on their next GET. Re-runs the search so the results
    /// table and action panel render together in the same response, matching
    /// the legacy <c>grdviewResults_SelectedIndexChanged</c> postback behaviour.
    /// </summary>
    public async Task<IActionResult> OnPostSelectAsync()
    {
        ViewData["Title"] = "View submissions";
        ViewData["PageTitle"] = "View submissions";
        await LoadLookupsAsync();

        if (SelectedBatchId > 0)
        {
            Session.BatchID     = SelectedBatchId;
            Session.ReturnPage  = "/Submissions/ViewSubmissions";  // GAP-3: context-aware back link on BatchDetails
            Session.IsViewSubmissionMode = true;
        }

        Results  = await _batches.SearchAsync(BuildCriteria());
        Searched = true;
        PopulateGridViewData();
        return Page();
    }

    /// <summary>
    /// CSV export — replaces legacy <c>lbExportExcel_Click</c> → <c>ExcelExport.aspx</c> pattern.
    /// </summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var results = await _batches.SearchAsync(BuildCriteria());
        return CsvExportHelper.BuildCsv(
            "view-submissions.csv",
            ["Submission number", "Project/Contract", "Pathologist", "Species",
             "Date submitted", "Date received/rejected", "Date completed",
             "Customer received date", "Status"],
            results.Select(r => (IReadOnlyList<string?>)new string?[]
            {
                r.ID.ToString(),
                r.ProjectDescription,
                r.ContactDescription,
                r.Species,
                r.BatchDate?.ToShortDateString(),
                r.DateReceived?.ToShortDateString(),
                r.DateCompleted?.ToShortDateString(),
                r.CustomerReceivedDate?.ToShortDateString(),
                BatchStatus.DisplayName(r.Status ?? "")
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
        SubmittedBy         = SubmittedBy,
        EnteredBy           = EnteredBy,
        HistologyRef        = NullIfEmpty(HistologyRef),
        SenderRef           = NullIfEmpty(SenderRef),
        SubmittedDateFrom   = SubmittedDateFrom,
        SubmittedDateTo     = SubmittedDateTo,
        ReceivedDateFrom    = ReceivedDateFrom,
        ReceivedDateTo      = ReceivedDateTo,
    };

    // Hidden form sends empty string for null-valued fields; the SP treats "" as a real
    // filter value and returns 0 rows. Convert to null so the SP applies no filter.
    private static string? NullIfEmpty(string? v) => string.IsNullOrWhiteSpace(v) ? null : v;
}
