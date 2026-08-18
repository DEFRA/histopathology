using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogByDate.aspx</c>.</summary>
public class AuditLogByDateModel : HistoPageModel
{
    private readonly IAuditLogService _auditLog;

    public AuditLogByDateModel(ISessionService session, IAuditLogService auditLog)
        : base(session) => _auditLog = auditLog;

    [BindProperty] public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);
    [BindProperty] public DateTime EndDate   { get; set; } = DateTime.Today;

    public IReadOnlyList<AuditLogEntry> Results { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Audit log by date";
        ViewData["PageTitle"] = "Audit log — search by date";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Audit log by date";
        ViewData["PageTitle"] = "Audit log — search by date";
        Results = await _auditLog.GetByDateAsync(StartDate, EndDate);
        return Page();
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as CSV.</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var results = await _auditLog.GetByDateAsync(StartDate, EndDate);
        return CsvExportHelper.BuildCsv(
            "AuditLogByDate.csv",
            ["Table", "Field", "Date/Time", "User", "Before", "After", "Reason", "Key"],
            results.Select(e => (IReadOnlyList<string?>)new string?[]
            {
                e.TableName, e.FieldName, e.ChangedAt.ToString("dd/MM/yyyy HH:mm:ss"), e.UserName,
                e.BeforeValue, e.AfterValue, e.Reason, e.KeyID
            }));
    }
}
