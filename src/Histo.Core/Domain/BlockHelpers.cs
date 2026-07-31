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

    /// <summary>
    /// Returns the next free two-digit block reference for an animal, given the
    /// block refs already assigned to that animal.
    ///
    /// Legacy source: HistopathologyLib/clsBlock.vb, <c>CopyBlock()</c> —
    /// "iBlockRef += 1 ... If iBlockRef &lt; 10 Then sNextBlockRef = "0" &amp; ..." block.
    /// Non-numeric refs are ignored (treated as 0) rather than throwing.
    /// </summary>
    public static string ComputeNextBlockRef(IEnumerable<string> existingBlockRefs)
    {
        var max = existingBlockRefs
            .Select(r => int.TryParse(r, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        var next = max + 1;
        return next < 10 ? $"0{next}" : next.ToString();
    }
}
