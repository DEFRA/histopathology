namespace Histo.Core.Domain;

/// <summary>
/// Formats block-reference usage into contiguous ranges for the Block Ref search
/// screen (three buckets: Used, Unused, Pre-Booked).
///
/// Legacy source: SearchBlockRefs.aspx.vb — CreateBlockRefsGrid(). Reimplemented
/// as a pure function; PreBookedUsed rows are merged into the Used bucket,
/// matching the legacy grouping rule.
///
/// Status codes mirror <c>Histo.Histology.Models.BlockStatus</c> (Used = 1,
/// PreBooked = 2, PreBookedUsed = 3) — duplicated here rather than referenced,
/// since <c>Histo.Core</c> has no project dependencies.
/// </summary>
public static class BlockRefRangeHelpers
{
    /// <summary>One row of the Block Ref search results grid.</summary>
    public sealed record BlockRefRangeRow(string? UsedBlockRefs, string? UnusedBlockRefs, string? PreBookedBlockRefs);

    private const int NotUsed = 0;
    private const int Used = 1;
    private const int PreBooked = 2;
    private const int PreBookedUsed = 3;

    /// <summary>
    /// Computes the Used/Unused/Pre-Booked range rows for a set of known block
    /// refs and their statuses (as returned by the <c>GetBlocksForHistoRef</c> /
    /// <c>GetBlocksForSenderRef</c> stored procedures).
    /// </summary>
    public static IReadOnlyList<BlockRefRangeRow> ComputeRanges(IReadOnlyList<(int BlockRef, int Status)> usedBlocks)
    {
        if (usedBlocks.Count == 0)
            return [new BlockRefRangeRow(null, "01+", null)];

        var byRef = usedBlocks.ToDictionary(b => b.BlockRef, b => b.Status);
        var maxValue = usedBlocks.Max(b => b.BlockRef);
        var rows = new List<BlockRefRangeRow>();

        var currentStatus = NotUsed;
        var firstInRange = 0;
        var i = 1;

        for (i = 1; i <= maxValue + 1; i++)
        {
            var effectiveStatus = byRef.TryGetValue(i, out var status) ? status : NotUsed;
            var sameBucket = effectiveStatus == currentStatus
                || (IsUsedBucket(currentStatus) && IsUsedBucket(effectiveStatus));

            if (!sameBucket)
            {
                if (!(currentStatus == NotUsed && firstInRange == 0 && i - 1 == 0))
                    rows.Add(BuildRow(currentStatus, firstInRange, i));

                firstInRange = i;
                currentStatus = effectiveStatus;
            }
        }

        // The legacy grid always ends with an open-ended "next unused" row.
        rows.Add(new BlockRefRangeRow(null, $"{FormatRef(i - 1)}+", null));
        return rows;
    }

    private static bool IsUsedBucket(int status) =>
        status == Used || status == PreBookedUsed;

    private static BlockRefRangeRow BuildRow(int status, int first, int to) => status switch
    {
        PreBooked => new BlockRefRangeRow(null, null, FormatRange(first, to)),
        var s when IsUsedBucket(s) => new BlockRefRangeRow(FormatRange(first, to), null, null),
        _ => new BlockRefRangeRow(null, FormatRange(first, to), null),
    };

    private static string FormatRef(int blockRef) => blockRef < 10 ? $"0{blockRef}" : blockRef.ToString();

    private static string FormatRange(int rangeFrom, int rangeTo)
    {
        var last = rangeTo - 1;
        if (rangeFrom == last) return FormatRef(rangeFrom);
        if (rangeFrom == 0) return last == 1 ? FormatRef(last) : $"01 - {FormatRef(last)}";
        return $"{FormatRef(rangeFrom)} - {FormatRef(last)}";
    }
}
