using Histo.Infrastructure;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="DateTimeTypeHandler"/>/<see cref="NullableDateTimeTypeHandler"/> —
/// locks in that the broadened short-date formats (added to fix ViewImportedData's "search not
/// working" bug) are purely additive: every previously-working format/null/passthrough case still
/// behaves identically, new short-date variants now succeed, and genuinely invalid strings still throw.
/// </summary>
public class DapperTypeHandlersTests
{
    private readonly NullableDateTimeTypeHandler _nullableHandler = new();
    private readonly DateTimeTypeHandler _handler = new();

    [Theory]
    [InlineData("09/02/2001", 2001, 2, 9)]   // original zero-padded dd/MM/yyyy format — unaffected
    [InlineData("9/3/04", 2004, 3, 9)]       // new: unpadded day/month, 2-digit year
    [InlineData("31/12/1999", 1999, 12, 31)] // original format, year below the 2-digit pivot
    public void NullableHandler_Parse_KnownFormats_ParsesCorrectly(string input, int year, int month, int day)
    {
        var result = _nullableHandler.Parse(input);

        Assert.Equal(new DateTime(year, month, day), result);
    }

    [Fact]
    public void NullableHandler_Parse_DBNull_ReturnsNull()
    {
        Assert.Null(_nullableHandler.Parse(DBNull.Value));
    }

    [Fact]
    public void NullableHandler_Parse_EmptyString_ReturnsNull()
    {
        Assert.Null(_nullableHandler.Parse("   "));
    }

    [Fact]
    public void NullableHandler_Parse_NativeDateTime_ReturnedAsIs()
    {
        var dt = new DateTime(2020, 5, 1);

        Assert.Equal(dt, _nullableHandler.Parse(dt));
    }

    [Fact]
    public void NullableHandler_Parse_InvalidString_StillThrows()
    {
        Assert.ThrowsAny<FormatException>(() => _nullableHandler.Parse("not-a-date"));
    }

    [Theory]
    [InlineData("09/02/2001", 2001, 2, 9)]
    [InlineData("6/4/04", 2004, 4, 6)]
    public void Handler_Parse_KnownFormats_ParsesCorrectly(string input, int year, int month, int day)
    {
        var result = _handler.Parse(input);

        Assert.Equal(new DateTime(year, month, day), result);
    }

    [Fact]
    public void Handler_Parse_DBNull_ReturnsMinValue()
    {
        Assert.Equal(DateTime.MinValue, _handler.Parse(DBNull.Value));
    }

    [Fact]
    public void Handler_Parse_InvalidString_StillThrows()
    {
        Assert.ThrowsAny<FormatException>(() => _handler.Parse("not-a-date"));
    }
}
