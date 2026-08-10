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
//   ✓ Sections with zero rows completely skipped (no heading, no rule, no empty table)
//   ✓ Field() private helper — safe null/DBNull column extraction
//
// Layout (A4 Portrait, ~15mm margins):
//   Header (every page):
//     ├── Left : bold "Submission notes for Submission:" + bold SubmissionNumber
//     └── Right: today's date "dd/MM/yyyy"
//   Content:
//     ├── Two-column row: left = "Submission Comments" (bold) + text;
//     │                   right = "Submission Status Comments" (bold) + text
//     └── Up to 5 conditional sections (each only when table has > 0 rows):
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
///   <item><description><b>Five conditional sections</b> (rendered only when the corresponding DataSet table has at least one row): bold-italic heading, thin horizontal rule, 8pt column-header row, data rows.</description></item>
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
    public Task<byte[]> RenderAsync(DataSet ds)
    {
        DataRow? submissionRow = ds.Tables["Submission"]?.Rows.Count > 0
            ? ds.Tables["Submission"]!.Rows[0] : null;

        var submissionNumber        = submissionRow is not null ? Field(submissionRow, "SubmissionNumber")        : string.Empty;
        var submissionComments      = submissionRow is not null ? Field(submissionRow, "SubmissionComments")      : string.Empty;
        var submissionStatusComment = submissionRow is not null ? Field(submissionRow, "SubmissionStatusComment") : string.Empty;

        var tissueRows      = ds.Tables["SubmissionTissues"]?.Rows.Cast<DataRow>().ToList()  ?? [];
        var blockRows       = ds.Tables["SubmissionBlocks"]?.Rows.Cast<DataRow>().ToList()   ?? [];
        var histologyRows   = ds.Tables["BlockHistology"]?.Rows.Cast<DataRow>().ToList()     ?? [];
        var specialStainRows = ds.Tables["BlockSpecialStain"]?.Rows.Cast<DataRow>().ToList() ?? [];
        var antibodyRows    = ds.Tables["BlockAntibodies"]?.Rows.Cast<DataRow>().ToList()    ?? [];

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
                page.Header().PaddingBottom(8).Row(header =>
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

                // ── Page Footer — repeats on every page ──────────────────────────
                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });

                // ── Content ──────────────────────────────────────────────────────
                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // ════════════════════════════════════════════════════════════
                    // Submission Comments — two equal-width columns side by side
                    // ════════════════════════════════════════════════════════════
                    col.Item().Row(row =>
                    {
                        // Left half: Submission Comments
                        row.RelativeItem().Column(c =>
                        {
                            c.Spacing(2);
                            c.Item().Text("Submission Comments").Bold();
                            c.Item().Text(submissionComments);
                        });

                        // 20pt gutter between the two columns
                        row.ConstantItem(20);

                        // Right half: Submission Status Comments
                        row.RelativeItem().Column(c =>
                        {
                            c.Spacing(2);
                            c.Item().Text("Submission Status Comments").Bold();
                            c.Item().Text(submissionStatusComment);
                        });
                    });

                    // ════════════════════════════════════════════════════════════
                    // SECTION 1 — Submission tissue comments (SubmissionTissues)
                    // Skipped entirely when tissueRows is empty.
                    // ════════════════════════════════════════════════════════════
                    if (tissueRows.Count > 0)
                    {
                        col.Item().Column(section =>
                        {
                            section.Spacing(2);
                            section.Item().Text("Submission tissue comments:").Bold().Italic();
                            section.Item().LineHorizontal(0.5f);
                            section.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(25); // SenderRef
                                    c.RelativeColumn(25); // Tissue Code
                                    c.RelativeColumn(25); // Tissue Comment
                                    c.RelativeColumn(25); // Tissue Archive Comment
                                });

                                foreach (var heading in new[] { "SenderRef", "Tissue Code", "Tissue Comment", "Tissue Archive Comment" })
                                    t.Cell().PaddingVertical(2).Text(heading).FontSize(8);

                                foreach (var r in tissueRows)
                                {
                                    t.Cell().PaddingVertical(2).Text(Field(r, "SenderRef"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "TissueCode"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "TissueComment"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "TissueArchiveComment"));
                                }
                            });
                        });
                    }

                    // ════════════════════════════════════════════════════════════
                    // SECTION 2 — Submission blocks comments (SubmissionBlocks)
                    // Skipped entirely when blockRows is empty.
                    // ════════════════════════════════════════════════════════════
                    if (blockRows.Count > 0)
                    {
                        col.Item().Column(section =>
                        {
                            section.Spacing(2);
                            section.Item().Text("Submission blocks comments:").Bold().Italic();
                            section.Item().LineHorizontal(0.5f);
                            section.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(25); // SenderRef
                                    c.RelativeColumn(25); // Block Ref
                                    c.RelativeColumn(25); // Block Comment
                                    c.RelativeColumn(25); // Block Archive Comment
                                });

                                foreach (var heading in new[] { "SenderRef", "Block Ref", "Block Comment", "Block Archive Comment" })
                                    t.Cell().PaddingVertical(2).Text(heading).FontSize(8);

                                foreach (var r in blockRows)
                                {
                                    t.Cell().PaddingVertical(2).Text(Field(r, "SenderRef"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "BlockRef"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "BlockComment"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "BlockArchiveComment"));
                                }
                            });
                        });
                    }

                    // ════════════════════════════════════════════════════════════
                    // SECTION 3 — Histology tests comments (BlockHistology)
                    // Skipped entirely when histologyRows is empty.
                    // ════════════════════════════════════════════════════════════
                    if (histologyRows.Count > 0)
                    {
                        col.Item().Column(section =>
                        {
                            section.Spacing(2);
                            section.Item().Text("Histology tests comments:").Bold().Italic();
                            section.Item().LineHorizontal(0.5f);
                            section.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(25); // Block Ref
                                    c.RelativeColumn(25); // Test
                                    c.RelativeColumn(25); // Test Comment
                                    c.RelativeColumn(25); // Test Archive Comment
                                });

                                foreach (var heading in new[] { "Block Ref", "Test", "Test Comment", "Test Archive Comment" })
                                    t.Cell().PaddingVertical(2).Text(heading).FontSize(8);

                                foreach (var r in histologyRows)
                                {
                                    t.Cell().PaddingVertical(2).Text(Field(r, "BlockRef"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "Test"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "TestComment"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "TestArchiveComment"));
                                }
                            });
                        });
                    }

                    // ════════════════════════════════════════════════════════════
                    // SECTION 4 — Special stain tests comments (BlockSpecialStain)
                    // Skipped entirely when specialStainRows is empty.
                    // ════════════════════════════════════════════════════════════
                    if (specialStainRows.Count > 0)
                    {
                        col.Item().Column(section =>
                        {
                            section.Spacing(2);
                            section.Item().Text("Special stain tests comments:").Bold().Italic();
                            section.Item().LineHorizontal(0.5f);
                            section.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(25); // Block Ref
                                    c.RelativeColumn(25); // Test
                                    c.RelativeColumn(25); // Test Comment
                                    c.RelativeColumn(25); // Test Archive Comment
                                });

                                foreach (var heading in new[] { "Block Ref", "Test", "Test Comment", "Test Archive Comment" })
                                    t.Cell().PaddingVertical(2).Text(heading).FontSize(8);

                                foreach (var r in specialStainRows)
                                {
                                    t.Cell().PaddingVertical(2).Text(Field(r, "BlockRef"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "Test"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "TestComment"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "TestArchiveComment"));
                                }
                            });
                        });
                    }

                    // ════════════════════════════════════════════════════════════
                    // SECTION 5 — Antibodies tests comments (BlockAntibodies)
                    // Skipped entirely when antibodyRows is empty.
                    // ════════════════════════════════════════════════════════════
                    if (antibodyRows.Count > 0)
                    {
                        col.Item().Column(section =>
                        {
                            section.Spacing(2);
                            section.Item().Text("Antibodies tests comments:").Bold().Italic();
                            section.Item().LineHorizontal(0.5f);
                            section.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(25); // BlockRef
                                    c.RelativeColumn(25); // Test
                                    c.RelativeColumn(25); // Test Comment
                                    c.RelativeColumn(25); // Test Archive Comment
                                });

                                foreach (var heading in new[] { "BlockRef", "Test", "Test Comment", "Test Archive Comment" })
                                    t.Cell().PaddingVertical(2).Text(heading).FontSize(8);

                                foreach (var r in antibodyRows)
                                {
                                    t.Cell().PaddingVertical(2).Text(Field(r, "BlockRef"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "Test"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "TestComment"));
                                    t.Cell().PaddingVertical(2).Text(Field(r, "TestArchiveComment"));
                                }
                            });
                        });
                    }
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
