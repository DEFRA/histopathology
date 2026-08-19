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
/// SIMPLIFIED: the legacy page drives several of its filters (Status,
/// Submitted Area, Submitted By, Project, Pathologist, Species, Fixation)
/// from lookup-populated drop-downs and hides a "SubmittedBy" grid column.
/// Status is rendered from the fixed <see cref="BatchStatus"/> constants,
/// Submitted/Entered By are populated from <see cref="UserService.GetAllUsersAsync"/>,
/// and the remaining filters are plain text fields matching the string
/// criteria already accepted by <see cref="BatchSearchCriteria"/>.
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

    public SearchSubmissionsModel(ISessionService session, IBatchService batches, IUserService users)
        : base(session)
    {
        _batches = batches;
        _users = users;
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
    [BindProperty] public DateTime? SubmittedDateFrom { get; set; }
    [BindProperty] public DateTime? SubmittedDateTo { get; set; }
    [BindProperty] public DateTime? ReceivedDateFrom { get; set; }
    [BindProperty] public DateTime? ReceivedDateTo { get; set; }

    /// <summary>
    /// ID of the currently selected search result row.
    /// Bound from the "Select" button value in <see cref="OnPostSelectAsync"/>.
    /// Mirrors the legacy <c>grdSearchResults.DataKeys(SelectedIndex)</c> pattern.
    /// </summary>
    [BindProperty] public int SelectedBatchId { get; set; }

    public IReadOnlyList<Administration.Models.User> Users { get; private set; } = [];
    public IReadOnlyList<BatchSearchResult> Results { get; private set; } = [];
    public bool Searched { get; private set; }

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
    // Edit test types — Submitted, Received, or InProgress only (matches CanEditTestTypes on BatchDetails).
    public bool CanEditTestTypes   => SelectedBatchStatus == BatchStatus.Submitted
                                   || SelectedBatchStatus == BatchStatus.Received
                                   || SelectedBatchStatus == BatchStatus.InProgress;

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Search Submissions";
        ViewData["PageTitle"] = "Search Submissions";
        Users = await _users.GetAllUsersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Search Submissions";
        ViewData["PageTitle"] = "Search Submissions";
        Users = await _users.GetAllUsersAsync();
        SelectedBatchId = 0;
        Results = await _batches.SearchAsync(BuildCriteria());
        Searched = true;
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
        Users = await _users.GetAllUsersAsync();

        if (SelectedBatchId > 0)
        {
            Session.BatchID    = SelectedBatchId;
            Session.ReturnPage = "/Search/SearchSubmissions";  // GAP-3: context-aware back link on BatchDetails
        }

        Results = await _batches.SearchAsync(BuildCriteria());
        Searched = true;
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
        SubmittedArea       = SubmittedArea,
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
