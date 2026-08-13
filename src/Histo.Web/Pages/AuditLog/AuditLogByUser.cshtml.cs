using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogByUser.aspx</c>.</summary>
public class AuditLogByUserModel : HistoPageModel
{
    private readonly IAuditLogService _auditLog;
    private readonly IUserService _users;

    public AuditLogByUserModel(ISessionService session, IAuditLogService auditLog, IUserService users)
        : base(session)
    {
        _auditLog = auditLog;
        _users    = users;
    }

    [BindProperty] public int       UserID    { get; set; }
    [BindProperty] public DateTime? StartDate { get; set; }
    [BindProperty] public DateTime? EndDate   { get; set; }

    public IReadOnlyList<User> Users { get; private set; } = [];
    public IReadOnlyList<AuditLogEntry> Results { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Audit log by user";
        ViewData["PageTitle"] = "Audit log — by user";
        Users = await _users.GetAllUsersAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Audit log by user";
        ViewData["PageTitle"] = "Audit log — by user";
        Users = await _users.GetAllUsersAsync();
        Results = await _auditLog.GetByUserAsync(UserID, StartDate, EndDate);
        return Page();
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as CSV.</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var results = await _auditLog.GetByUserAsync(UserID, StartDate, EndDate);
        return CsvExportHelper.BuildCsv(
            "AuditLogByUser.csv",
            ["Table", "Field", "Date/Time", "User", "Before", "After", "Reason", "Key"],
            results.Select(e => (IReadOnlyList<string?>)new string?[]
            {
                e.TableName, e.FieldName, e.ChangedAt.ToString("G"), e.UserName,
                e.BeforeValue, e.AfterValue, e.Reason, e.KeyID
            }));
    }
}
