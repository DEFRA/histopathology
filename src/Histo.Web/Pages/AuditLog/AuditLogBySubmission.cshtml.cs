using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogBySubmission.aspx</c>.</summary>
public class AuditLogBySubmissionModel : GridPageModel
{
    private readonly IAuditLogService _auditLog;

    public AuditLogBySubmissionModel(ISessionService session, IAuditLogService auditLog)
        : base(session) => _auditLog = auditLog;

    [BindProperty] public int      SubmissionID { get; set; }
    [BindProperty] public DateTime? StartDate    { get; set; }
    [BindProperty] public DateTime? EndDate      { get; set; }

    public IReadOnlyList<AuditLogEntry> Results { get; private set; } = [];
    public List<string> Errors { get; } = [];

    public int TotalCount => Results.Count;

    public IReadOnlyList<AuditLogEntry> PagedEntries =>
        (SortColumn switch
        {
            "FieldName" => SortDesc ? Results.OrderByDescending(e => e.FieldName) : Results.OrderBy(e => e.FieldName),
            "UserName"  => SortDesc ? Results.OrderByDescending(e => e.UserName)  : Results.OrderBy(e => e.UserName),
            "TableName" => SortDesc ? Results.OrderByDescending(e => e.TableName) : Results.OrderBy(e => e.TableName),
            _           => SortDesc ? Results.OrderByDescending(e => e.ChangedAt) : Results.OrderBy(e => e.ChangedAt),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public void OnGet()
    {
        ViewData["Title"] = "Audit log by submission";
        ViewData["PageTitle"] = "Audit log — by submission";
        // Pre-populate from session if navigated from a batch context
        if (Session.BatchID.HasValue) SubmissionID = Session.BatchID.Value;
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Audit log by submission";
        ViewData["PageTitle"] = "Audit log — by submission";

        if (SubmissionID <= 0) Errors.Add("Enter a submission number.");
        if (Errors.Count > 0)
        {
            PopulateGridViewData(TotalCount);
            return Page();
        }

        Results = await _auditLog.GetBySubmissionAsync(SubmissionID, StartDate, EndDate);
        PopulateGridViewData(TotalCount);
        return Page();
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as CSV.</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var results = await _auditLog.GetBySubmissionAsync(SubmissionID, StartDate, EndDate);
        return CsvExportHelper.BuildCsv(
            "AuditLogBySubmission.csv",
            ["Table", "Field", "Date/Time", "User", "Before", "After", "Reason", "Key"],
            results.Select(e => (IReadOnlyList<string?>)new string?[]
            {
                e.TableName, e.FieldName, e.ChangedAt.ToString("dd/MM/yyyy HH:mm:ss"), e.UserName,
                e.BeforeValue, e.AfterValue, e.Reason, e.KeyID
            }));
    }
}
