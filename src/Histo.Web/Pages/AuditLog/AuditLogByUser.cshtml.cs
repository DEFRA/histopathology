using Histo.AuditLog.Models;
using Histo.AuditLog.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogByUser.aspx</c>.</summary>
public class AuditLogByUserModel : HistoPageModel
{
    private readonly AuditLogService _auditLog;

    public AuditLogByUserModel(ISessionService session, AuditLogService auditLog)
        : base(session) => _auditLog = auditLog;

    [BindProperty] public int       UserID    { get; set; }
    [BindProperty] public DateTime? StartDate { get; set; }
    [BindProperty] public DateTime? EndDate   { get; set; }

    public IReadOnlyList<AuditLogEntry> Results { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Audit Log by User";
        ViewData["PageTitle"] = "Audit Log — By User";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Audit Log by User";
        ViewData["PageTitle"] = "Audit Log — By User";
        Results = await _auditLog.GetByUserAsync(UserID, StartDate, EndDate);
        return Page();
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as CSV.</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var results = await _auditLog.GetByUserAsync(UserID, StartDate, EndDate);
        return CsvExportHelper.BuildCsv(
            "AuditLogByUser.csv",
            ["Date", "User", "Action", "Entity", "Detail"],
            results.Select(e => (IReadOnlyList<string?>)new string?[]
            {
                e.ChangedAt.ToShortDateString(), e.UserName, e.Action, $"{e.EntityType} {e.EntityID}", e.Detail
            }));
    }
}
