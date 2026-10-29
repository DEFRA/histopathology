using Histo.AuditLog.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogBySubmission.aspx</c>.</summary>
public class AuditLogBySubmissionModel : AuditLogPageModelBase
{
    private readonly IAuditLogService _auditLog;

    public AuditLogBySubmissionModel(ISessionService session, IAuditLogService auditLog)
        : base(session) => _auditLog = auditLog;

    [BindProperty] public int?      SubmissionID { get; set; }
    [BindProperty] public DateTime? StartDate    { get; set; }
    [BindProperty] public DateTime? EndDate      { get; set; }

    public void OnGet()
    {
        SetTitles("Audit log by submission", "Audit log — by submission");
        // Pre-populate from session if navigated from a batch context
        if (Session.BatchID.HasValue) SubmissionID = Session.BatchID.Value;
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitles("Audit log by submission", "Audit log — by submission");

        if (SubmissionID is null or <= 0) Errors.Add("Enter a submission number.");
        if (Errors.Count > 0)
        {
            PopulateGridViewData(TotalCount);
            return Page();
        }

        Results = await _auditLog.GetBySubmissionAsync(SubmissionID!.Value, StartDate, EndDate);
        Searched = true;
        PopulateGridViewData(TotalCount);
        return Page();
    }

    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        if (SubmissionID is null or <= 0) return RedirectToPage();
        var results = await _auditLog.GetBySubmissionAsync(SubmissionID.Value, StartDate, EndDate);
        return BuildExport("SubmissionAuditlog.xlsx", results);
    }
}