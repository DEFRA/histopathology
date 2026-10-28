using Histo.AuditLog.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogByDate.aspx</c>.</summary>
public class AuditLogByDateModel : AuditLogPageModelBase
{
    private readonly IAuditLogService _auditLog;

    public AuditLogByDateModel(ISessionService session, IAuditLogService auditLog)
        : base(session) => _auditLog = auditLog;

    // Legacy AuditLogByDate.aspx does not pre-populate dates — fields are blank on first load.
    [BindProperty] public DateTime? StartDate { get; set; }
    [BindProperty] public DateTime? EndDate   { get; set; }

    public void OnGet()
    {
        SetTitles("Audit log by date", "Audit log — search by date");
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitles("Audit log by date", "Audit log — search by date");

        AddDateRangeErrors(StartDate, EndDate);
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

    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        if (!StartDate.HasValue || !EndDate.HasValue) return RedirectToPage();
        var results = await _auditLog.GetByDateAsync(StartDate.Value, EndDate.Value);
        return BuildExport("AuditLogByDate.xlsx", results);
    }
}