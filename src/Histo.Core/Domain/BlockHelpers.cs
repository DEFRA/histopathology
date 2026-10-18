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
    ///
    /// <paramref name="storedNextBlockRef"/> is the animal's persisted <c>NextBlockRef</c> — a
    /// high-water mark that only ever moves forward, so a ref freed by deleting a block is not
    /// handed out again. The result is never lower than it (legacy: <c>Animal.NextBlockRef</c>).
    ///
    /// Always returns the lowest free number at or after the starting point (either
    /// <paramref name="afterBlockRef"/> + 1, or <paramref name="storedNextBlockRef"/> when no
    /// <paramref name="afterBlockRef"/> is given) — gaps below an already-used higher ref (e.g.
    /// pre-booked refs 03/04 with 01/02 still free) are filled first, never skipped over.
    /// </summary>
    public static string ComputeNextBlockRef(IEnumerable<string> existingBlockRefs, string? storedNextBlockRef = null, string? afterBlockRef = null)
    {
        var used = existingBlockRefs
            .Select(r => int.TryParse(r, out var n) ? n : 0)
            .ToHashSet();
        var stored = int.TryParse(storedNextBlockRef, out var s) ? s : 0;

        var candidate = int.TryParse(afterBlockRef, out var after)
            ? Math.Max(after + 1, stored)
            : Math.Max(1, stored);
        while (used.Contains(candidate)) candidate++;
        return FormatBlockRef(candidate);
    }

    /// <summary>
    /// The block ref that follows <paramref name="blockRef"/> — what an animal's persisted
    /// <c>NextBlockRef</c> should be advanced to once that ref has been issued and removed.
    /// Returns <see langword="null"/> for a non-numeric ref.
    /// </summary>
    public static string? NextRefAfter(string? blockRef) =>
        int.TryParse(blockRef, out var n) ? FormatBlockRef(n + 1) : null;

    private static string FormatBlockRef(int number) => number < 10 ? $"0{number}" : number.ToString();
}
