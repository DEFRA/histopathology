using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.AuditLog.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogByUser.aspx</c>.</summary>
public class AuditLogByUserModel : AuditLogPageModelBase
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

    public async Task OnGetAsync()
    {
        SetTitles("Audit log by user", "Audit log — by user");
        Users = await _users.GetAllUsersAsync();
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitles("Audit log by user", "Audit log — by user");
        Users = await _users.GetAllUsersAsync();

        if (UserID <= 0) Errors.Add("Select a user.");
        AddDateRangeErrors(StartDate, EndDate);
        if (Errors.Count > 0)
        {
            PopulateGridViewData(TotalCount);
            return Page();
        }

        Results = await _auditLog.GetByUserAsync(UserID, StartDate, EndDate);
        Searched = true;
        PopulateGridViewData(TotalCount);
        return Page();
    }

    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        var results = await _auditLog.GetByUserAsync(UserID, StartDate, EndDate);
        return BuildExport("UserAuditlogResult.xlsx", results);
    }
}