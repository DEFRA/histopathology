using System.Globalization;

namespace Histo.Core.Domain;

/// <summary>
/// Converts between the app's legacy date string format and HTML5 date-input format.
///
/// Legacy source: CalendarDate.ascx.vb::FormattedDate — always stores/displays
/// dd/MM/yyyy. Native &lt;input type="date"&gt; requires yyyy-MM-dd for its value
/// attribute and always posts back in that format.
/// </summary>
public static class DateFormatHelpers
{
    private static readonly string[] AcceptedFormats = ["dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy"];

    /// <summary>Converts a legacy dd/MM/yyyy date string to yyyy-MM-dd for a date input. Returns null if empty/unparseable.</summary>
    public static string? ToIsoDate(string? legacyDate)
    {
        if (string.IsNullOrWhiteSpace(legacyDate)) return null;
        return TryParseFlexible(legacyDate, out var parsed) ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
    }

    /// <summary>Converts a date input's yyyy-MM-dd value back to the legacy dd/MM/yyyy format. Returns the raw input if unparseable rather than discarding it.</summary>
    public static string? ToLegacyDate(string? isoDate)
    {
        if (string.IsNullOrWhiteSpace(isoDate)) return null;
        return TryParseFlexible(isoDate, out var parsed) ? parsed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : isoDate;
    }

    private static bool TryParseFlexible(string value, out DateTime result) =>
        DateTime.TryParseExact(value, AcceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result) ||
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
}
