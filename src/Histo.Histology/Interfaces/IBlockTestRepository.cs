using Histo.Histology.Models;

namespace Histo.Histology.Interfaces;

/// <summary>
/// Data access contract for block-level test records (histology, antibodies and
/// special stain tests) — the quality-control and dispatch worklist.
///
/// Legacy source: HistopathologyLib/clsBatchSummary.vb (read) and
/// HistopathologyLib/clsCheckBoxData.vb — <c>UpdateBlockTablesDetails</c> (write).
/// See <see cref="Histo.Histology.Models.BlockTest"/> for scope notes.
/// </summary>
public interface IBlockTestRepository
{
    /// <summary>
    /// Returns every histology, antibodies and special-stain test for a batch's blocks.
    /// Maps to <c>GetTestsByBatchID</c>.
    /// </summary>
    Task<IReadOnlyList<BlockTest>> GetByBatchAsync(int batchId, CancellationToken ct = default);

    /// <summary>Updates a single test record's result, QC, dispatch and archive fields.
    /// Maps to <c>EditBlockHistology</c>, <c>EditBlockAntibodies</c> or
    /// <c>EditBlockStain</c>, selected by <see cref="BlockTest.TestType"/>.
    /// Throws <see cref="BlockTestConcurrencyException"/> on rowstamp mismatch.
    /// </summary>
    Task UpdateAsync(BlockTest test, int userId, CancellationToken ct = default);

    /// <summary>
    /// Delta-saves premium-charge (TC code) selections for a single test.
    /// Deletes removed codes via Delete*TCCode SPs; inserts added codes via Add*TCCode SPs.
    /// </summary>
    Task SaveTCCodesAsync(int batchId, int testId, string testType,
        IReadOnlyList<TcCode> existing, IReadOnlyList<string> selected,
        int userId, CancellationToken ct = default);
}
