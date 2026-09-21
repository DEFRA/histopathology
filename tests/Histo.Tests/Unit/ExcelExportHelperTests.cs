using ExcelDataReader;
using Histo.Web;

namespace Histo.Tests.Unit;

/// <summary>
/// Validates <see cref="ExcelExportHelper"/> by reading the generated .xlsx back via
/// ExcelDataReader (the recommended reading/validation counterpart to ClosedXML) and
/// asserting header order, row count, and typed cell values round-trip correctly.
/// </summary>
public class ExcelExportHelperTests
{
    [Fact]
    public void BuildXlsx_ProducesValidXlsxFile()
    {
        var result = ExcelExportHelper.BuildXlsx(
            "test.xlsx",
            ["Name", "Count"],
            new (string, int)[] { ("Alpha", 1), ("Beta", 2) }.Select(r => (IReadOnlyList<object?>)new object?[] { r.Item1, r.Item2 }));

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
        Assert.Equal("test.xlsx", result.FileDownloadName);
        Assert.True(result.FileContents.Length > 0);
    }

    [Fact]
    public void BuildXlsx_RowCountMatchesInput_NotJustAPage()
    {
        var rows = Enumerable.Range(1, 37)
            .Select(i => (IReadOnlyList<object?>)new object?[] { i, $"Row {i}" });

        var result = ExcelExportHelper.BuildXlsx("many-rows.xlsx", ["ID", "Label"], rows);

        using var stream = new MemoryStream(result.FileContents);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var dataSet = reader.AsDataSet();
        var table = dataSet.Tables[0];

        // Row 0 is the header; data rows follow — 37 input rows, ignoring any paging.
        Assert.Equal(38, table.Rows.Count);
        Assert.Equal("ID", table.Rows[0][0]);
        Assert.Equal("Label", table.Rows[0][1]);
        Assert.Equal(37d, table.Rows[37][0]);
        Assert.Equal("Row 37", table.Rows[37][1]);
    }

    [Fact]
    public void BuildXlsx_DateValue_ReadableAsRealDate()
    {
        var date = new DateTime(2026, 9, 21);
        var rows = new[] { (IReadOnlyList<object?>)new object?[] { date } };

        var result = ExcelExportHelper.BuildXlsx("dates.xlsx", ["Date"], rows);

        using var stream = new MemoryStream(result.FileContents);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var table = reader.AsDataSet().Tables[0];

        Assert.Equal(date, Convert.ToDateTime(table.Rows[1][0]));
    }

    [Fact]
    public void BuildXlsx_NullValue_RendersAsBlankCell()
    {
        var rows = new[] { (IReadOnlyList<object?>)new object?[] { null, "present" } };

        var result = ExcelExportHelper.BuildXlsx("nulls.xlsx", ["A", "B"], rows);

        using var stream = new MemoryStream(result.FileContents);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var table = reader.AsDataSet().Tables[0];

        Assert.Equal(string.Empty, table.Rows[1][0].ToString());
        Assert.Equal("present", table.Rows[1][1]);
    }

    [Fact]
    public void BuildXlsx_AppendsXlsxExtension_WhenMissing()
    {
        var result = ExcelExportHelper.BuildXlsx("no-extension", ["A"], []);
        Assert.Equal("no-extension.xlsx", result.FileDownloadName);
    }
}
