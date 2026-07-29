namespace Histo.Core.Domain;

/// <summary>
/// Pure helpers for block and submission ordering.
/// Extracted from <c>clsBlock.vb</c> and <c>clsBatchSubmission.vb</c>.
/// </summary>
public static class BlockHelpers
{
    /// <summary>
    /// Returns the next order value for a new block or batch submission row.
    ///
    /// Legacy source: HistopathologyLib/clsBlock.vb, private <c>GetOrder()</c>;
    /// HistopathologyLib/clsBatchSubmission.vb, <c>GetOrder()</c> — identical pattern.
    ///
    /// Rule: <c>Max(existingOrders) + 1</c>. Returns <c>0</c> when the collection
    /// is empty (mirrors the behaviour of <c>DataTable.Compute("Max(Order)")</c>
    /// returning <see cref="DBNull"/> on an empty table).
    /// </summary>
    public static int ComputeNextOrder(IEnumerable<int> existingOrders)
    {
        var list = existingOrders.ToList();
        return list.Count == 0 ? 0 : list.Max() + 1;
    }
}
