using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogByDate.aspx</c>.</summary>
public class AuditLogByDateModel : GridPageModel
{
    private readonly IAuditLogService _auditLog;

    public AuditLogByDateModel(ISessionService session, IAuditLogService auditLog)
        : base(session) => _auditLog = auditLog;

    // Legacy AuditLogByDate.aspx does not pre-populate dates — fields are blank on first load.
    [BindProperty] public DateTime? StartDate { get; set; }
    [BindProperty] public DateTime? EndDate   { get; set; }

    public IReadOnlyList<AuditLogEntry> Results { get; private set; } = [];
    public bool Searched { get; private set; }
    public List<string> Errors { get; } = [];

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

    public void OnGet()
    {
        ViewData["Title"] = "Audit log by date";
        ViewData["PageTitle"] = "Audit log — search by date";
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Audit log by date";
        ViewData["PageTitle"] = "Audit log — search by date";

        if (!StartDate.HasValue) Errors.Add("Enter a start date.");
        if (!EndDate.HasValue)   Errors.Add("Enter an end date.");
        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
            Errors.Add("The end date must be the same as or after the start date.");
        if (Errors.Count > 0)
        {
            PopulateGridViewData(TotalCount);
            return Page();
        }

        Searched = true;
        Results = await _auditLog.GetByDateAsync(StartDate!.Value, EndDate!.Value);
        PopulateGridViewData(TotalCount);
        return Page();
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as .xlsx.</summary>
    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        if (!StartDate.HasValue || !EndDate.HasValue) return RedirectToPage();
        var results = await _auditLog.GetByDateAsync(StartDate.Value, EndDate.Value);
        return ExcelExportHelper.BuildXlsx(
            "AuditLogByDate.xlsx",
            ["ID", "Table Name", "Field Name", "Date/Time", "User Name", "Before", "After", "Reason", "Key"],
            results.Select(e => (IReadOnlyList<object?>)new object?[]
            {
                e.ID, e.TableName, e.FieldName, e.ChangedAt, e.UserName,
                e.BeforeValue, e.AfterValue, e.Reason, e.KeyID
            }));
    }
}
