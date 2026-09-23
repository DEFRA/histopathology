using Histo.Core.Domain;

namespace Histo.Tests.Unit;

/// <summary>
/// Baseline unit tests for <see cref="ValidationHelpers"/>.
///
/// Covers: <c>ValidateMouseNumber</c>, <c>ConvertBlockRefToString</c>,
/// <c>ValidateHistoRef</c>, and <c>IsBatchPreCassetted</c>.
///
/// Legacy source: HistopathologySystem/Common.vb.
/// </summary>
public class ValidationHelperTests
{
    // =========================================================================
    // ValidateMouseNumber
    // =========================================================================

    [Theory]
    [InlineData("MC123456", true)]    // exact valid format
    [InlineData("mc123456", true)]    // lowercase — function calls ToUpper first
    [InlineData("MC123456extra", true)] // pattern found within longer string (regex not anchored)
    public void ValidateMouseNumber_ValidInputs_ReturnTrue(string value, bool expected)
        => Assert.Equal(expected, ValidationHelpers.ValidateMouseNumber(value));

    [Theory]
    [InlineData("MC12345")]    // only 5 digits
    [InlineData("AB123456")]   // wrong prefix
    [InlineData("MC1234AB")]   // non-numeric digits
    [InlineData("")]           // empty
    [InlineData("MC")]         // prefix only
    public void ValidateMouseNumber_InvalidInputs_ReturnFalse(string value)
        => Assert.False(ValidationHelpers.ValidateMouseNumber(value));

    // =========================================================================
    // ConvertBlockRefToString
    // =========================================================================

    [Theory]
    [InlineData(0, "00")]
    [InlineData(1, "01")]
    [InlineData(9, "09")]
    [InlineData(10, "10")]
    [InlineData(11, "11")]
    [InlineData(99, "99")]
    [InlineData(100, "100")]  // no cap — mirrors legacy ToString() behaviour
    public void ConvertBlockRefToString_ReturnsExpectedString(int input, string expected)
        => Assert.Equal(expected, ValidationHelpers.ConvertBlockRefToString(input));

    // =========================================================================
    // IsBatchPreCassetted
    // =========================================================================

    [Fact]
    public void IsBatchPreCassetted_CodeFive_ReturnsTrue()
        => Assert.True(ValidationHelpers.IsBatchPreCassetted("5"));

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("6")]
    [InlineData("")]
    [InlineData(null)]
    public void IsBatchPreCassetted_NonFiveCode_ReturnsFalse(string? code)
        => Assert.False(ValidationHelpers.IsBatchPreCassetted(code));

    // =========================================================================
    // IsWetTissueDescription
    // =========================================================================

    [Theory]
    [InlineData("Wet Tissue", true)]
    [InlineData("wet tissue", true)]  // case-insensitive
    [InlineData(" Wet Tissue ", true)] // trimmed
    [InlineData("Wax Block", false)]
    [InlineData("Pre Cassetted Tissue", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsWetTissueDescription_ComparesDescriptionNotCode(string? description, bool expected)
        => Assert.Equal(expected, ValidationHelpers.IsWetTissueDescription(description));

    // =========================================================================
    // ValidateHistoRef — format validation
    // =========================================================================

    [Theory]
    [InlineData("05/12345", false)]  // valid historical year, non-histology user
    [InlineData("05/12345", true)]   // valid historical year, histology user (not HP format)
    [InlineData("10/12345", false)]  // another valid historical year
    [InlineData("00/12345", false)]  // year "00" — 0 > currentYear? No → valid
    [InlineData("99/12345", false)]  // year "99" — 99 > currentYear but 99 >= 70 → valid
    public void ValidateHistoRef_ValidRef_ReturnsTrue(string histoRef, bool isHistologyUser)
        => Assert.True(ValidationHelpers.ValidateHistoRef(histoRef, isHistologyUser));

    [Fact]
    public void ValidateHistoRef_FutureYear_ReturnsFalse()
    {
        // Build a future year that is < 70 (to trigger the future-year rejection rule)
        // Using next year relative to today, capped to avoid the >= 70 exception
        var nextYear = DateTime.Now.Year + 1;
        var nextYear2Digit = nextYear.ToString()[^2..];

        // Skip this assertion if the 2-digit year would be >= 70 (the test is unstable after 2069)
        if (int.Parse(nextYear2Digit) >= 70) return;

        var futureRef = nextYear2Digit + "/12345";
        Assert.False(ValidationHelpers.ValidateHistoRef(futureRef, isHistologyUser: false));
    }

    [Theory]
    [InlineData("05/1234")]     // 7 chars — too short
    [InlineData("05/123456")]   // 9 chars — too long
    [InlineData("05-12345")]    // contains hyphen
    [InlineData("XX/12345")]    // non-numeric year — regex fails
    [InlineData("05/1234A")]    // non-numeric in digit section
    public void ValidateHistoRef_InvalidFormat_ReturnsFalse(string histoRef)
        => Assert.False(ValidationHelpers.ValidateHistoRef(histoRef, isHistologyUser: false));

    [Fact]
    public void ValidateHistoRef_HpFormatForHistologyUser_ReturnsFalse()
    {
        // HP0123/24 (10 chars) — matches HP regex and is explicitly rejected for histology users
        Assert.False(ValidationHelpers.ValidateHistoRef("HP0123/24", isHistologyUser: true));
    }

    [Fact]
    public void ValidateHistoRef_HpFormatForNonHistologyUser_ReturnsFalse()
    {
        // HP0123/24 is 10 chars — even without the HP check, it fails the length = 8 rule
        Assert.False(ValidationHelpers.ValidateHistoRef("HP0123/24", isHistologyUser: false));
    }
}
