namespace Histo.Web;

/// <summary>
/// Value backing the GOV.UK three-part date input (day / month / year).
///
/// The Design System advises against the native <c>&lt;input type="date"&gt;</c> picker:
/// rendering, locale and screen-reader behaviour vary between browsers. The three-field
/// component is the standard pattern, so search filters bind to this instead of DateTime.
///
/// Parts are held as strings so that invalid user input (e.g. "31" in February) survives
/// the round-trip and can be redisplayed alongside the error, rather than being silently
/// discarded by model binding.
/// </summary>
public sealed class DateParts
{
    public string? Day { get; set; }
    public string? Month { get; set; }
    public string? Year { get; set; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Day) && string.IsNullOrWhiteSpace(Month) && string.IsNullOrWhiteSpace(Year);

    public static DateParts FromDate(DateTime? value) => value is null
        ? new DateParts()
        : new DateParts
        {
            Day = value.Value.Day.ToString(),
            Month = value.Value.Month.ToString(),
            Year = value.Value.Year.ToString(),
        };

    /// <summary>
    /// Returns <see langword="false"/> when the parts are partially filled or do not form a
    /// real calendar date. An entirely empty input is valid and yields <see langword="null"/>,
    /// letting optional filters be omitted.
    /// </summary>
    public bool TryGetDate(out DateTime? value)
    {
        value = null;
        if (IsEmpty) return true;

        var year = Year?.Trim();
        if (!int.TryParse(Day?.Trim(), out var d)
            || !int.TryParse(Month?.Trim(), out var m)
            || !int.TryParse(year, out var y))
            return false;

        // GDS specifies a four-digit year field; rejecting shorter input avoids "26" being read as year 26.
        if (year!.Length != 4) return false;
        if (m is < 1 or > 12 || d < 1 || d > DateTime.DaysInMonth(y, m)) return false;

        value = new DateTime(y, m, d);
        return true;
    }
}

/// <summary>View model for the <c>_DateInput</c> partial.</summary>
public sealed class DateInputViewModel
{
    /// <summary>Binding prefix, e.g. "StartDate" — renders fields named "StartDate.Day" etc.</summary>
    public required string Name { get; init; }
    public required string Legend { get; init; }
    public string? Hint { get; init; }
    public string? Error { get; init; }
    public DateParts Value { get; init; } = new();

    /// <summary>Id of the day field — the anchor an error summary link should target.</summary>
    public string DayFieldId => $"{Name}-day";
}
