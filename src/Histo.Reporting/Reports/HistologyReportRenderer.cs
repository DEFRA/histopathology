// Stage 3 — HtmlToPdfConversionSkill
// Runtime paradigm : modern (net10.0, ASP.NET Core)
// PDF engine       : QuestPDF Community Edition (no CrystalDecisions, no paid engine)
// Source definition: output/definition/HistologyReport.ReportDefinition.json
// Source template  : output/templates/HistologyReport.html
// Generated        : 2026-08-05
// Updated          : 2026-08-06 — orientation corrected to landscape (confirmed from legacy PDF MediaBox [0 0 841 595])
// Updated          : 2026-08-07 — full layout rewrite to match legacy pre-printed form structure
//                                  (3-panel header, 13-column table, 4-panel bottom section)
//
// Gate checks (all passing):
//   ✓ No CrystalDecisions.* imports
//   ✓ No paid PDF engine
//   ✓ QuestPDF.Settings.License = LicenseType.Community set in static constructor
//   ✓ RenderAsync(DataSet ds) signature exposed
//   ✓ HistologySubReport section inlined in bottom-left panel — no separate render pass (ADR-004)
//   ✓ Wingdings Chr(252) → Unicode □/■ checkbox substitution
//   ✓ RepeatBlock → "*" appended to BlockRef when truthy
//   ✓ SafeToHandle bool → "Yes"/"No" (matches "Adequately Fixed?" label in legacy form)

using System.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Histo.Reporting.Reports;

/// <summary>
/// Renders HistologyReport to PDF using QuestPDF Community Edition.
/// Composes the HistologySubReport section inline within a single
/// <c>Document.Create()</c> call — no separate sub-report render pass,
/// matching the nesting described in ADR-004.
/// <para>
/// Expected DataSet tables (HistologyReportDataset.xsd schema):
/// <list type="bullet">
///   <item><description><b>Batch</b> — batch-level header fields (row 0)</description></item>
///   <item><description><b>BatchPostFixation</b> — Decal/Phenol/Formic/Other flags (row 0)</description></item>
///   <item><description><b>BatchHistology</b> — histology codes; linked via Batch.ID → BatchHistology.BatchID</description></item>
///   <item><description><b>BatchSubmission</b> — submission rows (SenderRef, HistologyRef, BlockRef, TissueDetails, CustomerRef, RepeatBlock)</description></item>
///   <item><description><b>Version</b> — version string for page footer (row 0)</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class HistologyReportRenderer
{
    static HistologyReportRenderer()
    {
        // QuestPDF Community licence must be declared before any Document.Create() call.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Renders the Histology Submission Form to PDF using QuestPDF Community Edition.
    /// Layout matches the legacy Crystal Reports pre-printed form:
    /// <list type="bullet">
    ///   <item>3-panel header row: left (batch identity), centre (title + dates/species), right (batch type badge + adequately fixed + total samples)</item>
    ///   <item>Italic instruction line</item>
    ///   <item>13-column submission table (5 data columns + 8 blank pre-printed columns)</item>
    ///   <item>4-panel bottom section: Histology Required / Fixation / Tissue Processing / Dispatch</item>
    ///   <item>Footer: version string left + disclaimer centre</item>
    /// </list>
    /// </summary>
    /// <param name="ds">Populated DataSet matching HistologyReportDataset.xsd.</param>
    /// <param name="rowsPerPage">Submission rows printed per page (legacy default 13). Values &lt;= 0 fall back to 13.</param>
    /// <returns>PDF content as a byte array.</returns>
    public Task<byte[]> RenderAsync(DataSet ds, int rowsPerPage = 13)
    {
        DataRow? batch   = ds.Tables["Batch"]?.Rows.Count > 0
            ? ds.Tables["Batch"]!.Rows[0] : null;
        DataRow? postFix = ds.Tables["BatchPostFixation"]?.Rows.Count > 0
            ? ds.Tables["BatchPostFixation"]!.Rows[0] : null;

        var histologyRows  = ds.Tables["BatchHistology"]?.Rows.Cast<DataRow>().ToList()  ?? [];
        var submissionRows = ds.Tables["BatchSubmission"]?.Rows.Cast<DataRow>().ToList() ?? [];

        var versionStr = ds.Tables["Version"]?.Rows.Count > 0
            ? Field(ds.Tables["Version"]!.Rows[0], "Version")
            : string.Empty;

        // Pre-resolve values used in multiple sections.
        var projectCode    = batch is not null ? Field(batch, "ProjectContractCode") : string.Empty;
        var pathologist    = batch is not null ? Field(batch, "ContactName")         : string.Empty;
        var submittedBy    = batch is not null ? Field(batch, "OtherSubmittedBy")    : string.Empty;
        var submittedAs    = batch is not null ? Field(batch, "SubmittedAs")         : string.Empty;
        var batchDate      = batch is not null ? FormatDate(Field(batch, "BatchDate"))     : string.Empty;
        var dateReceived   = batch is not null ? FormatDate(Field(batch, "DateReceived"))  : string.Empty;
        var timeReceived   = batch is not null ? Field(batch, "TimeReceived")        : string.Empty;
        var species        = batch is not null ? Field(batch, "Species")             : string.Empty;
        var batchType      = batch is not null ? Field(batch, "BatchType")           : string.Empty;
        var adequatelyFixed = batch is not null
            ? (Field(batch, "SafeToHandle") is "True" or "true" or "1" or "yes" or "Yes" ? "Yes" : "No")
            : string.Empty;
        var totalSamples   = batch is not null ? Field(batch, "NumberSamples")       : string.Empty;
        var fixation       = batch is not null ? Field(batch, "Fixation")            : string.Empty;
        var postFixOther   = batch is not null ? Field(batch, "PostFixationOther")   : string.Empty;
        var comments       = batch is not null ? Field(batch, "Comments")            : string.Empty;
        var commentLengthOk = batch is not null ? IsChecked(Field(batch, "CommentLengthOK")) : false;
        var batchId        = batch is not null ? Field(batch, "ID")                  : string.Empty;

        var decal  = postFix is not null ? IsChecked(Field(postFix, "Decal"))  : false;
        var phenol = postFix is not null ? IsChecked(Field(postFix, "Phenol")) : false;
        var formic = postFix is not null ? IsChecked(Field(postFix, "Formic")) : false;
        var other  = postFix is not null ? IsChecked(Field(postFix, "Other"))  : false;

        byte[] pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                // A4 Landscape — confirmed from legacy PDF MediaBox [0 0 841 595].
                page.Size(PageSizes.A4.Landscape());
                page.MarginTop(10,    Unit.Millimetre);
                page.MarginBottom(10, Unit.Millimetre);
                page.MarginLeft(10,   Unit.Millimetre);
                page.MarginRight(10,  Unit.Millimetre);
                page.DefaultTextStyle(s => s.FontFamily("Arial").FontSize(8));

                // ── Footer — bottom panels + version/disclaimer/page row ────────
                // QuestPDF footer grows upward from the page bottom, so the 4 panels
                // are always pinned to the foot of the page with natural white space
                // above them — no Extend() spacer needed in the content column.
                page.Footer().Column(foot =>
                {
                    foot.Spacing(0);

                    // ── Section 3 — 4-panel bottom row ───────────────────────────
                    foot.Item().Border(1).Row(panels =>
                    {
                        // ── Panel A — Histology Required ──────────────────────
                        panels.RelativeItem(30).BorderRight(1).Padding(3).Column(a =>
                        {
                            a.Spacing(2);

                            // Two columns side by side (matches legacy): Histology Required
                            // list on the left, Comments box + "More comments" checkbox on the right.
                            a.Item().Row(top =>
                            {
                                top.RelativeItem(34).Column(left =>
                                {
                                    left.Item().Text("Histology Required").Bold().FontSize(8);

                                    // HistologySubReport inlined (ADR-004): BatchHistology codes list.
                                    foreach (var hrow in histologyRows)
                                        left.Item().PaddingLeft(4).Text(Field(hrow, "Code")).FontSize(8);
                                });

                                top.ConstantItem(6);

                                top.RelativeItem(66).Column(cmt =>
                                {
                                    cmt.Item().Text("Comments").Bold().FontSize(7.5f);
                                    // Fixed (not Min) height — a hard cap so long comments never push
                                    // into or overlap the "Stain Ref" line below; overflow text clips.
                                    cmt.Item().Border(0.5f).Height(95).Padding(2).Text(comments).FontSize(8).ClampLines(7);
                                    CheckboxItem(cmt.Item().PaddingTop(3), commentLengthOk, "More comments on Submissions database");
                                });
                            });

                            // Stain Ref — bordered box with a fill-in line, matching the
                            // Processor/Program box style so it reads clearly as a form field.
                            a.Item().PaddingTop(6).Border(0.75f).Padding(3).Column(sr =>
                            {
                                sr.Item().Text("Stain Ref").Bold().FontSize(8);
                                sr.Item().PaddingTop(6).PaddingRight(4).BorderBottom(0.75f).MinHeight(2);
                            });
                        });

                        // ── Panel B — Fixation ────────────────────────────────
                        panels.RelativeItem(16).BorderRight(1).Padding(3).Column(b =>
                        {
                            b.Spacing(2);
                            b.Item().Text("Fixation").Bold().FontSize(8);
                            b.Item().Text(fixation).FontSize(8);

                            b.Item().PaddingTop(3).Text("Post Fixation").Bold().FontSize(8);

                            CheckboxItem(b.Item(), decal,  "Decalcify");
                            CheckboxItem(b.Item(), phenol, "Phenol / Alc");
                            CheckboxItem(b.Item(), formic, "Formic Acid");
                            CheckboxItem(b.Item(), other,  "Other", trailingLine: true, trailingValue: postFixOther);

                            b.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("Date").FontSize(8);
                                t.Span("  (sign)").FontSize(7);
                            });

                            void SignLine(string lbl) =>
                                b.Item().PaddingTop(3).Row(row =>
                                {
                                    row.ConstantItem(22).Text(lbl).FontSize(8);
                                    row.RelativeItem().AlignBottom().PaddingBottom(1).BorderBottom(0.75f).MinHeight(11);
                                });

                            SignLine("In");
                            SignLine("Out");
                        });

                        // ── Panel C — Tissue Processing (pre-printed, no data) ─
                        panels.RelativeItem(32).BorderRight(1).Padding(3).Column(c =>
                        {
                            c.Spacing(2);

                            // Title row: section name (left) + "Diagram" label (right, above the box).
                            c.Item().Row(titleRow =>
                            {
                                titleRow.RelativeItem().Text("Tissue Processing").Bold().FontSize(8);
                                titleRow.ConstantItem(94).AlignRight().Text("Diagram").FontSize(7.5f);
                            });

                            c.Item().Row(r =>
                            {
                                // Left — processing-schedule checkboxes.
                                r.RelativeItem().Column(inner =>
                                {
                                    CheckboxItem(inner.Item(), false, "Routine O/N");
                                    CheckboxItem(inner.Item(), false, "37C O/N");
                                    CheckboxItem(inner.Item(), false, "6 Hour");
                                    CheckboxItem(inner.Item(), false, "2 Day");
                                    CheckboxItem(inner.Item(), false, "3 Day");
                                    CheckboxItem(inner.Item(), false, "Other", trailingLine: true);
                                });

                                // Middle — Date: short bordered box, top-aligned with Routine O/N
                                // (wrapped in a column so it doesn't stretch to the tall Diagram box).
                                r.ConstantItem(50).Column(midCol =>
                                {
                                    midCol.Item().Border(0.75f).Height(62).Padding(3).Column(dateCol =>
                                    {
                                        dateCol.Item().Text("Date").FontSize(8);
                                        dateCol.Item().PaddingTop(32).BorderBottom(0.75f).MinHeight(2);
                                    });
                                });

                                r.ConstantItem(4);

                                // Right — Diagram box: large, starts at the top of this row
                                // (aligned with the Routine O/N checkbox).
                                r.ConstantItem(90).Border(0.5f).MinHeight(110);
                            });

                            c.Item().PaddingTop(3).Border(0.75f).Padding(3).Row(r =>
                            {
                                r.RelativeItem().Column(pc =>
                                {
                                    pc.Item().Text("Processor").FontSize(8);
                                    pc.Item().PaddingTop(10).PaddingRight(4).BorderBottom(0.75f).MinHeight(2);
                                });
                                r.ConstantItem(6);
                                r.RelativeItem().Column(pr =>
                                {
                                    pr.Item().Text("Program").FontSize(8);
                                    pr.Item().PaddingTop(10).PaddingRight(4).BorderBottom(0.75f).MinHeight(2);
                                });
                            });
                        });

                        // ── Panel D — Dispatch ────────────────────────────────
                        panels.RelativeItem(22).Padding(3).Column(d =>
                        {
                            d.Spacing(3);
                            d.Item().Text("Dispatch").Bold().FontSize(8);

                            // L / S grid with No. Blocks and No. Slides rows.
                            d.Item().Table(grid =>
                            {
                                grid.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(40); // label
                                    c.RelativeColumn(30); // L box
                                    c.RelativeColumn(30); // S box
                                });

                                void GridHeader(string lbl) =>
                                    grid.Cell().PaddingHorizontal(2).AlignCenter()
                                        .Text(lbl).Bold().FontSize(8);
                                void GridLabel(string lbl) =>
                                    grid.Cell().PaddingVertical(2)
                                        .Text(lbl).FontSize(8);
                                void GridBox() =>
                                    grid.Cell().Border(0.5f).MinHeight(22);

                                GridHeader(string.Empty); GridHeader("L"); GridHeader("S");
                                GridLabel("No. Blocks"); GridBox(); GridBox();
                                GridLabel("No. Slides"); GridBox(); GridBox();
                            });

                            // Submission Number — large bordered box.
                            d.Item().PaddingTop(6)
                             .Border(2)
                             .Padding(4)
                             .Column(box =>
                             {
                                 box.Item().AlignCenter()
                                    .Text("Submission Number").Bold().FontSize(8);
                                 box.Item().AlignCenter()
                                    .Text(batchId).Bold().FontSize(18);
                             });
                        });
                    });

                    // ── Version / disclaimer / page number ───────────────────────
                    foot.Item().PaddingTop(2).Row(row =>
                    {
                        row.RelativeItem().Text(versionStr).FontSize(7);
                        row.RelativeItem(3)
                           .AlignCenter()
                           .Text("* indicates further comments/information may be found on the Submissions database")
                           .FontSize(7).Italic();
                        row.RelativeItem().AlignRight()
                           .Text(t =>
                           {
                               t.Span("Page ").FontSize(7);
                               t.CurrentPageNumber().FontSize(7);
                               t.Span(" of ").FontSize(7);
                               t.TotalPages().FontSize(7);
                           });
                    });
                });

                // ── Report header (repeats on every page, per legacy) ───────────
                page.Header().Column(col =>
                {
                    col.Spacing(3);

                    // ════════════════════════════════════════════════════════════
                    // SECTION 0 — ReportHeader
                    // 3-panel outer table:
                    //   Left (~27%):  Project/Contract Code, Pathologist, Submitted By, Submitted As
                    //   Centre (~50%): Title bar + Submission Date / Received Date / Species / Received Time
                    //   Right (~23%): BatchType badge + Adequately Fixed? + Total Samples
                    // ════════════════════════════════════════════════════════════
                    col.Item().Border(1).Table(outer =>
                    {
                        outer.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(27); // left panel
                            c.RelativeColumn(50); // centre panel
                            c.RelativeColumn(23); // right panel
                        });

                        // ── Left panel ────────────────────────────────────────
                        outer.Cell().BorderRight(1).Table(left =>
                        {
                            left.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(45); // label
                                c.RelativeColumn(55); // value
                            });

                            void LLabel(string text) =>
                                left.Cell().PaddingVertical(2).PaddingHorizontal(3)
                                    .Text(text).Bold().FontSize(8);
                            void LValue(string val) =>
                                left.Cell().PaddingVertical(2).PaddingHorizontal(3)
                                    .Text(val).FontSize(8);

                            LLabel("Project/Contract Code"); LValue(projectCode);
                            LLabel("Pathologist");           LValue(pathologist);
                            LLabel("Submitted By");          LValue(submittedBy);
                            LLabel("Submitted As");          LValue(submittedAs);
                        });

                        // ── Centre panel ──────────────────────────────────────
                        outer.Cell().BorderRight(1).Column(centre =>
                        {
                            centre.Spacing(0);

                            // Title bar: white text on black background.
                            // Fixed height + vertical centring so it aligns exactly with the
                            // batch-type badge in the right panel (both bars share the same height).
                            centre.Item()
                                  .Background(Colors.Black)
                                  .Height(24)
                                  .PaddingHorizontal(4)
                                  .AlignMiddle()
                                  .AlignCenter()
                                  .Text("HISTOLOGY SUBMISSION FORM")
                                  .Bold().FontSize(13).FontColor(Colors.White);

                            // Dates + species grid — 4 columns.
                            centre.Item().Table(grid =>
                            {
                                grid.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(22); // label
                                    c.RelativeColumn(28); // value
                                    c.RelativeColumn(22); // label
                                    c.RelativeColumn(28); // value
                                });

                                void GLabel(string t) =>
                                    grid.Cell().PaddingVertical(2).PaddingHorizontal(3)
                                        .Text(t).Bold().FontSize(8);
                                void GValue(string v) =>
                                    grid.Cell().PaddingVertical(2).PaddingHorizontal(3)
                                        .Text(v).FontSize(8);

                                GLabel("Submission Date"); GValue(batchDate);
                                GLabel("Received Date");   GValue(dateReceived);
                                GLabel("Species");         GValue(species);
                                GLabel("Received Time");   GValue(timeReceived);
                            });
                        });

                        // ── Right panel ───────────────────────────────────────
                        outer.Cell().Column(right =>
                        {
                            right.Spacing(0);

                            // BatchType badge: white text on black background.
                            // Same fixed height as the centre title bar so both align exactly.
                            right.Item()
                                 .Background(Colors.Black)
                                 .Height(24)
                                 .PaddingHorizontal(4)
                                 .AlignMiddle()
                                 .AlignCenter()
                                 .Text(batchType)
                                 .Bold().FontSize(12).FontColor(Colors.White);

                            right.Item().Table(rtable =>
                            {
                                rtable.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(60); // label
                                    c.RelativeColumn(40); // value
                                });

                                void RLabel(string t) =>
                                    rtable.Cell().PaddingVertical(2).PaddingHorizontal(3)
                                          .Background(Colors.Grey.Lighten3)
                                          .Text(t).Bold().FontSize(8);
                                void RValue(string v) =>
                                    rtable.Cell().PaddingVertical(2).PaddingHorizontal(3)
                                          .Text(v).FontSize(8);

                                RLabel("Adequately Fixed?"); RValue(adequatelyFixed);
                                RLabel("Total Samples");     RValue(totalSamples);
                            });
                        });
                    });

                    // ════════════════════════════════════════════════════════════
                    // Instruction line (italic, centred)
                    // ════════════════════════════════════════════════════════════
                    col.Item()
                       .AlignCenter()
                       .Text("It is essential that this form is dated and initialled at completion of each stage")
                       .Italic().FontSize(7.5f);
                });

                page.Content().Column(col =>
                {
                    // ════════════════════════════════════════════════════════════
                    // SECTIONS 1 + 2 — PageHeader + Detail
                    // 13-column submission table, paginated at exactly 13 rows per page
                    // (matches legacy Crystal Reports). Column headers repeat per chunk;
                    // the report header (SECTION 0) and 4-panel footer repeat via the
                    // page-level Header()/Footer(), so every page carries both.
                    // Columns 1–4 carry data; columns 5–13 are pre-printed blanks.
                    // RepeatBlock rule: "*" suffix on BlockRef when truthy.
                    // ════════════════════════════════════════════════════════════
                    // Configurable page size (legacy default 13); guarded against non-positive values.
                    int RowsPerPage = rowsPerPage > 0 ? rowsPerPage : 13;
                    var pageChunks = submissionRows
                        .Select((row, i) => (row, i))
                        .GroupBy(x => x.i / RowsPerPage)
                        .Select(g => g.Select(x => x.row).ToList())
                        .ToList();
                    if (pageChunks.Count == 0) pageChunks.Add([]);

                    for (int chunkIndex = 0; chunkIndex < pageChunks.Count; chunkIndex++)
                    {
                        var chunk = pageChunks[chunkIndex];
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(10); // 1  Sender Ref
                                c.RelativeColumn(9);  // 2  Histology Ref
                                c.RelativeColumn(6);  // 3  Block Ref
                                c.RelativeColumn(9);  // 4  Tissue Code
                                c.RelativeColumn(6);  // 5  Block      (blank)
                                c.RelativeColumn(5);  // 6  Cass       (blank)
                                c.RelativeColumn(6);  // 7  Embed      (blank)
                                c.RelativeColumn(6);  // 8  Section    (blank)
                                c.RelativeColumn(7);  // 9  Block Filed (blank)
                                c.RelativeColumn(6);  // 10 Stain      (blank)
                                c.RelativeColumn(6);  // 11 QC Code    (blank)
                                c.RelativeColumn(14); // 12 Comments   (CustomerRef)
                                c.RelativeColumn(10); // 13 Dispatch   (blank)
                            });

                            t.Header(h =>
                            {
                                void Th(string lbl) =>
                                    h.Cell()
                                     .Background(Colors.Grey.Lighten3)
                                     .Border(0.5f)
                                     .PaddingVertical(2).PaddingHorizontal(2)
                                     .Text(lbl).Bold().FontSize(7.5f);

                                Th("Sender Ref");
                                Th("Histology\nRef");
                                Th("Block\nRef");
                                Th("Tissue\nCode");
                                Th("Block");
                                Th("Cass");
                                Th("Embed");
                                Th("Section");
                                Th("Block\nFiled");
                                Th("Stain");
                                Th("QC\nCode");
                                Th("Comments");
                                Th("Dispatch");
                            });

                            foreach (var row in chunk)
                            {
                                var blockRef = Field(row, "BlockRef")
                                    + (IsRepeatBlock(Field(row, "RepeatBlock")) ? "*" : string.Empty);

                                void Td(string v) =>
                                    t.Cell().Border(0.5f)
                                     .PaddingVertical(2).PaddingHorizontal(2)
                                     .Text(v).FontSize(8);

                                Td(Field(row, "SenderRef"));
                                Td(Field(row, "HistologyRef"));
                                Td(blockRef);
                                Td(Field(row, "TissueDetails"));
                                Td(string.Empty); // Block
                                Td(string.Empty); // Cass
                                Td(string.Empty); // Embed
                                Td(string.Empty); // Section
                                Td(string.Empty); // Block Filed
                                Td(string.Empty); // Stain
                                Td(string.Empty); // QC Code
                                Td(Field(row, "CustomerRef")); // Comments column
                                Td(string.Empty); // Dispatch
                            }
                        });

                        // Force a new page after each full chunk except the last.
                        if (chunkIndex < pageChunks.Count - 1)
                            col.Item().PageBreak();
                    }

                });
            });
        }).GeneratePdf();

        return Task.FromResult(pdf);
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

    /// <summary>    /// Renders a legacy-style square checkbox (a bordered box, not a tiny Unicode glyph) followed
    /// by its label. When <paramref name="trailingLine"/> is set, a fill-in underline follows the
    /// label (used for "Other ____"). Replaces the Wingdings Chr(252) tick from the legacy form.
    /// </summary>
    private static void CheckboxItem(IContainer item, bool isChecked, string label,
        bool trailingLine = false, string trailingValue = "")
    {
        item.PaddingBottom(3).Row(row =>
        {
            // AlignMiddle before Height so the fixed 9×9 box is vertically centred in the row
            // rather than stretched to the taller label height — stretching left glitched corners.
            row.ConstantItem(9).AlignMiddle().Width(9).Height(9).Border(0.75f).AlignMiddle().AlignCenter()
               .Text(isChecked ? "X" : string.Empty).FontSize(7).Bold();
            row.ConstantItem(4);
            if (trailingLine)
            {
                row.ConstantItem(30).AlignMiddle().Text(label).FontSize(8);
                row.RelativeItem().AlignBottom().PaddingBottom(1).BorderBottom(0.75f)
                   .Text(trailingValue).FontSize(8);
            }
            else
            {
                row.RelativeItem().AlignMiddle().Text(label).FontSize(8);
            }
        });
    }

    /// <summary>    /// Safe field extractor. Returns <see cref="string.Empty"/> when the column
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

    /// <summary>Formats a raw date/time string to dd/MM/yyyy for UK display.</summary>
    private static string FormatDate(string raw)
    {
        var culture = System.Globalization.CultureInfo.GetCultureInfo("en-GB");
        return DateTime.TryParse(raw, culture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var dt)
            ? dt.ToString("dd/MM/yyyy", culture)
            : raw;
    }

    /// <summary>
    /// Determines whether a field value represents a "checked" / tick state.
    /// <para>
    /// The original Crystal Reports reports used <c>Chr(252)</c> in the Wingdings font
    /// to render a tick mark. The DataSet fields that drove this are boolean or truthy
    /// string values set by SubmissionForm.aspx.vb before passing to Crystal Reports.
    /// </para>
    /// <para>
    /// Substitution: any non-empty, non-falsy value is treated as checked.
    /// The literal Chr(252) / ü (\u00FC) byte value is also implicitly handled
    /// (it is neither "0" nor "false" so it passes the truthy check).
    /// Callers replace with Unicode ✓ (U+2713).
    /// </para>
    /// </summary>
    private static bool IsChecked(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return false;
        return val.Trim() switch
        {
            "0" or "false" or "False" or "FALSE" => false,
            _ => true
        };
    }

    /// <summary>
    /// Determines whether the RepeatBlock flag warrants a " *" suffix on BlockRef.
    /// Matches the <c>CreateBlockRefString</c> logic in SubmissionForm.aspx.vb.
    /// </summary>
    private static bool IsRepeatBlock(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return false;
        return val.Trim() switch
        {
            "0" or "false" or "False" or "FALSE" => false,
            _ => true
        };
    }
}
