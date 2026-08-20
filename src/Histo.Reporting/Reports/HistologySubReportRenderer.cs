// Stage 3 — HtmlToPdfConversionSkill
// Runtime paradigm : modern (net10.0, ASP.NET Core)
// PDF engine       : QuestPDF Community Edition (no CrystalDecisions, no paid engine)
// Source definition: output/definition/HistologySubReport.ReportDefinition.json
// Source template  : output/templates/HistologySubReport.html
// Parent report    : HistologyReport.rpt (embedded sub-report per ADR-004)
// Generated        : 2026-08-05
// Status           : stub — callingPageContext absent (Histo.Reporting is a stub project)
//
// TODO: wire data source — re-run Stage 3 after migration completes.
//
// IMPORTANT: In normal production use the sub-report section is composed inline
// within HistologyReportRenderer.RenderAsync() — no separate render pass.
// This standalone renderer is provided for independent rendering only
// (e.g. unit testing, previewing the sub-report in isolation).
//
// Gate checks (all passing):
//   ✓ No CrystalDecisions.* imports
//   ✓ No paid PDF engine
//   ✓ QuestPDF.Settings.License = LicenseType.Community set in static constructor
//   ✓ RenderAsync(DataSet ds) signature exposed

using System.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Histo.Reporting.Reports;

/// <summary>
/// Standalone renderer for HistologySubReport (histology codes table).
/// <para>
/// In normal use this sub-report section is composed inline within
/// <see cref="HistologyReportRenderer.RenderAsync"/> — no separate render pass.
/// This class is provided for independent rendering only (e.g. unit testing).
/// No CrystalDecisions dependency.
/// </para>
/// <para>
/// Expected DataSet tables (HistologyReportDataset.xsd schema):
/// <list type="bullet">
///   <item><description><b>BatchHistology</b> — columns: BatchID (int), Code (string).
///   Max 8 rows (batch-level) or 9 rows (block-level) per SubmissionForm.aspx.vb
///   CreateBatchTestTable/CreateBlockTestTable modulo logic.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class HistologySubReportRenderer
{
    static HistologySubReportRenderer()
    {
        // QuestPDF Community licence must be declared before any Document.Create() call.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Renders the sub-report to a PDF byte array.
    /// <para>
    /// TODO: wire data source — re-run Stage 3 after migration completes.
    /// The calling controller/page must populate <paramref name="ds"/> before
    /// calling this method.
    /// </para>
    /// <para>
    /// In production, call <see cref="HistologyReportRenderer.RenderAsync"/> to render
    /// the full parent report (which includes this sub-report section inline).
    /// </para>
    /// </summary>
    /// <param name="ds">DataSet containing at minimum a BatchHistology table.</param>
    /// <returns>PDF content as a byte array.</returns>
    public Task<byte[]> RenderAsync(DataSet ds)
    {
        var histologyRows = ds.Tables["BatchHistology"]?.Rows.Cast<DataRow>().ToList() ?? [];

        byte[] pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Sub-report inherits page context from parent — margins 0 when embedded.
                // For standalone rendering A4 with zero margin preserves that expectation.
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(9));

                page.Content().Column(col =>
                {
                    // Section 0 — ReportHeader (ColHeader_Code label)
                    // Section 2 — Detail (repeating rows — one per BatchHistology record)
                    // Sections 1, 3, 4 are empty/suppressed for this sub-report.
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => c.RelativeColumn());

                        t.Header(header =>
                            header.Cell()
                                  .Background(Colors.Grey.Lighten2)
                                  .Border(0.5f)
                                  .PaddingVertical(2).PaddingHorizontal(8)
                                  .Text("Histology Codes").Bold()
                        );

                        foreach (var row in histologyRows)
                        {
                            t.Cell().Border(0.5f)
                             .PaddingVertical(2).PaddingHorizontal(8)
                             .Text(Field(row, "Code"));
                        }
                    });
                });
            });
        }).GeneratePdf();

        return Task.FromResult(pdf);
    }

    /// <summary>
    /// Safe field extractor. Returns <see cref="string.Empty"/> when the column
    /// is absent, the row value is <see cref="DBNull"/>, or the value is null.
    /// </summary>
    private static string Field(DataRow row, string column)
    {
        if (!row.Table.Columns.Contains(column)) return string.Empty;
        var val = row[column];
        return val is DBNull || val is null
            ? string.Empty
            : Convert.ToString(val) ?? string.Empty;
    }
}
