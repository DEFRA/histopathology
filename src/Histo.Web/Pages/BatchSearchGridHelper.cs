using Histo.Core.Domain;
using Histo.Submissions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Histo.Web.Pages;

/// <summary>Grid and Excel-export plumbing shared by the SearchSubmissions and ViewSubmissions pages.</summary>
public static class BatchSearchGridHelper
{
    public const int PageSize = 10;

    /// <summary>
    /// formaction for each row's Select button. SortColumn/PageNumber are POST-bound (user
    /// controllable), so they're percent-encoded before being embedded in the query string —
    /// otherwise a value containing '&amp;' could inject extra query parameters.
    /// </summary>
    public static string BuildSelectFormAction(string? sortColumn, bool sortDesc, int pageNumber) =>
        $"?handler=Select&SortColumn={Uri.EscapeDataString(sortColumn ?? string.Empty)}&SortDesc={(sortDesc ? "true" : "false")}&PageNumber={pageNumber}";

    /// <summary>Fills the ViewData keys read by the sort/pagination partials; returns the page number clamped to the result set.</summary>
    public static int PopulateGridViewData(ViewDataDictionary viewData, int resultCount, int pageNumber, string? sortColumn, bool sortDesc, string formId)
    {
        var totalPages = resultCount == 0 ? 1 : (int)Math.Ceiling(resultCount / (double)PageSize);
        if (pageNumber < 1) pageNumber = 1;
        else if (pageNumber > totalPages) pageNumber = totalPages;
        viewData["SortColumn"] = sortColumn;
        viewData["SortDesc"] = sortDesc;
        viewData["CurrentPage"] = pageNumber;
        viewData["TotalPages"] = totalPages;
        viewData["FormId"] = formId;
        viewData["Handler"] = "Search";
        return pageNumber;
    }

    /// <summary>Legacy <c>lbExportExcel_Click</c> → <c>ExcelExport.aspx</c>: the dedicated 16-column export table.</summary>
    public static FileContentResult BuildExport(string fileName, IEnumerable<BatchSearchResult> results) =>
        ExcelExportHelper.BuildXlsx(
            fileName,
            ["Submission Number", "Project/Contract", "Pathologist", "Species", "Submitted Date",
             "Submission Type", "Submitted By", "Safe To Handle", "Received Date", "Time Received/Rejected",
             "Received By", "Other Submitted By", "Comments", "Customer Received Date", "Status", "Completed Date"],
            results.Select(r => (IReadOnlyList<object?>)new object?[]
            {
                r.ID,
                r.ProjectDescription,
                r.ContactDescription,
                r.Species,
                r.BatchDate,
                r.BatchType == "0" ? "TSE" : "NON TSE",
                r.SubmittedBy,
                r.SafeToHandle is "1" or "true" or "True" or "yes" or "Yes" ? "Yes" : "No",
                r.DateReceived,
                r.ReceivedTime,
                r.ReceivedBy,
                r.OtherSubmittedBy,
                r.Comments,
                r.CustomerReceivedDate,
                BatchStatus.DisplayName(r.Status ?? string.Empty),
                r.DateCompleted
            }));
}
