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
    Task<bool> UpdateStatusAsync(int batchId, string newStatus, int userId, CancellationToken ct = default);

    /// <summary>
    /// Persists the ByPassSort flag on a batch.
    /// Legacy source: <c>BatchBlockSummary.aspx.vb</c>::<c>chkByPassSort_CheckedChanged</c>.
    /// </summary>
    Task<bool> SetByPassSortAsync(int batchId, bool byPassSort, int userId, CancellationToken ct = default);

    /// <summary>Multi-field submission search.</summary>
    Task<IReadOnlyList<BatchSearchResult>> SearchAsync(BatchSearchCriteria criteria, CancellationToken ct = default);

    /// <summary>Returns a simplified test-item listing for a project/date range.</summary>
    Task<IReadOnlyList<TestItemRow>> GetTestItemRowsAsync(string? projectDesc, int batchType, CancellationToken ct = default);

    /// <summary>Recomputes and corrects the CompletedDate of all cassetted batches. Returns the number of batches updated.</summary>
    Task<int> FixCompletedDatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the customer received date ("date returned") on a batch without changing any other field.
    /// Returns <see langword="false"/> on failure.
    /// Legacy source: <c>BatchDetails.aspx</c> in receive mode (<c>SV_ReceiveBatch = True</c>).
    /// </summary>
    Task<bool> SetCustomerReceivedDateAsync(int batchId, DateTime? date, byte[] rowStamp, int userId, CancellationToken ct = default);

    // -----------------------------------------------------------------------
    // Batch-level test type selections (Histology / Antibodies / Special Stains)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the batch-level histology, antibody, and special-stain type selections
    /// for the given batch.
    /// Legacy source: <c>BatchDetails.aspx</c> — "Select Histology and required tests" section.
    /// </summary>
    Task<BatchTestSelections> GetBatchTestSelectionsAsync(int batchId, CancellationToken ct = default);

    /// <summary>
    /// Persists the batch-level test type selections.  Existing rows whose code is absent
    /// from the new selection are deleted; new codes are inserted.
    /// Returns <see langword="false"/> on failure.
    /// Legacy source: <c>clsBatch.vb::UpdateBatchDetails</c> — <c>clsCheckBoxData.UpdateTable</c>
    /// called for BATCH_HISTOLOGY_TABLE, BATCH_ANTIBODIES_TABLE, BATCH_STAIN_TABLE.
    /// </summary>
    Task<bool> SaveBatchTestSelectionsAsync(
        int batchId,
        IReadOnlyList<string> histologyCodes,
        IReadOnlyList<string> antibodyCodes,
        IReadOnlyList<string> stainCodes,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the raw "Submitted As" code for a batch (from <c>BATCH_SUBMITTEDAS_TABLE</c>).
    /// Returns <see langword="null"/> when no record exists.
    /// Legacy: <c>LOOKUP_SUBMITTEDAS = 11</c> is used by the caller to resolve the code to a display name.
    /// </summary>
    Task<string?> GetSubmittedAsCodeAsync(int batchId, CancellationToken ct = default);

    /// <summary>Inserts a SubmittedAs code record for the batch. Does not replace any existing entry.</summary>
    Task SaveSubmittedAsAsync(int batchId, string code, int userId, CancellationToken ct = default);

    /// <summary>Returns the post-fixation codes currently selected for a batch.</summary>
    Task<IReadOnlyList<string>> GetPostFixationCodesAsync(int batchId, CancellationToken ct = default);

    /// <summary>Persists post-fixation selections. Returns <see langword="false"/> on failure.</summary>
    Task<bool> SavePostFixationCodesAsync(int batchId, IReadOnlyList<string> codes, int userId, CancellationToken ct = default);
}
