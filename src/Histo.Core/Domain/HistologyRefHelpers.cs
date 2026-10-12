namespace Histo.Core.Domain;

/// <summary>
/// The single source of truth for the <c>yy/NNNNN</c> histology reference format.
///
/// Every automatically drawn reference must carry the CURRENT two-digit year, and only
/// current-year references may be reused from the pre-booked pool — a pooled reference stores its
/// own year prefix from whenever it was booked, so an unfiltered pool read hands back refs from
/// years gone by (e.g. <c>08/40070</c>).
///
/// Legacy source: <c>clsHistology.vb::GetNextAvailableHistologyRef</c> — <c>Right$(Year, 2) + "/" + ref</c>.
/// </summary>
public static class HistologyRefHelpers
{
    /// <summary>The current two-digit year followed by a slash, e.g. "26/".</summary>
    public static string CurrentYearPrefix => $"{DateTime.Now.Year % 100:D2}/";

    /// <summary>Formats a drawn counter number as a current-year histology reference.</summary>
    public static string ForCurrentYear(string refNumber) => CurrentYearPrefix + refNumber.Trim();

    /// <summary>True when the reference carries the current two-digit year prefix.</summary>
    public static bool IsCurrentYear(string? histologyRef) =>
        histologyRef?.Trim().StartsWith(CurrentYearPrefix, StringComparison.Ordinal) == true;

    /// <summary>
    /// The trailing 5-digit number of a reference, used to order pooled refs lowest-first.
    /// Returns <see cref="int.MaxValue"/> for anything unparseable so it sorts last rather than
    /// being picked by mistake.
    /// </summary>
    public static int RefNumber(string? histologyRef)
    {
        var trimmed = histologyRef?.Trim();
        if (trimmed is null || trimmed.Length < 5) return int.MaxValue;
        return int.TryParse(trimmed[^5..], out var number) ? number : int.MaxValue;
    }
}
