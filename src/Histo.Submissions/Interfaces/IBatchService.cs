using Histo.Submissions.Models;

namespace Histo.Submissions.Interfaces;

/// <summary>
/// Public service contract for batch (submission header) management — the module boundary exposed to Histo.Web.
/// Concrete implementation: <see cref="Histo.Submissions.Services.BatchService"/>.
/// </summary>
public interface IBatchService
{
    /// <summary>Returns a batch by ID, or <see langword="null"/> if not found.</summary>
    Task<Batch?> GetByIdAsync(int batchId, CancellationToken ct = default);

    Task<IReadOnlyList<BatchListResult>> GetReceivedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BatchListResult>> GetInProgressAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BatchListResult>> GetOnHoldAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BatchListResult>> GetCompletedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BatchListResult>> GetNotReceivedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BatchListResult>> GetAllBatchesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BatchListResult>> GetForDispatchAsync(CancellationToken ct = default);

    /// <summary>Creates a new batch. Returns the new ID, or 0 on failure.</summary>
    Task<int> AddAsync(Batch batch, int userId, CancellationToken ct = default);

    /// <summary>Updates the editable batch header fields. Returns <see langword="false"/> on failure.</summary>
    Task<bool> UpdateAsync(Batch batch, int userId, CancellationToken ct = default);

    /// <summary>Creates a new batch header copied from an existing one. Returns the new ID, or 0 on failure.</summary>
    Task<int> CopyBatchHeaderAsync(Batch source, int userId, CancellationToken ct = default);

    /// <summary>Updates batch status. Throws <see cref="BatchConcurrencyException"/> on concurrent modification.</summary>
    Task<bool> UpdateStatusAsync(int batchId, string newStatus, byte[] rowStamp, int userId, CancellationToken ct = default);

    /// <summary>Multi-field submission search.</summary>
    Task<IReadOnlyList<BatchSearchResult>> SearchAsync(BatchSearchCriteria criteria, CancellationToken ct = default);

    /// <summary>Returns a simplified test-item listing for a project/date range.</summary>
    Task<IReadOnlyList<TestItemRow>> GetTestItemRowsAsync(string? projectDesc, int batchType, CancellationToken ct = default);

    /// <summary>Recomputes and corrects the CompletedDate of all cassetted batches. Returns the number of batches updated.</summary>
    Task<int> FixCompletedDatesAsync(CancellationToken ct = default);
}
