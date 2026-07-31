using Histo.AuditLog.Models;
using Histo.AuditLog.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogByDate.aspx</c>.</summary>
public class AuditLogByDateModel : HistoPageModel
{
    private readonly AuditLogService _auditLog;

    public AuditLogByDateModel(ISessionService session, AuditLogService auditLog)
        : base(session) => _auditLog = auditLog;

    [BindProperty] public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);
    [BindProperty] public DateTime EndDate   { get; set; } = DateTime.Today;

    public IReadOnlyList<AuditLogEntry> Results { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Audit Log by Date";
        ViewData["PageTitle"] = "Audit Log — Search by Date";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Audit Log by Date";
        ViewData["PageTitle"] = "Audit Log — Search by Date";
        Results = await _auditLog.GetByDateAsync(StartDate, EndDate);
        return Page();
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as CSV.</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var results = await _auditLog.GetByDateAsync(StartDate, EndDate);
        return CsvExportHelper.BuildCsv(
            "AuditLogByDate.csv",
            ["Date", "User", "Action", "Entity", "Detail"],
            results.Select(e => (IReadOnlyList<string?>)new string?[]
            {
                e.ChangedAt.ToShortDateString(), e.UserName, e.Action, $"{e.EntityType} {e.EntityID}", e.Detail
            }));
    }
}
