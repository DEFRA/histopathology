using Histo.AuditLog.Models;
using Histo.AuditLog.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogBySubmission.aspx</c>.</summary>
public class AuditLogBySubmissionModel : HistoPageModel
{
    private readonly AuditLogService _auditLog;

    public AuditLogBySubmissionModel(ISessionService session, AuditLogService auditLog)
        : base(session) => _auditLog = auditLog;

    [BindProperty] public int    SubmissionID { get; set; }
    [BindProperty] public DateTime? StartDate { get; set; }
    [BindProperty] public DateTime? EndDate   { get; set; }

    public IReadOnlyList<AuditLogEntry> Results { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Audit Log by Submission";
        ViewData["PageTitle"] = "Audit Log — By Submission";
        // Pre-populate from session if navigated from a batch context
        if (Session.BatchID.HasValue) SubmissionID = Session.BatchID.Value;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Audit Log by Submission";
        ViewData["PageTitle"] = "Audit Log — By Submission";
        Results = await _auditLog.GetBySubmissionAsync(SubmissionID, StartDate, EndDate);
        return Page();
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as CSV.</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var results = await _auditLog.GetBySubmissionAsync(SubmissionID, StartDate, EndDate);
        return CsvExportHelper.BuildCsv(
            "AuditLogBySubmission.csv",
            ["Date", "User", "Action", "Entity", "Detail"],
            results.Select(e => (IReadOnlyList<string?>)new string?[]
            {
                e.ChangedAt.ToShortDateString(), e.UserName, e.Action, $"{e.EntityType} {e.EntityID}", e.Detail
            }));
    }
}
