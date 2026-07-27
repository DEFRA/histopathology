namespace Histo.Core.Domain;

/// <summary>
/// Pure domain helpers for animal record creation.
/// Extracted from legacy <c>clsAnimal.vb</c>, <c>NewRecord()</c> methods.
/// </summary>
public static class AnimalHelpers
{
    /// <summary>
    /// Computes the auto-reversed histology reference for neuropath submissions
    /// where the sender reference is in PG-number format (e.g. "PG012302").
    ///
    /// Legacy source: HistopathologyLib/clsAnimal.vb, NewRecord() —
    /// "Auto reverse if the sender ref is PG Number and user area is neuropath" block.
    ///
    /// Reversal rules:
    ///   - Only applied when <paramref name="isNeuropath"/> is true.
    ///   - <paramref name="senderRef"/> must be longer than 2 characters.
    ///   - First 2 characters of <paramref name="senderRef"/> must be "PG" (case-insensitive).
    ///   - The remaining characters are split: first 4 = ID, last 2 = year.
    ///   - Reversal is applied only when the year satisfies <see cref="IsAfterYear01"/>
    ///     (i.e. year is between 1 and 69 inclusive).
    ///   - Output format: <c>"{year}/0{id}"</c>.
    ///
    /// Returns <see langword="null"/> when no reversal applies.
    /// </summary>
    public static string? ComputePgAutoHistologyRef(string senderRef, bool isNeuropath)
    {
        if (!isNeuropath) return null;
        if (senderRef.Length <= 2) return null;

        var prefix = senderRef[..2];
        if (!prefix.Equals("PG", StringComparison.OrdinalIgnoreCase)) return null;

        var remaining = senderRef[2..];
        if (remaining.Length < 2) return null;

        // Mirror VB Left$(str, 4) — safe truncation when str is shorter than 4
        var strId = remaining.Length >= 4 ? remaining[..4] : remaining;
        // Mirror VB Right$(str, 2)
        var strYear = remaining[^2..];

        if (!IsAfterYear01(strYear)) return null;

        return strYear + "/0" + strId;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the two-digit year string represents
    /// a year between 1 and 69 (inclusive).
    ///
    /// Legacy source: HistopathologyLib/clsAnimal.vb, <c>IsAfter01()</c> function (line ~1029).
    ///
    /// Note: despite the name, year "01" (integer value 1) satisfies the condition
    /// and returns <see langword="true"/>. Year "00" and years ≥ 70 return
    /// <see langword="false"/>. This matches the original VB implementation exactly.
    /// </summary>
    public static bool IsAfterYear01(string yearPart)
    {
        if (!int.TryParse(yearPart, out var year)) return false;
        return year >= 1 && year < 70;
    }
}
