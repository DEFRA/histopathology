using System.Data;
using Histo.Reporting.Reports;

namespace Histo.Tests.Unit;

/// <summary>
/// Standalone unit tests for <see cref="HistologySubReportRenderer"/>.
///
/// No database, no web host — the DataSet is constructed in-memory with a
/// single <c>BatchHistology</c> table (columns: BatchID int, Code string).
///
/// In production the sub-report is rendered inline within
/// <see cref="HistologyReportRenderer"/>. This renderer exists for independent
/// unit/preview use only (per ADR-004).
///
/// On each test run a PDF is written to the test output directory so it can
/// be opened manually and compared against the inlined section.
/// </summary>
public class HistologySubReportRendererTests
{
    private static DataSet BuildTestDataSet(params string[] codes)
    {
        var ds = new DataSet("HistologySubReport");

        var histology = new DataTable("BatchHistology");
        histology.Columns.Add("BatchID", typeof(int));
        histology.Columns.Add("Code");

        foreach (var code in codes)
        {
            var r = histology.NewRow();
            r["BatchID"] = 42;
            r["Code"]    = code;
            histology.Rows.Add(r);
        }

        ds.Tables.Add(histology);
        return ds;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RenderAsync_EmptyBatchHistologyTable_ReturnsPdf()
    {
        var renderer = new HistologySubReportRenderer();
        var ds       = BuildTestDataSet(); // zero rows

        var pdf = await renderer.RenderAsync(ds);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 0, "PDF byte array should not be empty.");
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }

    [Fact]
    public async Task RenderAsync_MultipleHistologyCodes_ReturnsPdf()
    {
        var renderer = new HistologySubReportRenderer();
        var ds       = BuildTestDataSet("H&E", "PAS", "MT");

        var pdf = await renderer.RenderAsync(ds);

        Assert.True(pdf.Length > 0, "PDF with 3 histology codes should not be empty.");
        Assert.Equal((byte)'%', pdf[0]);
    }

    [Fact]
    public async Task RenderAsync_WritesReadablePdfToDisk()
    {
        var renderer   = new HistologySubReportRenderer();
        var ds         = BuildTestDataSet("H&E", "PAS");
        var outputPath = Path.Combine(
            Path.GetDirectoryName(typeof(HistologySubReportRendererTests).Assembly.Location)!,
            "HistologySubReport-Test.pdf");

        var pdf = await renderer.RenderAsync(ds);
        await File.WriteAllBytesAsync(outputPath, pdf);

        Assert.True(File.Exists(outputPath), "PDF file should exist on disk.");
        Assert.True(new FileInfo(outputPath).Length > 0, "PDF file should not be empty.");

        Console.WriteLine($"PDF written to: {outputPath}");
    }

    [Fact]
    public async Task RenderAsync_MissingBatchHistologyTable_DoesNotThrow()
    {
        var renderer = new HistologySubReportRenderer();
        // DataSet with no BatchHistology table at all — renderer should fall back to empty list
        var ds = new DataSet("HistologySubReport");

        var pdf = await renderer.RenderAsync(ds);

        Assert.True(pdf.Length > 0, "PDF should render even when BatchHistology table is absent.");
    }
}
