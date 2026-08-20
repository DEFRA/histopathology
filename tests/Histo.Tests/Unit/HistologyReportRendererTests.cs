using System.Data;
using Histo.Reporting.Reports;

namespace Histo.Tests.Unit;

/// <summary>
/// Standalone unit tests for <see cref="HistologyReportRenderer"/>.
///
/// No database, no web host — the DataSet is constructed in-memory matching
/// the HistologyReportDataset.xsd schema.
///
/// On each test run a PDF is written to the test output directory so it can
/// be opened manually and compared against the legacy Crystal Reports export.
/// </summary>
public class HistologyReportRendererTests
{
    // ── Shared DataSet factory ───────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal but representative DataSet that exercises every section
    /// of the HistologyReport:
    /// <list type="bullet">
    ///   <item>Batch header with long comments (CommentLengthOK checkmark)</item>
    ///   <item>BatchPostFixation with Formic tick</item>
    ///   <item>BatchHistology with two codes (sub-report section)</item>
    ///   <item>Two BatchSubmission rows — second has RepeatBlock set</item>
    ///   <item>Version footer string</item>
    /// </list>
    /// </summary>
    private static DataSet BuildTestDataSet(int batchId = 42)
    {
        var ds = new DataSet("HistologyReport");

        // ── Batch ────────────────────────────────────────────────────────────
        var batch = new DataTable("Batch");
        batch.Columns.Add("ProjectContractCode");
        batch.Columns.Add("ContactName");
        batch.Columns.Add("BatchDate");
        batch.Columns.Add("Species");
        batch.Columns.Add("DateReceived");
        batch.Columns.Add("TimeReceived");
        batch.Columns.Add("SafeToHandle", typeof(bool));
        batch.Columns.Add("Comments");
        batch.Columns.Add("Fixation");
        batch.Columns.Add("ID");
        batch.Columns.Add("PostFixationOther");
        batch.Columns.Add("CommentLengthOK");
        batch.Columns.Add("OtherSubmittedBy");
        batch.Columns.Add("NumberSamples");
        batch.Columns.Add("BatchType");
        batch.Columns.Add("SubmittedAs");
        batch.Columns.Add("MoreHistology");
        var br = batch.NewRow();
        br["ID"]                 = batchId.ToString();
        br["ProjectContractCode"] = "TBA";
        br["ContactName"]        = "A. Schock";          // Pathologist
        br["BatchDate"]          = "04/07/2011";          // Submission Date
        br["Species"]            = "Porcine";
        br["DateReceived"]       = "04/07/2011";
        br["TimeReceived"]       = "before 11.00";
        br["SafeToHandle"]       = true;                  // Adequately Fixed? → Yes
        br["Comments"]           = "Blocks & Slides returned to Lasswade 13.7.11 HS";
        br["CommentLengthOK"]    = "";                   // short comment — no overflow flag
        br["Fixation"]           = "Other";
        br["PostFixationOther"]  = "";
        br["OtherSubmittedBy"]   = "VLA Lasswade";        // Submitted By
        br["NumberSamples"]      = "1";
        br["BatchType"]          = "NON TSE";
        br["SubmittedAs"]        = "Wax Block";
        br["MoreHistology"]      = "";
        batch.Rows.Add(br);
        ds.Tables.Add(batch);

        // ── BatchPostFixation ────────────────────────────────────────────────
        var postFix = new DataTable("BatchPostFixation");
        postFix.Columns.Add("BatchID");
        postFix.Columns.Add("Decal");
        postFix.Columns.Add("Phenol");
        postFix.Columns.Add("Formic");
        postFix.Columns.Add("Other");
        var pfr = postFix.NewRow();
        pfr["BatchID"] = batchId.ToString();
        pfr["Formic"]  = "1"; // truthy → renderer emits ✓
        postFix.Rows.Add(pfr);
        ds.Tables.Add(postFix);

        // ── BatchHistology (sub-report section) ──────────────────────────────
        var histology = new DataTable("BatchHistology");
        histology.Columns.Add("BatchID", typeof(int));
        histology.Columns.Add("Code");
        var h1 = histology.NewRow(); h1["BatchID"] = batchId; h1["Code"] = "H1N2";
        var h2 = histology.NewRow(); h2["BatchID"] = batchId; h2["Code"] = "PAS";
        histology.Rows.Add(h1);
        histology.Rows.Add(h2);
        ds.Tables.Add(histology);

        // ── BatchSubmission ──────────────────────────────────────────────────
        var submissions = new DataTable("BatchSubmission");
        submissions.Columns.Add("BatchID");
        submissions.Columns.Add("SenderRef");
        submissions.Columns.Add("HistologyRef");
        submissions.Columns.Add("BlockRef");
        submissions.Columns.Add("TissueDetails");
        submissions.Columns.Add("RepeatBlock");
        submissions.Columns.Add("CustomerRef");

        var s1 = submissions.NewRow();
        s1["BatchID"]      = batchId.ToString();
        s1["SenderRef"]    = "15/P220/06/11";
        s1["HistologyRef"] = "11/42879";
        s1["BlockRef"]     = "01";
        s1["TissueDetails"] = "Viscera";
        s1["RepeatBlock"]  = "";
        s1["CustomerRef"]  = "visc 1";   // populates the Comments column
        submissions.Rows.Add(s1);

        var s2 = submissions.NewRow();
        s2["BatchID"]      = batchId.ToString();
        s2["SenderRef"]    = "15/P220/06/11";
        s2["HistologyRef"] = "11/42879";
        s2["BlockRef"]     = "02";
        s2["TissueDetails"] = "Viscera";
        s2["RepeatBlock"]  = "1";
        s2["CustomerRef"]  = "visc 2";
        submissions.Rows.Add(s2);
        ds.Tables.Add(submissions);

        // ── Version ──────────────────────────────────────────────────────────
        var version = new DataTable("Version");
        version.Columns.Add("Version");
        var vr = version.NewRow(); vr["Version"] = "Form: PAT1 123  version 4  (14/08/06)";
        version.Rows.Add(vr);
        ds.Tables.Add(version);

        return ds;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RenderAsync_FullDataSet_ReturnsPdf()
    {
        var renderer = new HistologyReportRenderer();
        var ds       = BuildTestDataSet();

        var pdf = await renderer.RenderAsync(ds);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0, "PDF byte array should not be empty.");
        // PDF files start with the %PDF- magic bytes
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }

    [Fact]
    public async Task RenderAsync_WritesReadablePdfToDisk()
    {
        var renderer  = new HistologyReportRenderer();
        var ds        = BuildTestDataSet();
        var outputPath = Path.Combine(
            Path.GetDirectoryName(typeof(HistologyReportRendererTests).Assembly.Location)!,
            "HistologyReport-Test.pdf");

        var pdf = await renderer.RenderAsync(ds);
        await File.WriteAllBytesAsync(outputPath, pdf);

        Assert.True(File.Exists(outputPath), "PDF file should exist on disk.");
        Assert.True(new FileInfo(outputPath).Length > 0, "PDF file should not be empty.");

        // Emit the path so it appears in test output for manual inspection
        Console.WriteLine($"PDF written to: {outputPath}");
    }

    [Fact]
    public async Task RenderAsync_EmptySubmissionRows_StillProducesPdf()
    {
        var renderer = new HistologyReportRenderer();
        var ds       = BuildTestDataSet();

        // Clear submission rows — report should still render without throwing
        ds.Tables["BatchSubmission"]!.Rows.Clear();

        var pdf = await renderer.RenderAsync(ds);

        Assert.True(pdf.Length > 0);
    }

    [Fact]
    public async Task RenderAsync_EmptyHistologyRows_StillProducesPdf()
    {
        var renderer = new HistologyReportRenderer();
        var ds       = BuildTestDataSet();

        // Clear histology rows — sub-report section should render empty without throwing
        ds.Tables["BatchHistology"]!.Rows.Clear();

        var pdf = await renderer.RenderAsync(ds);

        Assert.True(pdf.Length > 0);
    }
}
