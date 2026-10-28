using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogByUser.aspx</c>.</summary>
public class AuditLogByUserModel : GridPageModel
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
    public List<string> Errors { get; } = [];
    public bool Searched { get; private set; }

    public int TotalCount => Results.Count;

    public IReadOnlyList<AuditLogEntry> GetPagedEntries() =>
        (SortColumn switch
        {
            "FieldName"   => OrderResults(e => e.FieldName),
            "UserName"    => OrderResults(e => e.UserName),
            "TableName"   => OrderResults(e => e.TableName),
            "BeforeValue" => OrderResults(e => e.BeforeValue),
            "AfterValue"  => OrderResults(e => e.AfterValue),
            "Reason"      => OrderResults(e => e.Reason),
            "KeyID"       => OrderResults(e => e.KeyID),
            _             => OrderResults(e => e.ChangedAt),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    private IOrderedEnumerable<AuditLogEntry> OrderResults<TKey>(Func<AuditLogEntry, TKey> key) =>
        SortDesc ? Results.OrderByDescending(key) : Results.OrderBy(key);

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Audit log by user";
        ViewData["PageTitle"] = "Audit log — by user";
        Users = await _users.GetAllUsersAsync();
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Audit log by user";
        ViewData["PageTitle"] = "Audit log — by user";
        Users = await _users.GetAllUsersAsync();

        if (UserID <= 0)          Errors.Add("Select a user.");
        if (!StartDate.HasValue)  Errors.Add("Enter a start date.");
        if (!EndDate.HasValue)    Errors.Add("Enter an end date.");
        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
            Errors.Add("The end date must be the same as or after the start date.");
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

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as .xlsx.</summary>
    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        var results = await _auditLog.GetByUserAsync(UserID, StartDate, EndDate);
        return ExcelExportHelper.BuildXlsx(
            "UserAuditlogResult.xlsx",
            ["ID", "Table Name", "Field Name", "Date/Time", "User Name", "Before", "After", "Reason", "Key"],
            results.Select(e => (IReadOnlyList<object?>)new object?[]
            {
                e.ID, e.TableName, e.FieldName, e.ChangedAt, e.UserName,
                e.BeforeValue, e.AfterValue, e.Reason, e.KeyID
            }));
    }
}
