using System.Text.RegularExpressions;

namespace Histo.Core.Domain;

/// <summary>
/// Input validation helpers ported from the legacy <c>Common.vb</c> module.
/// All methods are pure functions with no side effects.
/// </summary>
public static partial class ValidationHelpers
{
    // Compiled regex mirrors for legacy VB Regex objects in Common.vb

    [GeneratedRegex(@"MC[0-9]{6}")]
    private static partial Regex MouseNumberRegex();

    [GeneratedRegex(@"[0-9]{2}/[0-9]{5}")]
    private static partial Regex HistoRefRegex();

    [GeneratedRegex(@"HP[0-9]{4}/[0-9]{2}")]
    private static partial Regex HpRefRegex();

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="value"/> contains a valid
    /// mouse number in the format <c>MC######</c> (6 decimal digits, case-insensitive).
    ///
    /// Legacy source: Common.vb, <c>ValidateMouseNumber()</c>.
    ///
    /// Note: the underlying regex is not anchored, so the pattern need only appear
    /// within the value string — this faithfully mirrors the legacy behaviour.
    /// </summary>
    public static bool ValidateMouseNumber(string value)
    {
        var upper = value.ToUpperInvariant();
        return MouseNumberRegex().IsMatch(upper);
    }

    /// <summary>
    /// Zero-pads a block reference integer to a 2-character string.
    /// Values less than 10 are prefixed with "0"; values ≥ 10 are returned as-is.
    ///
    /// Legacy source: Common.vb, <c>ConvertBlockRefToString()</c>.
    /// </summary>
    public static string ConvertBlockRefToString(int blockRef)
        => blockRef < 10 ? $"0{blockRef}" : blockRef.ToString();

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="histologyRef"/> is a
    /// valid histology reference string.
    ///
    /// Legacy source: Common.vb, <c>ValidateHistoRef()</c>.
    ///
    /// Validation rules (applied in order):
    /// <list type="number">
    ///   <item>If <paramref name="isHistologyUser"/> is <see langword="true"/> and the
    ///     reference matches HP format (<c>HP####/##</c>), return invalid.</item>
    ///   <item>If the reference contains a hyphen, return invalid.</item>
    ///   <item>The reference must be exactly 8 characters.</item>
    ///   <item>The reference must match the pattern <c>YY/NNNNN</c>.</item>
    ///   <item>If the 2-digit year is greater than the current year AND less than 70,
    ///     return invalid (future-year rejection).</item>
    /// </list>
    /// </summary>
    public static bool ValidateHistoRef(string histologyRef, bool isHistologyUser)
    {
        if (isHistologyUser && HpRefRegex().IsMatch(histologyRef))
            return false;

        if (histologyRef.Contains('-'))
            return false;

        if (histologyRef.Length != 8)
            return false;

        if (!HistoRefRegex().IsMatch(histologyRef))
            return false;

        var histoYear = int.Parse(histologyRef[..2]);
        var currentYear = int.Parse(DateTime.Now.Year.ToString()[^2..]);

        // Reject future years, but not years ≥ 70 (those are treated as 1970s — valid historical refs)
        if (histoYear > currentYear && histoYear < 70)
            return false;

        return true;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the batch's submitted-as code indicates
    /// pre-cassetted submission (code <c>"5"</c>).
    ///
    /// Legacy source: Common.vb, <c>IsBatchPreCassetted()</c>.
    ///
    /// The DataSet table lookup (table index 5, BatchID filter) is the responsibility
    /// of the repository layer. This method accepts the already-resolved code string.
    /// </summary>
    public static bool IsBatchPreCassetted(string? submittedAsCode)
        => submittedAsCode == "5";

    /// <summary>
    /// Returns <see langword="true"/> when the resolved "Submitted As" lookup description
    /// is "Wet Tissue" — the Block Types vs. Tissue Type split that decides whether Add sample
    /// routes to Sample Blocks (SubmissionDetailsBlock) or Sample Details (SubmissionDetails).
    ///
    /// Legacy source: Cassetted.aspx.vb, <c>btnYes_Click</c> —
    /// <c>chkblSubmittedAs.SelectedItem.Text.ToString() = "Wet Tissue"</c>. Unlike
    /// <see cref="IsBatchPreCassetted"/>, legacy never hardcodes a hardcoded numeric code for
    /// Wet Tissue — it compares the lookup item's *Description*, not its Code, so callers must
    /// resolve the code to a description via LOOKUP_SUBMITTEDAS (table 11) first.
    /// </summary>
    public static bool IsWetTissueDescription(string? submittedAsDescription)
        => string.Equals(submittedAsDescription?.Trim(), "Wet Tissue", StringComparison.OrdinalIgnoreCase);
}
