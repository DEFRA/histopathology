using Histo.Submissions.Models;

namespace Histo.Submissions.Interfaces;

/// <summary>
/// Data access contract for batch (submission) records.
///
/// Legacy source: HistopathologyLib/clsBatch.vb — all stored procedure calls
/// translated to async Dapper pattern.
/// </summary>
public interface IBatchRepository
{
    /// <summary>
    /// Returns batch header + all sub-tables (histology, antibodies, stains,
    /// post-fixations, submitted-as) for a given batch ID.
    /// Maps to <c>GetCommonBatchTablesByID</c>.
    /// </summary>
    Task<Batch?> GetByIdAsync(int batchId, CancellationToken ct = default);

    /// <summary>Returns all batches in Received status. Maps to <c>GetReceivedBatches</c>.</summary>
    Task<IReadOnlyList<BatchListResult>> GetReceivedAsync(CancellationToken ct = default);

    /// <summary>Returns all batches in InProgress status. Maps to <c>GetInProgressBatches</c>.</summary>
    Task<IReadOnlyList<BatchListResult>> GetInProgressAsync(CancellationToken ct = default);

    /// <summary>Returns batches not yet received. Maps to <c>GetBatchesNotReceived</c>.</summary>
    Task<IReadOnlyList<BatchListResult>> GetNotReceivedAsync(CancellationToken ct = default);

    /// <summary>Returns batches on hold. Maps to <c>GetBatchesOnHold</c>.</summary>
    Task<IReadOnlyList<BatchListResult>> GetOnHoldAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns batches awaiting Quality Data entry. Maps to <c>GetBatchesForDispatch</c>.
    /// Legacy source: BatchesForDispatch.aspx.vb — <c>clsBatch.GetBatchesForDispatch</c>.
    /// </summary>
    Task<IReadOnlyList<BatchListResult>> GetForDispatchAsync(CancellationToken ct = default);

    /// <summary>
    /// Adds a new batch. Maps to <c>AddBatch</c>.
    /// Returns the new batch ID.
    /// </summary>
    Task<int> AddAsync(Batch batch, int userId, CancellationToken ct = default);

    /// <summary>Updates a batch header. Maps to <c>EditBatch</c>.</summary>
    Task UpdateAsync(Batch batch, int userId, CancellationToken ct = default);

    /// <summary>
    /// Updates batch status. Maps to <c>EditBatchStatus</c>.
    /// Throws <see cref="BatchConcurrencyException"/> on rowstamp mismatch.
    /// </summary>
    Task UpdateStatusAsync(int batchId, string newStatus, byte[] rowStamp, int userId, CancellationToken ct = default);

    /// <summary>Returns comment rows for a batch. Maps to <c>GetAllBatchComments</c>.</summary>
    Task<IReadOnlyList<string>> GetCommentsAsync(int batchId, CancellationToken ct = default);

    // -----------------------------------------------------------------------
    // Search (read-only)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Multi-field submission search. Maps to <c>GetSearchBatchDetails</c>.
    /// Legacy source: SearchSubmissions.aspx.vb — <c>clsBatch.SearchBatchDetails</c>.
    /// </summary>
    Task<IReadOnlyList<BatchSearchResult>> SearchAsync(BatchSearchCriteria criteria, CancellationToken ct = default);

    /// <summary>
    /// Returns a simplified test-item listing for a project/date range.
    /// Maps to <c>GetTestRows</c>. Legacy source: SearchTest.aspx.vb — <c>clsBatch.GetTestItemRows</c>.
    ///
    /// SIMPLIFIED: the legacy screen additionally builds a histology/antibody/special-stain
    /// checkbox-driven premium-charge cross-tab via <c>CountHistologysTestItems</c>,
    /// <c>CountStainTestItems</c>, and <c>CountAntibodesTestItems</c> — that analytics
    /// engine is not ported. See the search module report for details.
    /// </summary>
    Task<IReadOnlyList<TestItemRow>> GetTestItemRowsAsync(string? projectDesc, int batchType, CancellationToken ct = default);

    // -----------------------------------------------------------------------
    // Fix Completed Dates (admin data-correction utility)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the IDs of all batches that have blocks (cassetted batches).
    /// Maps to <c>GetBatchesLinkedToBlocks</c>.
    /// Legacy source: <c>FixCompletedDates.aspx.vb</c> — <c>GetBatchIDs</c>.
    /// </summary>
    Task<IReadOnlyList<int>> GetBatchIdsLinkedToBlocksAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the dispatch status of every histology test on a batch.
    /// Maps to <c>GetHistologyDispatched</c>.
    /// Legacy source: <c>FixCompletedDates.aspx.vb</c> — <c>GetBatchHistology</c>.
    /// </summary>
    Task<IReadOnlyList<TestDispatchStatus>> GetHistologyDispatchStatusAsync(int batchId, CancellationToken ct = default);

    /// <summary>
    /// Returns the dispatch status of every special stain test on a batch.
    /// Maps to <c>GetStainDispatched</c>.
    /// Legacy source: <c>FixCompletedDates.aspx.vb</c> — <c>GetBatchStain</c>.
    /// </summary>
    Task<IReadOnlyList<TestDispatchStatus>> GetStainDispatchStatusAsync(int batchId, CancellationToken ct = default);

    /// <summary>
    /// Returns the dispatch status of every antibodies test on a batch.
    /// Maps to <c>GetAntibodiesDispatched</c>.
    /// Legacy source: <c>FixCompletedDates.aspx.vb</c> — <c>GetBatchAntibodies</c>.
    /// </summary>
    Task<IReadOnlyList<TestDispatchStatus>> GetAntibodiesDispatchStatusAsync(int batchId, CancellationToken ct = default);

    /// <summary>
    /// Sets a batch's completed date directly (bypasses normal status workflow).
    /// Maps to <c>EditBatchCompletedDate</c>.
    /// Legacy source: <c>FixCompletedDates.aspx.vb</c> — <c>UpdateBatchCompletedDate</c>.
    /// </summary>
    Task UpdateCompletedDateAsync(int batchId, DateTime completedDate, CancellationToken ct = default);
}
