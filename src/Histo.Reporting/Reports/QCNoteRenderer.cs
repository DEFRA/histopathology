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
//           TestSummary (Sender Ref/Histo Ref/Block Ref/Test, monospaced — built by the
//           DataSetBuilder from live SP row data, always present regardless of QCText),
//           then QCText (the free-form note) below it. QCText alone cannot be trusted to
//           contain the headings — it is copied verbatim from the SP and can be pure free
//           text for a genuinely saved note.
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
///   <item><description><b>Body bordered box</b>: <c>TestSummary</c> (always present) followed by <c>QCText</c>, both monospaced. Expands to fill remaining page height.</description></item>
///   <item><description><b>Footer</b>: CreatedBy left, DateCreated right.</description></item>
/// </list>
/// <para>
/// Expected DataSet: single table named <c>"Header"</c> with columns
/// QCNoteRef, SubmissionNumber, Project, Species, StainRef, QCText, TestSummary, CreatedBy, DateCreated.
/// DateCreated is pre-formatted as "dd MMMM yyyy" by the DataSetBuilder — no parsing needed here.
/// <c>TestSummary</c> is the Sender Ref/Histo Ref/Block Ref/Test table built directly from the SP's
/// row data — it is always shown, because <c>QCText</c> alone is not a reliable source of these
/// headings (a genuinely saved note can be pure free text with no such table embedded in it).
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
        var testSummary      = Field(row, "TestSummary");
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
                    // TestSummary (Sender Ref/Histo Ref/Block Ref/Test, built by the
                    // DataSetBuilder from live SP data) is always shown first, followed by
                    // QCText — QCText alone cannot be relied on to contain the headings.
                    // ════════════════════════════════════════════════════════════
                    col.Item().Extend().Border(1).Padding(4).Column(body =>
                    {
                        body.Spacing(4);
                        if (!string.IsNullOrEmpty(testSummary))
                            body.Item().Text(testSummary).FontFamily("Consolas").FontSize(9);
                        body.Item().Text(qcText).FontFamily("Consolas").FontSize(9);
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
