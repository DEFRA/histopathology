using Histo.Reporting.Services;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for the <c>internal static FormatLongDate</c> helper extracted
/// from <see cref="QCNoteDataSetBuilder"/>.
///
/// <c>FormatLongDate</c> converts a raw date string to "dd MMMM yyyy" (UK long
/// date), matching the legacy Crystal Reports "Long Date" format used in
/// QCNote.rpt.  The method returns the raw string unchanged when it cannot be
/// parsed as a date — this mirrors the legacy fallback behaviour.
/// </summary>
public class QCNoteDataSetBuilderTests
{
    [Fact]
    public void FormatLongDate_StandardIsoDate_ReturnsLongFormat()
    {
        var result = QCNoteDataSetBuilder.FormatLongDate("2017-01-11");

        Assert.Equal("11 January 2017", result);
    }

    [Fact]
    public void FormatLongDate_DateTimeWithTime_ReturnsDateOnly()
    {
        // Datetime values from SQL include a time component — only the date matters
        var result = QCNoteDataSetBuilder.FormatLongDate("2017-01-11T14:30:00");

        Assert.Equal("11 January 2017", result);
    }

    [Fact]
    public void FormatLongDate_InvalidString_ReturnsRawValue()
    {
        // Unparseable values are passed through unchanged to avoid data loss
        var result = QCNoteDataSetBuilder.FormatLongDate("not a date");

        Assert.Equal("not a date", result);
    }

    [Fact]
    public void FormatLongDate_EmptyString_ReturnsEmptyString()
    {
        var result = QCNoteDataSetBuilder.FormatLongDate(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatLongDate_DifferentMonth_ReturnsCorrectMonthName()
    {
        var result = QCNoteDataSetBuilder.FormatLongDate("2024-03-15");

        Assert.Equal("15 March 2024", result);
    }

    [Fact]
    public void FormatLongDate_LastDayOfYear_ReturnsCorrectDate()
    {
        var result = QCNoteDataSetBuilder.FormatLongDate("2023-12-31");

        Assert.Equal("31 December 2023", result);
    }
}
