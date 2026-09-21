using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web;

/// <summary>
/// Builds real <c>.xlsx</c> downloads (via ClosedXML) for search/audit result grids.
///
/// Replaces the legacy <c>ExcelExport.aspx</c> pattern — any page stashed a
/// DataTable/DataView into Session, then redirected to ExcelExport.aspx, which
/// rendered a <c>DataGrid</c> to an <c>application/vnd.ms-excel</c> response
/// (an HTML table masquerading as <c>.xls</c>, not a real binary workbook).
/// This produces a genuine Open XML workbook with typed cells instead.
/// </summary>
public static class ExcelExportHelper
{
    /// <summary>
    /// Builds a downloadable .xlsx file from a header row and typed data rows.
    /// Cell values are written with their native type (date/number/string) so
    /// Excel treats them as real dates/numbers, not text.
    /// </summary>
    public static FileContentResult BuildXlsx(
        string fileName,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Export");

        for (var c = 0; c < headers.Count; c++)
        {
            var cell = sheet.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
        }

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Count; c++)
                SetCellValue(sheet.Cell(r, c + 1), row[c]);
            r++;
        }

        if (r > 2) sheet.Range(1, 1, r - 1, headers.Count).SetAutoFilter();
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var extension = fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? fileName : fileName + ".xlsx";
        return new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = extension
        };
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = "dd/MM/yyyy";
                break;
            case DateOnly d:
                cell.Value = d.ToDateTime(TimeOnly.MinValue);
                cell.Style.DateFormat.Format = "dd/MM/yyyy";
                break;
            case int i:
                cell.Value = i;
                break;
            case long l:
                cell.Value = l;
                break;
            case short s:
                cell.Value = s;
                break;
            case decimal m:
                cell.Value = m;
                break;
            case double dd:
                cell.Value = dd;
                break;
            case bool b:
                cell.Value = b;
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }
}
