using Histo.Web;

namespace Histo.Tests.Unit;

public class DatePartsTests
{
    [Fact]
    public void TryGetDate_ValidParts_ReturnsDate()
    {
        var parts = new DateParts { Day = "5", Month = "3", Year = "2024" };

        Assert.True(parts.TryGetDate(out var value));
        Assert.Equal(new DateTime(2024, 3, 5), value);
    }

    [Fact]
    public void TryGetDate_AllEmpty_ReturnsTrueWithNull()
    {
        Assert.True(new DateParts().TryGetDate(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void TryGetDate_TwoDigitYear_IsRejected()
    {
        var parts = new DateParts { Day = "5", Month = "3", Year = "24" };

        Assert.False(parts.TryGetDate(out var value));
        Assert.Null(value);
    }

    [Theory]
    [InlineData("31", "2", "2024")]
    [InlineData("0", "2", "2024")]
    [InlineData("1", "13", "2024")]
    [InlineData("1", "0", "2024")]
    [InlineData("x", "1", "2024")]
    [InlineData("1", "1", null)]
    public void TryGetDate_NotACalendarDate_IsRejected(string day, string month, string? year)
    {
        var parts = new DateParts { Day = day, Month = month, Year = year };

        Assert.False(parts.TryGetDate(out _));
    }

    [Fact]
    public void FromDate_RoundTripsThroughTryGetDate()
    {
        var parts = DateParts.FromDate(new DateTime(2023, 11, 9));

        Assert.True(parts.TryGetDate(out var value));
        Assert.Equal(new DateTime(2023, 11, 9), value);
    }
}
