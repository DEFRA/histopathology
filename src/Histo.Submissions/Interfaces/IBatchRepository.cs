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
    Task<IReadOnlyList<Batch>> GetReceivedAsync(CancellationToken ct = default);

    /// <summary>Returns all batches in InProgress status. Maps to <c>GetInProgressBatches</c>.</summary>
    Task<IReadOnlyList<Batch>> GetInProgressAsync(CancellationToken ct = default);

    /// <summary>Returns batches not yet received. Maps to <c>GetBatchesNotReceived</c>.</summary>
    Task<IReadOnlyList<Batch>> GetNotReceivedAsync(CancellationToken ct = default);

    /// <summary>Returns batches on hold. Maps to <c>GetBatchesOnHold</c>.</summary>
    Task<IReadOnlyList<Batch>> GetOnHoldAsync(CancellationToken ct = default);

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
}
