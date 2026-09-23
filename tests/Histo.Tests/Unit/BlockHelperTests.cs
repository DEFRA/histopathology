using Histo.Core.Domain;

namespace Histo.Tests.Unit;

/// <summary>
/// Baseline unit tests for <see cref="BlockHelpers"/>.
///
/// Covers <c>ComputeNextOrder</c> — the pure-function equivalent of the private
/// <c>GetOrder(DataTable)</c> method in both <c>clsBlock.vb</c> and
/// <c>clsBatchSubmission.vb</c>.
///
/// Legacy source: HistopathologyLib/clsBlock.vb, private <c>GetOrder()</c>.
/// </summary>
public class BlockHelperTests
{
    [Fact]
    public void ComputeNextOrder_EmptyCollection_ReturnsZero()
    {
        // Mirrors: DataTable.Compute("Max(Order)") returning DBNull on empty table → return 0
        var result = BlockHelpers.ComputeNextOrder([]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ComputeNextOrder_SingleElement_ReturnsMaxPlusOne()
    {
        var result = BlockHelpers.ComputeNextOrder([0]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void ComputeNextOrder_MultipleElements_ReturnsMaxPlusOne()
    {
        var result = BlockHelpers.ComputeNextOrder([0, 1, 2, 3]);
        Assert.Equal(4, result);
    }

    [Fact]
    public void ComputeNextOrder_NonSequentialOrders_ReturnsMaxPlusOne()
    {
        // Orders can be non-sequential if rows have been deleted
        var result = BlockHelpers.ComputeNextOrder([0, 5, 3]);
        Assert.Equal(6, result);
    }

    [Fact]
    public void ComputeNextOrder_OrdersContainingZeroOnly_ReturnsOne()
    {
        var result = BlockHelpers.ComputeNextOrder([0]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void ComputeNextOrder_LargeOrderValues_ReturnsCorrectNext()
    {
        var result = BlockHelpers.ComputeNextOrder([10, 20, 15]);
        Assert.Equal(21, result);
    }
}
