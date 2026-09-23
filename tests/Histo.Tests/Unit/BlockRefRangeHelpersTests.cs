using Histo.Core.Domain;

namespace Histo.Tests.Unit;

/// <summary>
/// Verifies <see cref="BlockRefRangeHelpers.ComputeRanges"/> against the real legacy
/// <c>SearchBlockRefs.aspx.vb::FormatString</c> quoting rule: a single ref (or an
/// implied-single-ref 0-based range) is unquoted; only a genuine multi-ref range is
/// wrapped in single quotes.
/// </summary>
public class BlockRefRangeHelpersTests
{
    [Fact]
    public void NoBlocks_ReturnsOpenEndedUnusedFromOne()
    {
        var rows = BlockRefRangeHelpers.ComputeRanges([]);

        var row = Assert.Single(rows);
        Assert.Equal("01+", row.UnusedBlockRefs);
        Assert.Null(row.UsedBlockRefs);
        Assert.Null(row.PreBookedBlockRefs);
    }

    [Fact]
    public void SingleUsedRef_IsUnquoted()
    {
        var rows = BlockRefRangeHelpers.ComputeRanges([(5, 1)]);

        Assert.Contains(rows, r => r.UsedBlockRefs == "05");
    }

    [Fact]
    public void MultiRefRangeNotStartingAtOne_IsQuoted()
    {
        var rows = BlockRefRangeHelpers.ComputeRanges([(5, 2), (6, 2), (7, 2)]);

        Assert.Contains(rows, r => r.PreBookedBlockRefs == "'05 - 07'");
    }

    [Fact]
    public void MultiRefRangeStartingAtOne_IsQuotedWithLeadingZeroOne()
    {
        var rows = BlockRefRangeHelpers.ComputeRanges([(1, 2), (2, 2)]);

        Assert.Contains(rows, r => r.PreBookedBlockRefs == "'01 - 02'");
    }

    [Fact]
    public void SingleRefAtPositionOne_IsUnquoted()
    {
        var rows = BlockRefRangeHelpers.ComputeRanges([(1, 2)]);

        Assert.Contains(rows, r => r.PreBookedBlockRefs == "01");
    }

    [Fact]
    public void TrailingOpenEndedUnusedRow_IsAlwaysUnquoted()
    {
        var rows = BlockRefRangeHelpers.ComputeRanges([(1, 1)]);

        Assert.Contains(rows, r => r.UnusedBlockRefs == "02+");
    }
}
