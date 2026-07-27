using Histo.Core.Domain;

namespace Histo.Tests.Unit;

/// <summary>
/// Baseline unit tests for <see cref="AnimalHelpers"/>.
///
/// These tests document the PG-number auto-reversal business rule observed in
/// HistopathologyLib/clsAnimal.vb NewRecord() and the IsAfter01() helper.
///
/// All test cases mirror realistic inputs from the legacy application and are
/// named to serve as living documentation of the expected behaviour.
/// </summary>
public class AnimalHelperTests
{
    // -----------------------------------------------------------------------
    // ComputePgAutoHistologyRef — reversal applied
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("PG012302", "02/00123")]  // uppercase PG
    [InlineData("pg012302", "02/00123")]  // lowercase pg
    [InlineData("Pg012302", "02/00123")]  // mixed case Pg
    [InlineData("pG012302", "02/00123")]  // mixed case pG
    public void ComputePgAutoHistologyRef_NeuropathWithPgPrefix_ReturnsReversedRef(
        string senderRef, string expectedHistologyRef)
    {
        var result = AnimalHelpers.ComputePgAutoHistologyRef(senderRef, isNeuropath: true);
        Assert.Equal(expectedHistologyRef, result);
    }

    [Fact]
    public void ComputePgAutoHistologyRef_YearIs01_ReturnsReversedRef()
    {
        // Year "01" → IsAfterYear01 returns true (year >= 1 AND year < 70)
        // The function name is slightly misleading: "01" IS considered after "01".
        var result = AnimalHelpers.ComputePgAutoHistologyRef("PG012301", isNeuropath: true);
        Assert.Equal("01/00123", result);
    }

    [Fact]
    public void ComputePgAutoHistologyRef_YearIs69_ReturnsReversedRef()
    {
        // Year "69" is the last value that satisfies IsAfterYear01 (< 70)
        var result = AnimalHelpers.ComputePgAutoHistologyRef("PG012369", isNeuropath: true);
        Assert.Equal("69/00123", result);
    }

    // -----------------------------------------------------------------------
    // ComputePgAutoHistologyRef — reversal inhibited
    // -----------------------------------------------------------------------

    [Fact]
    public void ComputePgAutoHistologyRef_NotNeuropath_ReturnsNull()
    {
        // When the user area is not neuropath, no reversal is applied regardless of ref format
        var result = AnimalHelpers.ComputePgAutoHistologyRef("PG012302", isNeuropath: false);
        Assert.Null(result);
    }

    [Fact]
    public void ComputePgAutoHistologyRef_NonPgPrefix_ReturnsNull()
    {
        var result = AnimalHelpers.ComputePgAutoHistologyRef("AB012302", isNeuropath: true);
        Assert.Null(result);
    }

    [Fact]
    public void ComputePgAutoHistologyRef_TooShort_ReturnsNull()
    {
        // senderRef.Length <= 2 — guard in NewRecord() fires first
        var result = AnimalHelpers.ComputePgAutoHistologyRef("PG", isNeuropath: true);
        Assert.Null(result);
    }

    [Fact]
    public void ComputePgAutoHistologyRef_YearIs00_ReturnsNull()
    {
        // Year "00" → IsAfterYear01 returns false (year = 0, 0 >= 1 is false)
        var result = AnimalHelpers.ComputePgAutoHistologyRef("PG012300", isNeuropath: true);
        Assert.Null(result);
    }

    [Fact]
    public void ComputePgAutoHistologyRef_YearIs70_ReturnsNull()
    {
        // Year "70" → IsAfterYear01 returns false (70 < 70 is false)
        var result = AnimalHelpers.ComputePgAutoHistologyRef("PG012370", isNeuropath: true);
        Assert.Null(result);
    }

    [Fact]
    public void ComputePgAutoHistologyRef_YearAbove70_ReturnsNull()
    {
        // Years 71-99 also return false from IsAfterYear01
        var result = AnimalHelpers.ComputePgAutoHistologyRef("PG012399", isNeuropath: true);
        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // IsAfterYear01 — boundary conditions
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("01", true)]   // minimum valid year
    [InlineData("02", true)]
    [InlineData("25", true)]
    [InlineData("69", true)]   // maximum valid year
    [InlineData("00", false)]  // zero — excluded
    [InlineData("70", false)]  // 1970s boundary — excluded
    [InlineData("99", false)]  // 1999 — excluded
    public void IsAfterYear01_ReturnsExpectedResult(string yearPart, bool expected)
    {
        Assert.Equal(expected, AnimalHelpers.IsAfterYear01(yearPart));
    }

    [Fact]
    public void IsAfterYear01_NonNumericString_ReturnsFalse()
    {
        Assert.False(AnimalHelpers.IsAfterYear01("AB"));
    }
}
