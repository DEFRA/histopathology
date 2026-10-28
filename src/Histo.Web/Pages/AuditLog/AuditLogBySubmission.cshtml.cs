using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Replaces <c>AuditLogBySubmission.aspx</c>.</summary>
public class AuditLogBySubmissionModel : GridPageModel
{
    private readonly IAuditLogService _auditLog;

    public AuditLogBySubmissionModel(ISessionService session, IAuditLogService auditLog)
        : base(session) => _auditLog = auditLog;

    [BindProperty] public int?      SubmissionID { get; set; }
    [BindProperty] public DateTime? StartDate    { get; set; }
    [BindProperty] public DateTime? EndDate      { get; set; }

    public IReadOnlyList<AuditLogEntry> Results { get; private set; } = [];
    public List<string> Errors { get; } = [];
    public bool Searched { get; private set; }

    public int TotalCount => Results.Count;

    public IReadOnlyList<AuditLogEntry> GetPagedEntries() =>
        (SortDesc ? Results.OrderByDescending(SortKey(SortColumn)) : Results.OrderBy(SortKey(SortColumn)))
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    private static Func<AuditLogEntry, object?> SortKey(string? column) => column switch
    {
        "FieldName"   => e => e.FieldName,
        "UserName"    => e => e.UserName,
        "TableName"   => e => e.TableName,
        "BeforeValue" => e => e.BeforeValue,
        "AfterValue"  => e => e.AfterValue,
        "Reason"      => e => e.Reason,
        "KeyID"       => e => e.KeyID,
        _             => e => e.ChangedAt,
    };

    public void OnGet()
    {
        ViewData["Title"] = "Audit log by submission";
        ViewData["PageTitle"] = "Audit log — by submission";
        // Pre-populate from session if navigated from a batch context
        if (Session.BatchID.HasValue) SubmissionID = Session.BatchID.Value;
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Audit log by submission";
        ViewData["PageTitle"] = "Audit log — by submission";

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

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as .xlsx.</summary>
    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        if (SubmissionID is null or <= 0) return RedirectToPage();
        var results = await _auditLog.GetBySubmissionAsync(SubmissionID.Value, StartDate, EndDate);
        return ExcelExportHelper.BuildXlsx(
            "SubmissionAuditlog.xlsx",
            ["ID", "Table Name", "Field Name", "Date/Time", "User Name", "Before", "After", "Reason", "Key"],
            results.Select(e => (IReadOnlyList<object?>)new object?[]
            {
                e.ID, e.TableName, e.FieldName, e.ChangedAt, e.UserName,
                e.BeforeValue, e.AfterValue, e.Reason, e.KeyID
            }));
    }
}
