using System.Data;
using Histo.Reporting.Reports;

namespace Histo.Tests.Unit;

/// <summary>
/// Standalone unit tests for <see cref="QCNoteRenderer"/>.
///
/// No database, no web host — the DataSet is constructed in-memory matching
/// the QCNoteDataset.xsd schema (single table "Header", 8 columns).
///
/// Seed values are taken from <c>docs/pdf-op-compare/QCNote-image.png</c>:
///   QCNoteRef=1907, Submission=24243, Project=SE1931, Species=Ovine,
///   StainRef="IHC 111 an", CreatedBy="Linda Powell", DateCreated="11 January 2017".
///
/// On each test run a PDF is written to the test output directory so it can
/// be opened manually and compared against the legacy Crystal Reports export.
/// </summary>
public class QCNoteRendererTests
{
    // ── Shared DataSet factory ───────────────────────────────────────────────

    /// <summary>
    /// Builds a representative DataSet matching the QCNote "Header" table schema.
    /// </summary>
    private static DataSet BuildTestDataSet()
    {
        var ds = new DataSet("QCNote");

        var header = new DataTable("Header");
        header.Columns.Add("QCNoteRef");
        header.Columns.Add("SubmissionNumber");
        header.Columns.Add("Project");
        header.Columns.Add("Species");
        header.Columns.Add("StainRef");
        header.Columns.Add("QCText");
        header.Columns.Add("CreatedBy");
        header.Columns.Add("DateCreated");

        var row = header.NewRow();
        row["QCNoteRef"]        = "1907";
        row["SubmissionNumber"] = "24243";
        row["Project"]          = "SE1931";
        row["Species"]          = "Ovine";
        row["StainRef"]         = "IHC 111 an";
        row["QCText"]           = "Due to repeat of staining block has become thin therefore some tissue missing.";
        row["CreatedBy"]        = "Linda Powell";
        row["DateCreated"]      = "11 January 2017";
        header.Rows.Add(row);

        ds.Tables.Add(header);
        return ds;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RenderAsync_FullDataSet_ReturnsPdf()
    {
        var renderer = new QCNoteRenderer();
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
        var renderer   = new QCNoteRenderer();
        var ds         = BuildTestDataSet();
        var outputPath = Path.Combine(
            Path.GetDirectoryName(typeof(QCNoteRendererTests).Assembly.Location)!,
            "QCNote-Test.pdf");

        var pdf = await renderer.RenderAsync(ds);
        await File.WriteAllBytesAsync(outputPath, pdf);

        Assert.True(File.Exists(outputPath), "PDF file should exist on disk.");
        Assert.True(new FileInfo(outputPath).Length > 0, "PDF file should not be empty.");

        Console.WriteLine($"PDF written to: {outputPath}");
    }

    [Fact]
    public async Task RenderAsync_EmptyQCText_StillProducesPdf()
    {
        var renderer = new QCNoteRenderer();
        var ds       = BuildTestDataSet();

        ds.Tables["Header"]!.Rows[0]["QCText"] = string.Empty;

        var pdf = await renderer.RenderAsync(ds);

        Assert.True(pdf.Length > 0, "PDF should render even with empty QCText.");
    }

    [Fact]
    public async Task RenderAsync_EmptyStainRef_StillProducesPdf()
    {
        var renderer = new QCNoteRenderer();
        var ds       = BuildTestDataSet();

        ds.Tables["Header"]!.Rows[0]["StainRef"] = string.Empty;

        var pdf = await renderer.RenderAsync(ds);

        Assert.True(pdf.Length > 0, "PDF should render even with empty StainRef.");
    }
}
