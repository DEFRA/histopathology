// Stage 3 — HtmlToPdfConversionSkill
// Runtime paradigm : modern (net10.0, ASP.NET Core) — explicit, not auto-detected
// PDF engine       : QuestPDF Community Edition (no CrystalDecisions, no paid engine)
// Source definition: output/definition/QCNote.ReportDefinition.json
// Source template  : output/templates/QCNote.html
// Generated        : 2026-08-10
//
// Gate checks (all passing):
//   ✓ No CrystalDecisions.* imports
//   ✓ No paid PDF engine
//   ✓ QuestPDF.Settings.License = LicenseType.Community set in static constructor
//   ✓ RenderAsync(DataSet ds) signature exposed
//   ✓ Reads from ds.Tables["Header"].Rows[0]
//   ✓ Field() private helper — safe null/DBNull column extraction
//
// Layout (A4 Portrait, ~15mm margins):
//   Content:
//     ├── Top bordered box  — 5-row, 2-column table (40% label / 60% bold value)
//     │     QC Note Ref | Submission Number | Project | Species | Stain Ref
//     └── Body bordered box — fills remaining page height
//           QCText, monospaced (legacy stores the Sender Ref/Histo Ref/Block Ref/Test
//           header and data row as pre-padded plain text inside QCText itself — do not
//           render a separate header row here, or it duplicates on every note).
//   Footer (pinned): CreatedBy left  /  DateCreated right

using System.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Histo.Reporting.Reports;

/// <summary>
/// Renders QCNote to PDF using QuestPDF Community Edition.
/// Layout matches the legacy Crystal Reports A4 portrait QC Note form:
/// <list type="bullet">
///   <item><description><b>Top bordered box</b>: 5 label/value rows — QCNoteRef, SubmissionNumber, Project, Species, StainRef.</description></item>
///   <item><description><b>Body bordered box</b>: <c>QCText</c> rendered as a single monospaced block. Expands to fill remaining page height.</description></item>
///   <item><description><b>Footer</b>: CreatedBy left, DateCreated right.</description></item>
/// </list>
/// <para>
/// Expected DataSet: single table named <c>"Header"</c> with columns
/// QCNoteRef, SubmissionNumber, Project, Species, StainRef, QCText, CreatedBy, DateCreated.
/// DateCreated is pre-formatted as "dd MMMM yyyy" by the DataSetBuilder — no parsing needed here.
/// <c>QCText</c> already contains the Sender Ref/Histo Ref/Block Ref/Test header and data row
/// as space-padded plain text (legacy builds this directly into the field), so it must be
/// rendered as-is in a monospaced font rather than alongside a second, separately-drawn header.
/// </para>
/// </summary>
public sealed class QCNoteRenderer
{
    static QCNoteRenderer()
    {
        // QuestPDF Community licence must be declared before any Document.Create() call.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Renders the QC Note to PDF using QuestPDF Community Edition.
    /// </summary>
    /// <param name="ds">Populated DataSet with a single table named <c>"Header"</c>.</param>
    /// <returns>PDF content as a byte array.</returns>
    public Task<byte[]> RenderAsync(DataSet ds)
    {
        DataRow row = ds.Tables["Header"]!.Rows[0];

        var qcNoteRef        = Field(row, "QCNoteRef");
        var submissionNumber = Field(row, "SubmissionNumber");
        var project          = Field(row, "Project");
        var species          = Field(row, "Species");
        var stainRef         = Field(row, "StainRef");
        var qcText           = Field(row, "QCText");
        var createdBy        = Field(row, "CreatedBy");
        var dateCreated      = Field(row, "DateCreated");

        byte[] pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                // A4 Portrait — confirmed from visual reference (QCNote-image.png).
                page.Size(PageSizes.A4);
                page.MarginTop(15,    Unit.Millimetre);
                page.MarginBottom(15, Unit.Millimetre);
                page.MarginLeft(15,   Unit.Millimetre);
                page.MarginRight(15,  Unit.Millimetre);
                page.DefaultTextStyle(s => s.FontFamily("Arial").FontSize(9));

                // ── Footer — CreatedBy left, DateCreated right ───────────────────
                // Pinned to the page bottom; grows upward so content area remains
                // bounded above it.
                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().Text(createdBy).FontSize(9);
                    footer.RelativeItem().AlignRight().Text(dateCreated).FontSize(9);
                });

                // ── Content ──────────────────────────────────────────────────────
                page.Content().Column(col =>
                {
                    col.Spacing(4);

                    // ════════════════════════════════════════════════════════════
                    // SECTION 1 — PageHeader
                    // Bordered box: 5-row, 2-column table.
                    // Column widths: 40% label (plain) / 60% value (bold).
                    // ════════════════════════════════════════════════════════════
                    col.Item().Border(1).Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(40); // label
                            c.RelativeColumn(60); // value
                        });

                        void Label(string text) =>
                            t.Cell().PaddingVertical(3).PaddingHorizontal(4)
                             .Text(text).FontSize(9);

                        void Value(string val) =>
                            t.Cell().PaddingVertical(3).PaddingHorizontal(4)
                             .Text(val).Bold().FontSize(9);

                        Label("QC Note Ref:");       Value(qcNoteRef);
                        Label("Submission Number:"); Value(submissionNumber);
                        Label("Project:");           Value(project);
                        Label("Species:");           Value(species);
                        Label("Stain Ref:");         Value(stainRef);
                    });

                    // ════════════════════════════════════════════════════════════
                    // SECTION 2 — Detail / Body
                    // Bordered box that fills the remaining page height (Extend()).
                    // QCText already contains the Sender Ref/Histo Ref/Block Ref/Test
                    // header and data row as space-padded plain text (built by legacy
                    // directly into the field) — render it as a single monospaced
                    // block so columns stay aligned. Do not draw a second header row
                    // here; it would duplicate what QCText already contains.
                    // ════════════════════════════════════════════════════════════
                    col.Item().Extend().Border(1).Padding(4)
                       .Text(qcText).FontFamily("Consolas").FontSize(9);
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
