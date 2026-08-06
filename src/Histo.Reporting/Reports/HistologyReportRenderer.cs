// Stage 3 — HtmlToPdfConversionSkill
// Runtime paradigm : modern (net10.0, ASP.NET Core)
// PDF engine       : QuestPDF Community Edition (no CrystalDecisions, no paid engine)
// Source definition: output/definition/HistologyReport.ReportDefinition.json
// Source template  : output/templates/HistologyReport.html
// Generated        : 2026-08-05
// Status           : stub — callingPageContext absent (Histo.Reporting is a stub project)
//
// TODO: wire data source — re-run Stage 3 after migration completes.
//       The calling controller/page must construct and populate the DataSet before
//       calling RenderAsync(ds). See HistologyReportDataset.xsd for the full schema.
//
// Gate checks (all passing):
//   ✓ No CrystalDecisions.* imports
//   ✓ No paid PDF engine
//   ✓ QuestPDF.Settings.License = LicenseType.Community set in static constructor
//   ✓ RenderAsync(DataSet ds) signature exposed
//   ✓ HistologySubReport section composed inline — no separate render pass (ADR-004)
//   ✓ Wingdings Chr(252) → Unicode ✓ (U+2713) substitution
//   ✓ RepeatBlock → " *" appended to BlockRef when truthy

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
    /// Renders the report to a PDF byte array.
    /// <para>
    /// TODO: wire data source — re-run Stage 3 after migration completes.
    /// The calling controller/page must populate <paramref name="ds"/> using the
    /// existing business-layer data methods before invoking this renderer.
    /// </para>
    /// </summary>
    /// <param name="ds">Populated DataSet matching HistologyReportDataset.xsd.</param>
    /// <returns>PDF content as a byte array.</returns>
    public Task<byte[]> RenderAsync(DataSet ds)
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

        byte[] pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(12,    Unit.Millimetre);
                page.MarginBottom(12, Unit.Millimetre);
                page.MarginLeft(12,   Unit.Millimetre);
                page.MarginRight(12,  Unit.Millimetre);
                page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(9));

                // ── Section 4 — PageFooter ───────────────────────────────────────
                // Version string sourced from Version DataTable row 0.
                page.Footer()
                    .BorderTop(0.5f).PaddingTop(4)
                    .Text($"Version: {versionStr}").FontSize(8);

                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    // ── Section 0 — ReportHeader ─────────────────────────────────

                    // Report title: bold 12pt, centered, separated from content by bottom border.
                    col.Item()
                       .BorderBottom(2).PaddingBottom(4)
                       .AlignCenter()
                       .Text("Histology Report")
                       .Bold().FontSize(12);

                    // Batch-level form fields — two-column label/value layout.
                    if (batch is not null)
                    {
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(22); // label col A
                                c.RelativeColumn(28); // value col A
                                c.RelativeColumn(22); // label col B
                                c.RelativeColumn(28); // value col B
                            });

                            void Label(string text) =>
                                t.Cell().PaddingVertical(2).PaddingHorizontal(4)
                                 .Text(text).Bold();

                            void Value(string val) =>
                                t.Cell().PaddingVertical(2).PaddingHorizontal(4).Text(val);

                            Label("Project/Contract Code:"); Value(Field(batch, "ProjectContractCode"));
                            Label("Contact Name:");           Value(Field(batch, "ContactName"));
                            Label("Batch Date:");             Value(FormatDate(Field(batch, "BatchDate")));
                            Label("Species:");                Value(Field(batch, "Species"));
                            Label("Date Received:");          Value(FormatDate(Field(batch, "DateReceived")));
                            Label("Time Received:");          Value(Field(batch, "TimeReceived"));
                            Label("Safe to Handle:");         Value(Field(batch, "SafeToHandle"));
                            Label("Batch Type:");             Value(Field(batch, "BatchType"));
                            Label("Submitted As:");           Value(Field(batch, "SubmittedAs"));
                            Label("Other Submitted By:");     Value(Field(batch, "OtherSubmittedBy"));
                            Label("No. of Samples:");         Value(Field(batch, "NumberSamples"));
                            Label("Fixation:");               Value(Field(batch, "Fixation"));
                            Label("Post Fixation Other:");    Value(Field(batch, "PostFixationOther"));
                            Label("More Histology:");         Value(Field(batch, "MoreHistology"));

                            // Comments + CommentLengthOK checkmark.
                            // Original Crystal Reports: Chr(252) rendered in Wingdings font.
                            // Substitution: Unicode ✓ (U+2713) appended when field is truthy.
                            var checkmark    = IsChecked(Field(batch, "CommentLengthOK")) ? " \u2713" : string.Empty;
                            var commentsText = Field(batch, "Comments") + checkmark;

                            Label("Comments:");
                            // Span 3 remaining columns (value col A + label col B + value col B)
                            t.Cell().ColumnSpan(3)
                             .PaddingVertical(2).PaddingHorizontal(4)
                             .Text(commentsText);
                        });

                        // Post-fixation checkmarks: Decal / Phenol / Formic / Other.
                        // Original: Chr(252) Wingdings tick rendered when post-fixation type selected.
                        // Substitution: Unicode ✓ (U+2713) when field value is truthy; blank otherwise.
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(); c.RelativeColumn();
                                c.RelativeColumn(); c.RelativeColumn();
                            });

                            t.Header(header =>
                            {
                                void Th(string lbl) =>
                                    header.Cell()
                                          .Background(Colors.Grey.Lighten2)
                                          .Border(0.5f)
                                          .AlignCenter()
                                          .PaddingVertical(2).PaddingHorizontal(10)
                                          .Text(lbl).Bold();

                                Th("Decal"); Th("Phenol"); Th("Formic"); Th("Other");
                            });

                            void Tick(string val) =>
                                t.Cell().Border(0.5f).AlignCenter()
                                 .PaddingVertical(2).PaddingHorizontal(10)
                                 .Text(IsChecked(val) ? "\u2713" : string.Empty);

                            Tick(postFix is not null ? Field(postFix, "Decal")  : string.Empty);
                            Tick(postFix is not null ? Field(postFix, "Phenol") : string.Empty);
                            Tick(postFix is not null ? Field(postFix, "Formic") : string.Empty);
                            Tick(postFix is not null ? Field(postFix, "Other")  : string.Empty);
                        });
                    }

                    // ── HistologySubReport — inlined within Document.Create() ─────
                    //
                    // ADR-004: HistologyReport.rpt embeds HistologySubReport.rpt as a
                    // sub-report positioned within ReportHeader (Section 0), linked via
                    // Batch.ID → BatchHistology.BatchID.
                    //
                    // In QuestPDF there is no concept of sub-reports as separate documents.
                    // The sub-report is composed as a QuestPDF Table appended directly to
                    // this Column — matching the inline structure in HistologyReport.html.
                    // No call to HistologySubReportRenderer is made; data from BatchHistology
                    // is consumed here directly from the parent DataSet.
                    //
                    // The BatchHistology table is assumed pre-filtered to the current batch
                    // by the calling page (matching the Crystal Reports implicit link behaviour).
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

                    // ── Sections 1 + 2 — PageHeader + Detail (submission rows) ────
                    //
                    // Header row repeats on each printed page (QuestPDF table header
                    // behaviour matches renderHints.repeatHeaderOnEachPage in template).
                    //
                    // RepeatBlock rule: if BatchSubmission.RepeatBlock is non-empty and
                    // not a falsy value ("0", "false", "False") then " *" is appended to
                    // the BlockRef display value — matching CreateBlockRefString logic in
                    // the original SubmissionForm.aspx.vb.
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(15); // Sender Ref
                            c.RelativeColumn(15); // Histology Ref
                            c.RelativeColumn(15); // Block Ref
                            c.RelativeColumn(35); // Tissue Details
                            c.RelativeColumn(20); // Customer Ref
                        });

                        t.Header(header =>
                        {
                            void Th(string lbl) =>
                                header.Cell()
                                      .Background(Colors.Grey.Lighten2)
                                      .Border(0.5f)
                                      .PaddingVertical(3).PaddingHorizontal(5)
                                      .Text(lbl).Bold();

                            Th("Sender Ref"); Th("Histology Ref"); Th("Block Ref");
                            Th("Tissue Details"); Th("Customer Ref");
                        });

                        foreach (var row in submissionRows)
                        {
                            var blockRef = Field(row, "BlockRef")
                                + (IsRepeatBlock(Field(row, "RepeatBlock")) ? " *" : string.Empty);

                            t.Cell().Border(0.5f).PaddingVertical(3).PaddingHorizontal(5).Text(Field(row, "SenderRef"));
                            t.Cell().Border(0.5f).PaddingVertical(3).PaddingHorizontal(5).Text(Field(row, "HistologyRef"));
                            t.Cell().Border(0.5f).PaddingVertical(3).PaddingHorizontal(5).Text(blockRef);
                            t.Cell().Border(0.5f).PaddingVertical(3).PaddingHorizontal(5).Text(Field(row, "TissueDetails"));
                            t.Cell().Border(0.5f).PaddingVertical(3).PaddingHorizontal(5).Text(Field(row, "CustomerRef"));
                        }
                    });

                    // Section 3 — ReportFooter: no elements defined in ReportDefinition.json.
                });
            });
        }).GeneratePdf();

        return Task.FromResult(pdf);
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

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

    /// <summary>Formats a raw date/time string to dd/MM/yyyy for UK display.</summary>
    private static string FormatDate(string raw) =>
        DateTime.TryParse(raw, out var dt) ? dt.ToString("dd/MM/yyyy") : raw;

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
