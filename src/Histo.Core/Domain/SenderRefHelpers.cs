namespace Histo.Core.Domain;

/// <summary>
/// Pure domain helpers for Sender Ref / Block Ref parsing and formatting, used by the
/// Bookings module's range-booking screens.
///
/// Legacy source: HistopathologySystem/SenderRef.ascx.vb (<c>IsPGNumber</c>/<c>IsMouseNumber</c>),
/// BookBlockRef.aspx.vb (<c>PadPGNumberZeroes</c>/<c>PadMouseNumberZeroes</c>), and Common.vb
/// (<c>ConvertBlockRefToString</c>).
/// </summary>
public static class SenderRefHelpers
{
    /// <summary>True when the sender ref starts with "PG" (case-insensitive), e.g. "PG0123/24".</summary>
    public static bool IsPgNumber(string senderRef) =>
        senderRef.Length > 2 && senderRef[..2].Equals("PG", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the sender ref starts with "MC" (case-insensitive), e.g. "MC000123".</summary>
    public static bool IsMouseNumber(string senderRef) =>
        senderRef.Length > 2 && senderRef[..2].Equals("MC", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Parses a PG number ("PGNNNN/YY") into its numeric ID and 2-digit year.
    /// Returns <see langword="false"/> when the format doesn't match.
    /// </summary>
    public static bool TryParsePgNumber(string senderRef, out int id, out string year)
    {
        id = 0;
        year = string.Empty;
        if (!IsPgNumber(senderRef)) return false;

        var remainder = senderRef[2..];
        var slash = remainder.IndexOf('/');
        if (slash < 0) return false;

        var idPart = remainder[..slash];
        year = remainder[(slash + 1)..];
        return int.TryParse(idPart, out id);
    }

    /// <summary>Parses a Mouse number ("MCNNNNNN") into its numeric ID.</summary>
    public static bool TryParseMouseNumber(string senderRef, out int id)
    {
        id = 0;
        if (!IsMouseNumber(senderRef)) return false;
        return int.TryParse(senderRef[2..], out id);
    }

    /// <summary>Formats a PG number, zero-padding the ID to at least 4 digits. Legacy: PadPGNumberZeroes.</summary>
    public static string FormatPgNumber(int id, string year) => $"PG{id:D4}/{year}";

    /// <summary>Formats a Mouse number, zero-padding the ID to at least 6 digits. Legacy: PadMouseNumberZeroes.</summary>
    public static string FormatMouseNumber(int id) => $"MC{id:D6}";

    /// <summary>Formats a numeric block ref, zero-padding single digits. Legacy: ConvertBlockRefToString.</summary>
    public static string FormatBlockRef(int blockRef) => blockRef < 10 ? $"0{blockRef}" : blockRef.ToString();

    /// <summary>
    /// Validates a block ref string against legacy's <c>ClientValidateBlockRef</c>/<c>ValidateBlockRefRef</c>
    /// rules: rejects "00"/"000", requires two digits for refs of length ≤ 2, and a leading
    /// non-zero digit for 3-digit refs.
    /// </summary>
    public static bool IsValidBlockRef(string blockRef)
    {
        if (blockRef is "00" or "000") return false;
        return blockRef.Length <= 2
            ? System.Text.RegularExpressions.Regex.IsMatch(blockRef, "^[0-9][0-9]$")
            : System.Text.RegularExpressions.Regex.IsMatch(blockRef, "^[1-9][0-9][0-9]$");
    }
}
