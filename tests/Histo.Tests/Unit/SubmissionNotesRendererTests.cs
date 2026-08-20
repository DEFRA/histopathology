using System.Data;
using Histo.Reporting.Reports;

namespace Histo.Tests.Unit;

/// <summary>
/// Standalone unit tests for <see cref="SubmissionNotesRenderer"/>.
///
/// No database, no web host — the DataSet is constructed in-memory matching
/// the SubmissionNotesDataset.xsd schema (6 tables).
///
/// Seed values are taken from <c>docs/pdf-op-compare/SubmissionNotes-image.png</c>:
///   SubmissionNumber=29401, SenderRef=PD0573/93, TissueCode=ABCESS, TissueComment=Test.
///
/// On each test run a PDF is written to the test output directory so it can
/// be opened manually and compared against the legacy Crystal Reports export.
/// </summary>
public class SubmissionNotesRendererTests
{
    // ── Shared DataSet factory ───────────────────────────────────────────────

    /// <summary>
    /// Builds a representative DataSet with all 6 tables populated.
    /// Each comment section has at least one row so all sections render.
    /// </summary>
    private static DataSet BuildTestDataSet()
    {
        var ds = new DataSet("SubmissionNotes");

        // ── Submission ───────────────────────────────────────────────────────
        var submission = new DataTable("Submission");
        submission.Columns.Add("SubmissionNumber");
        submission.Columns.Add("SubmissionComments");
        submission.Columns.Add("SubmissionStatusComment");
        var sr = submission.NewRow();
        sr["SubmissionNumber"]        = "29401";
        sr["SubmissionComments"]      = "Test submission comment";
        sr["SubmissionStatusComment"] = "Test status comment";
        submission.Rows.Add(sr);
        ds.Tables.Add(submission);

        // ── SubmissionTissues ────────────────────────────────────────────────
        var tissues = new DataTable("SubmissionTissues");
        tissues.Columns.Add("SenderRef");
        tissues.Columns.Add("TissueCode");
        tissues.Columns.Add("TissueComment");
        tissues.Columns.Add("TissueArchiveComment");
        var tr = tissues.NewRow();
        tr["SenderRef"]            = "PD0573/93";
        tr["TissueCode"]           = "ABCESS";
        tr["TissueComment"]        = "Test";
        tr["TissueArchiveComment"] = "";
        tissues.Rows.Add(tr);
        ds.Tables.Add(tissues);

        // ── SubmissionBlocks ─────────────────────────────────────────────────
        var blocks = new DataTable("SubmissionBlocks");
        blocks.Columns.Add("SenderRef");
        blocks.Columns.Add("BlockRef");
        blocks.Columns.Add("BlockComment");
        blocks.Columns.Add("BlockArchiveComment");
        var br = blocks.NewRow();
        br["SenderRef"]          = "PD0573/93";
        br["BlockRef"]           = "BLK-001";
        br["BlockComment"]       = "Test block comment";
        br["BlockArchiveComment"] = "";
        blocks.Rows.Add(br);
        ds.Tables.Add(blocks);

        // ── BlockHistology ───────────────────────────────────────────────────
        var histology = new DataTable("BlockHistology");
        histology.Columns.Add("BlockRef");
        histology.Columns.Add("Test");
        histology.Columns.Add("TestComment");
        histology.Columns.Add("TestArchiveComment");
        var hr = histology.NewRow();
        hr["BlockRef"]           = "BLK-001";
        hr["Test"]               = "H&E";
        hr["TestComment"]        = "Test histology comment";
        hr["TestArchiveComment"] = "";
        histology.Rows.Add(hr);
        ds.Tables.Add(histology);

        // ── BlockSpecialStain ────────────────────────────────────────────────
        var stains = new DataTable("BlockSpecialStain");
        stains.Columns.Add("BlockRef");
        stains.Columns.Add("Test");
        stains.Columns.Add("TestComment");
        stains.Columns.Add("TestArchiveComment");
        var str2 = stains.NewRow();
        str2["BlockRef"]           = "BLK-001";
        str2["Test"]               = "PAS";
        str2["TestComment"]        = "Test stain comment";
        str2["TestArchiveComment"] = "";
        stains.Rows.Add(str2);
        ds.Tables.Add(stains);

        // ── BlockAntibodies ──────────────────────────────────────────────────
        var antibodies = new DataTable("BlockAntibodies");
        antibodies.Columns.Add("BlockRef");
        antibodies.Columns.Add("Test");
        antibodies.Columns.Add("TestComment");
        antibodies.Columns.Add("TestArchiveComment");
        var ar = antibodies.NewRow();
        ar["BlockRef"]           = "BLK-001";
        ar["Test"]               = "IHC-CD3";
        ar["TestComment"]        = "Test antibody comment";
        ar["TestArchiveComment"] = "";
        antibodies.Rows.Add(ar);
        ds.Tables.Add(antibodies);

        return ds;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RenderAsync_FullDataSet_ReturnsPdf()
    {
        var renderer = new SubmissionNotesRenderer();
        var ds       = BuildTestDataSet();

        var pdf = await renderer.RenderAsync(ds);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0, "PDF byte array should not be empty.");
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }

    [Fact]
    public async Task RenderAsync_WritesReadablePdfToDisk()
    {
        var renderer   = new SubmissionNotesRenderer();
        var ds         = BuildTestDataSet();
        var outputPath = Path.Combine(
            Path.GetDirectoryName(typeof(SubmissionNotesRendererTests).Assembly.Location)!,
            "SubmissionNotes-Test.pdf");

        var pdf = await renderer.RenderAsync(ds);
        await File.WriteAllBytesAsync(outputPath, pdf);

        Assert.True(File.Exists(outputPath), "PDF file should exist on disk.");
        Assert.True(new FileInfo(outputPath).Length > 0, "PDF file should not be empty.");

        Console.WriteLine($"PDF written to: {outputPath}");
    }

    [Fact]
    public async Task RenderAsync_AllCommentSectionsEmpty_SkipsThemAndStillProducesPdf()
    {
        var renderer = new SubmissionNotesRenderer();
        var ds       = BuildTestDataSet();

        // Clear all 5 detail sections — only Submission header row remains
        ds.Tables["SubmissionTissues"]!.Rows.Clear();
        ds.Tables["SubmissionBlocks"]!.Rows.Clear();
        ds.Tables["BlockHistology"]!.Rows.Clear();
        ds.Tables["BlockSpecialStain"]!.Rows.Clear();
        ds.Tables["BlockAntibodies"]!.Rows.Clear();

        var pdf = await renderer.RenderAsync(ds);

        Assert.True(pdf.Length > 0, "PDF should render even when all comment sections are empty.");
    }

    [Fact]
    public async Task RenderAsync_OnlyTissueSection_RendersOnlyThatSection()
    {
        var renderer = new SubmissionNotesRenderer();
        var ds       = BuildTestDataSet();

        // Clear all sections except tissues
        ds.Tables["SubmissionBlocks"]!.Rows.Clear();
        ds.Tables["BlockHistology"]!.Rows.Clear();
        ds.Tables["BlockSpecialStain"]!.Rows.Clear();
        ds.Tables["BlockAntibodies"]!.Rows.Clear();

        var pdf = await renderer.RenderAsync(ds);

        Assert.True(pdf.Length > 0, "PDF should render with only tissue section populated.");
    }
}
