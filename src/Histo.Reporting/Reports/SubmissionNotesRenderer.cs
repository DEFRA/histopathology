// Stage 3 — HtmlToPdfConversionSkill
// Runtime paradigm : modern (net10.0, ASP.NET Core) — explicit, not auto-detected
// PDF engine       : QuestPDF Community Edition (no CrystalDecisions, no paid engine)
// Source definition: output/definition/SubmissionNotesReport.ReportDefinition.json
// Source template  : output/templates/SubmissionNotesReport.html
// Generated        : 2026-08-10
//
// Gate checks (all passing):
//   ✓ No CrystalDecisions.* imports
//   ✓ No paid PDF engine
//   ✓ QuestPDF.Settings.License = LicenseType.Community set in static constructor
//   ✓ RenderAsync(DataSet ds) signature exposed
//   ✓ Reads from ds.Tables by name:
//       Submission, SubmissionTissues, SubmissionBlocks,
//       BlockHistology, BlockSpecialStain, BlockAntibodies
//   ✓ Field() private helper — safe null/DBNull column extraction
//
// Layout (A4 Portrait, ~15mm margins):
//   Header (every page):
//     ├── Left : bold "Submission notes for Submission:" + bold SubmissionNumber
//     └── Right: today's date "dd/MM/yyyy"
//   Content:
//     ├── Two-column row: left = "Submission Comments" (bold) + text,
//     │                   right = "Submission Status Comments" (bold) + text
//     └── 5 sections, always rendered regardless of row count (confirmed against
//         SubmissionNotes-image.png reference — legacy always prints heading/column
//         headers even with zero matching rows; only the data rows themselves vary):
//           bold-italic heading → thin horizontal rule → 8pt column-header row → data rows
//           1. SubmissionTissues  — SenderRef | TissueCode | TissueComment | TissueArchiveComment
//           2. SubmissionBlocks   — SenderRef | BlockRef   | BlockComment  | BlockArchiveComment
//           3. BlockHistology     — BlockRef  | Test       | TestComment   | TestArchiveComment
//           4. BlockSpecialStain  — BlockRef  | Test       | TestComment   | TestArchiveComment
//           5. BlockAntibodies    — BlockRef  | Test       | TestComment   | TestArchiveComment
//   Footer (every page): right-aligned "Page {current} of {total}"

using System.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Histo.Reporting.Reports;

/// <summary>
/// Renders SubmissionNotesReport to PDF using QuestPDF Community Edition.
/// Layout matches the legacy Crystal Reports A4 portrait Submission Notes report.
/// <list type="bullet">
///   <item><description><b>Page header</b>: left — bold "Submission notes for Submission:" + bold SubmissionNumber; right — today's date "dd/MM/yyyy".</description></item>
///   <item><description><b>Submission comments</b>: two equal-width columns — "Submission Comments" (left) and "Submission Status Comments" (right), each with a bold heading above the comment text.</description></item>
///   <item><description><b>Five sections</b> (always rendered, regardless of row count — matches the legacy reference PDF, which always prints the heading/column headers even with zero matching rows): bold-italic heading, thin horizontal rule, 8pt column-header row, data rows.</description></item>
///   <item><description><b>Page footer</b>: right-aligned "Page {n} of {total}".</description></item>
/// </list>
/// <para>
/// Expected DataSet tables (pre-filtered by DataSetBuilder — only non-blank rows are present):
/// <list type="bullet">
///   <item><description><b>Submission</b>     — SubmissionNumber, SubmissionComments, SubmissionStatusComment (row 0)</description></item>
///   <item><description><b>SubmissionTissues</b> — SenderRef, TissueCode, TissueComment, TissueArchiveComment</description></item>
///   <item><description><b>SubmissionBlocks</b>  — SenderRef, BlockRef, BlockComment, BlockArchiveComment</description></item>
///   <item><description><b>BlockHistology</b>    — BlockRef, Test, TestComment, TestArchiveComment</description></item>
///   <item><description><b>BlockSpecialStain</b> — BlockRef, Test, TestComment, TestArchiveComment</description></item>
///   <item><description><b>BlockAntibodies</b>   — BlockRef, Test, TestComment, TestArchiveComment</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class SubmissionNotesRenderer
{
    private const string SenderRefColumn = "SenderRef";
    private const string BlockRefColumn = "BlockRef";

    private static readonly string[] TissueCommentHeaders = [SenderRefColumn, "Tissue Code", "Tissue Comment", "Tissue Archive Comment"];
    private static readonly string[] BlockCommentHeaders = [SenderRefColumn, "Block Ref", "Block Comment", "Block Archive Comment"];
    private static readonly string[] TestCommentHeaders = ["Block Ref", "Test", "Test Comment", "Test Archive Comment"];
    private static readonly string[] TestCommentHeadersWithoutBlockRef = [BlockRefColumn, "Test", "Test Comment", "Test Archive Comment"];

    private static readonly string[] TestCommentFields = [BlockRefColumn, "Test", "TestComment", "TestArchiveComment"];

    static SubmissionNotesRenderer()
    {
        // QuestPDF Community licence must be declared before any Document.Create() call.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Renders the Submission Notes report to PDF using QuestPDF Community Edition.
    /// </summary>
    /// <param name="ds">Populated DataSet containing the 6 named tables described above.</param>
    /// <returns>PDF content as a byte array.</returns>
    public static Task<byte[]> RenderAsync(DataSet ds)
    {
        DataRow? submissionRow = ds.Tables["Submission"]?.Rows.Count > 0
            ? ds.Tables["Submission"]!.Rows[0] : null;

        var submissionNumber        = SubmissionField(submissionRow, "SubmissionNumber");
        var submissionComments      = SubmissionField(submissionRow, "SubmissionComments");
        var submissionStatusComment = SubmissionField(submissionRow, "SubmissionStatusComment");

        var tissueRows       = TableRows(ds, "SubmissionTissues");
        var blockRows        = TableRows(ds, "SubmissionBlocks");
        var histologyRows    = TableRows(ds, "BlockHistology");
        var specialStainRows = TableRows(ds, "BlockSpecialStain");
        var antibodyRows     = TableRows(ds, "BlockAntibodies");

        var reportDate = DateTime.Now.ToString("dd/MM/yyyy");

        byte[] pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                // A4 Portrait — confirmed from visual reference (SubmissionNotes-image.png).
                page.Size(PageSizes.A4);
                page.MarginTop(15,    Unit.Millimetre);
                page.MarginBottom(15, Unit.Millimetre);
                page.MarginLeft(15,   Unit.Millimetre);
                page.MarginRight(15,  Unit.Millimetre);
                page.DefaultTextStyle(s => s.FontFamily("Arial").FontSize(9));

                // ── Page Header — repeats on every page ──────────────────────────
                ComposeHeader(page.Header(), submissionNumber, reportDate);

                // ── Page Footer — repeats on every page ──────────────────────────
                ComposeFooter(page.Footer());

                // ── Content ──────────────────────────────────────────────────────
                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // ════════════════════════════════════════════════════════════
                    // Submission Comments — two equal-width columns side by side
                    // ════════════════════════════════════════════════════════════
                    ComposeSubmissionComments(col, submissionComments, submissionStatusComment);

                    // ════════════════════════════════════════════════════════════
                    // SECTION 1 — Submission tissue comments (SubmissionTissues)
                    // Heading/column headers always render; only data rows vary.
                    // ════════════════════════════════════════════════════════════
                    ComposeNotesSection(col, "Submission tissue comments:", TissueCommentHeaders,
                        [SenderRefColumn, "TissueCode", "TissueComment", "TissueArchiveComment"], tissueRows);

                    // ════════════════════════════════════════════════════════════
                    // SECTION 2 — Submission blocks comments (SubmissionBlocks)
                    // Heading/column headers always render; only data rows vary.
                    // ════════════════════════════════════════════════════════════
                    ComposeNotesSection(col, "Submission blocks comments:", BlockCommentHeaders,
                        [SenderRefColumn, BlockRefColumn, "BlockComment", "BlockArchiveComment"], blockRows);

                    // ════════════════════════════════════════════════════════════
                    // SECTION 3 — Histology tests comments (BlockHistology)
                    // Heading/column headers always render; only data rows vary.
                    // ════════════════════════════════════════════════════════════
                    ComposeNotesSection(col, "Histology tests comments:", TestCommentHeaders, TestCommentFields, histologyRows);

                    // ════════════════════════════════════════════════════════════
                    // SECTION 4 — Special stain tests comments (BlockSpecialStain)
                    // Heading/column headers always render; only data rows vary.
                    // ════════════════════════════════════════════════════════════
                    ComposeNotesSection(col, "Special stain tests comments:", TestCommentHeaders, TestCommentFields, specialStainRows);

                    // ════════════════════════════════════════════════════════════
                    // SECTION 5 — Antibodies tests comments (BlockAntibodies)
                    // Heading/column headers always render; only data rows vary.
                    // ════════════════════════════════════════════════════════════
                    ComposeNotesSection(col, "Antibodies tests comments:", TestCommentHeadersWithoutBlockRef, TestCommentFields, antibodyRows);
                });
            });
        }).GeneratePdf();

        return Task.FromResult(pdf);
    }

    private static string SubmissionField(DataRow? row, string column)
        => row is not null ? Field(row, column) : string.Empty;

    private static List<DataRow> TableRows(DataSet ds, string tableName)
        => ds.Tables[tableName]?.Rows.Cast<DataRow>().ToList() ?? [];

    private static void ComposeHeader(IContainer container, string submissionNumber, string reportDate)
    {
        container.Column(headerCol =>
        {
            headerCol.Item().Row(header =>
            {
                // Left: bold label + bold submission number (both bold, per visual reference)
                header.RelativeItem().Text(t =>
                {
                    t.Span("Submission notes for Submission:  ").Bold();
                    t.Span(submissionNumber).Bold();
                });
                // Right: today's date
                header.RelativeItem().AlignRight().Text(reportDate);
            });
            // Full-width rule under the title, per visual reference (SubmissionNotes-image.png).
            headerCol.Item().PaddingTop(2).PaddingBottom(8).LineHorizontal(0.5f);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignRight().Text(t =>
        {
            t.Span("Page ");
            t.CurrentPageNumber();
            t.Span(" of ");
            t.TotalPages();
        });
    }

    private static void ComposeSubmissionComments(ColumnDescriptor col, string comments, string statusComments)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Spacing(2);
                c.Item().Text("Submission Comments").Bold();
                c.Item().Text(comments);
            });

            // 20pt gutter between the two columns
            row.ConstantItem(20);

            row.RelativeItem().Column(c =>
            {
                c.Spacing(2);
                c.Item().Text("Submission Status Comments").Bold();
                c.Item().Text(statusComments);
            });
        });
    }

    private static void ComposeNotesSection(
        ColumnDescriptor col,
        string title,
        string[] headers,
        string[] fields,
        List<DataRow> rows)
    {
        col.Item().Column(section =>
        {
            section.Spacing(2);
            section.Item().Text(title).Bold().Italic();
            section.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    for (var i = 0; i < 4; i++)
                        c.RelativeColumn(25);
                });

                foreach (var heading in headers)
                    t.Cell().PaddingVertical(2).Text(heading).FontSize(8);

                t.Cell().ColumnSpan(4).LineHorizontal(0.5f);

                foreach (var r in rows)
                {
                    foreach (var field in fields)
                        t.Cell().PaddingVertical(2).Text(Field(r, field));
                }
            });
        });
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
