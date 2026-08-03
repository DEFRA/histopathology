using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web;

/// <summary>
/// Builds CSV downloads for search/audit result grids.
///
/// Replaces the legacy <c>ExcelExport.aspx</c> pattern — any page stashed a
/// DataTable/DataView into Session, then redirected to ExcelExport.aspx, which
/// rendered a <c>DataGrid</c> to an <c>application/vnd.ms-excel</c> response.
/// ASP.NET Core has no <c>DataGrid.RenderControl</c> equivalent, and no
/// Excel-writing NuGet package is referenced anywhere in this solution — so a
/// plain CSV download (which Excel opens natively) is the simplest faithful
/// equivalent and requires no new dependency.
/// </summary>
public static class CsvExportHelper
{
    private static readonly char[] SpecialChars = [',', '"', '\n', '\r'];

    /// <summary>Builds a downloadable CSV file result from a header row and data rows.</summary>
    public static FileContentResult BuildCsv(
        string fileName,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string?>> rows)
    {
        var sb = new StringBuilder();
        AppendRow(sb, headers);
        foreach (var row in rows)
            AppendRow(sb, row);

        // UTF-8 BOM so Excel correctly detects the encoding when opening the file.
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
        return new FileContentResult(bytes, "text/csv") { FileDownloadName = fileName };
    }

    private static void AppendRow(StringBuilder sb, IReadOnlyList<string?> fields) =>
        sb.AppendLine(string.Join(",", fields.Select(Escape)));

    private static string Escape(string? field)
    {
        field ??= string.Empty;
        return field.IndexOfAny(SpecialChars) >= 0
            ? $"\"{field.Replace("\"", "\"\"")}\""
            : field;
    }
}
