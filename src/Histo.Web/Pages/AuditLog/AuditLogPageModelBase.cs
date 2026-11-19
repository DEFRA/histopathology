using Histo.AuditLog.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.AuditLog;

/// <summary>Result grid, sorting, date-range validation and Excel export shared by the AuditLogBy* pages.</summary>
public abstract class AuditLogPageModelBase : GridPageModel
{
    protected AuditLogPageModelBase(ISessionService session) : base(session) { }

    public IReadOnlyList<AuditLogEntry> Results { get; protected set; } = [];
    public bool Searched { get; protected set; }
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

    protected void SetTitles(string title, string pageTitle)
    {
        ViewData["Title"] = title;
        ViewData["PageTitle"] = pageTitle;
    }

    protected void AddDateRangeErrors(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue) Errors.Add("Enter a start date.");
        if (!endDate.HasValue)   Errors.Add("Enter an end date.");
        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
            Errors.Add("The end date must be the same as or after the start date.");
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the given results as .xlsx.</summary>
    protected static FileContentResult BuildExport(string fileName, IEnumerable<AuditLogEntry> results) =>
        ExcelExportHelper.BuildXlsx(
            fileName,
            ["ID", "Table Name", "Field Name", "Date/Time", "User Name", "Before", "After", "Reason", "Key"],
            results.Select(e => (IReadOnlyList<object?>)new object?[]
            {
                e.ID, e.TableName, e.FieldName, e.ChangedAt, e.UserName,
                e.BeforeValue, e.AfterValue, e.Reason, e.KeyID
            }));
}
