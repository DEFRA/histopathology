using Histo.Core.Domain;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;

namespace Histo.Submissions.Services;

/// <summary>
/// Application service for batch (submission header) management.
///
/// Replaces the database-persistence methods of legacy <c>clsBatch.vb</c>.
/// Batch status constants remain in <see cref="BatchStatus"/> (Histo.Core).
/// </summary>
public sealed class BatchService : IBatchService
{
    private readonly IBatchRepository _batches;
    private readonly IAppLogger _logger;

    public BatchService(IBatchRepository batches, IAppLogger logger)
    {
        _batches = batches;
        _logger  = logger;
    }

    /// <summary>Returns a batch by ID, or <see langword="null"/> if not found.</summary>
    public async Task<Batch?> GetByIdAsync(int batchId, CancellationToken ct = default)
    {
        try { return await _batches.GetByIdAsync(batchId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get batch {BatchId}.", ex, batchId); return null; }
    }

    /// <summary>Returns all received batches.</summary>
    public async Task<IReadOnlyList<BatchListResult>> GetReceivedAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetReceivedAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get received batches.", ex); return []; }
    }

    /// <summary>Returns all in-progress batches.</summary>
    public async Task<IReadOnlyList<BatchListResult>> GetInProgressAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetInProgressAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get in-progress batches.", ex); return []; }
    }

    /// <summary>Returns all batches on hold.</summary>
    public async Task<IReadOnlyList<BatchListResult>> GetOnHoldAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetOnHoldAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get on-hold batches.", ex); return []; }
    }

    /// <summary>
    /// Returns completed batches available for archiving.
    /// Legacy source: BatchesForArchiving.aspx.vb — <c>clsBatch.GetBatchesWithStatus(STATUS_COMPLETED)</c>.
    /// </summary>
    public async Task<IReadOnlyList<BatchListResult>> GetCompletedAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetCompletedAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get completed batches.", ex); return []; }
    }

    /// <summary>Returns batches not yet received.</summary>
    public async Task<IReadOnlyList<BatchListResult>> GetNotReceivedAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetNotReceivedAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get not-received batches.", ex); return []; }
    }

    /// <summary>
    /// Returns all batches regardless of status.
    /// Legacy source: BatchesForEditing.aspx.vb — <c>clsBatch.GetBatchesWithStatus(0)</c>.
    /// </summary>
    public async Task<IReadOnlyList<BatchListResult>> GetAllBatchesAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetAllBatchesAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get all batches.", ex); return []; }
    }

    /// <summary>
    /// Returns batches awaiting Quality Data entry.
    /// Legacy source: BatchesForDispatch.aspx — <c>clsBatch.GetBatchesForDispatch</c>.
    /// </summary>
    public async Task<IReadOnlyList<BatchListResult>> GetForDispatchAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetForDispatchAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get batches for dispatch.", ex); return []; }
    }

    /// <summary>Creates a new batch and returns its ID.</summary>
    public async Task<int> AddAsync(Batch batch, int userId, CancellationToken ct = default)
    {
        try { return await _batches.AddAsync(batch, userId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to add batch.", ex); return 0; }
    }

    /// <summary>
    /// Updates the editable batch header fields (CustomerRef, Comments, IsPreCassetted).
    /// Returns <see langword="false"/> and logs on failure.
    /// </summary>
    public async Task<bool> UpdateAsync(Batch batch, int userId, CancellationToken ct = default)
    {
        try { await _batches.UpdateAsync(batch, userId, ct); return true; }
        catch (Exception ex) { _logger.LogError("Failed to update batch {BatchId}.", ex, batch.ID); return false; }
    }

    /// <summary>
    /// Sets the customer received date ("date returned") on a batch without changing status.
    /// Returns <see langword="false"/> and logs on failure.
    /// Legacy source: <c>BatchDetails.aspx::btnSave_Click</c> when <c>SV_ReceiveBatch = True</c>.
    /// </summary>
    public async Task<bool> SetCustomerReceivedDateAsync(int batchId, DateTime? date, byte[] rowStamp, int userId, CancellationToken ct = default)
    {
        try { await _batches.SetCustomerReceivedDateAsync(batchId, date, rowStamp, userId, ct); return true; }
        catch (Exception ex) { _logger.LogError("Failed to set customer received date for batch {BatchId}.", ex, batchId); return false; }
    }

    /// <summary>
    /// Creates a new batch header copied from an existing one — the starting point
    /// for the "Copy batch" workflow. The new batch always starts in Submitted
    /// status regardless of the source batch's current status.
    ///
    /// Legacy source: HistopathologyLib/clsBatch.vb — <c>CopyBatch()</c> /
    /// <c>CopyDataToNewBatch()</c> (batch-header portion only; sub-tables are
    /// copied by the caller via <see cref="Histo.Submissions.Services.SubmissionService"/>).
    /// </summary>
    public async Task<int> CopyBatchHeaderAsync(Batch source, int userId, CancellationToken ct = default)
    {
        var batch = new Batch
        {
            Status            = BatchStatus.Submitted,
            CustomerRef        = source.CustomerRef,
            Comments           = source.Comments,
            SubmittedByUserID  = userId,
            UserAreaCode       = source.UserAreaCode,
            IsPreCassetted     = source.IsPreCassetted,
        };

        try { return await _batches.AddAsync(batch, userId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to copy batch {BatchId}.", ex, source.ID); return 0; }
    }

    /// <summary>
    /// Updates batch status. Throws <see cref="BatchConcurrencyException"/> on
    /// concurrent modification.
    /// </summary>
    public async Task<bool> UpdateStatusAsync(int batchId, string newStatus, byte[] rowStamp, int userId, CancellationToken ct = default)
    {
        // BatchConcurrencyException propagates — the UI must handle it
        await _batches.UpdateStatusAsync(batchId, newStatus, rowStamp, userId, ct);
        return true;
    }

    // -----------------------------------------------------------------------
    // Search (read-only)
    // -----------------------------------------------------------------------

    /// <summary>Multi-field submission search.</summary>
    public async Task<IReadOnlyList<BatchSearchResult>> SearchAsync(BatchSearchCriteria criteria, CancellationToken ct = default)
    {
        try { return await _batches.SearchAsync(criteria, ct); }
        catch (Exception ex) { _logger.LogError("Failed to search submissions.", ex); return []; }
    }

    /// <summary>
    /// Returns a simplified test-item listing for a project/date range.
    /// See <see cref="IBatchRepository.GetTestItemRowsAsync"/> for scope notes.
    /// </summary>
    public async Task<IReadOnlyList<TestItemRow>> GetTestItemRowsAsync(string? projectDesc, int batchType, CancellationToken ct = default)
    {
        try { return await _batches.GetTestItemRowsAsync(projectDesc, batchType, ct); }
        catch (Exception ex) { _logger.LogError("Failed to retrieve test item rows.", ex); return []; }
    }

    /// <summary>
    /// Recomputes and corrects the <c>CompletedDate</c> of every cassetted batch whose
    /// histology, antibodies and special-stain tests have all been dispatched, setting
    /// it to the latest dispatch date found across those tests. Batches with any
    /// outstanding (not-dispatched) test are left untouched.
    ///
    /// Replaces <c>FixCompletedDates.aspx.vb</c> — <c>btnUpdate_Click</c>. The legacy
    /// page wrapped every batch update in a single SQL transaction; here each batch is
    /// corrected independently so a failure on one batch does not prevent the others
    /// from being fixed.
    /// </summary>
    /// <returns>The number of batches whose completed date was updated.</returns>
    public async Task<int> FixCompletedDatesAsync(CancellationToken ct = default)
    {
        var updated = 0;
        try
        {
            var batchIds = await _batches.GetBatchIdsLinkedToBlocksAsync(ct);
            foreach (var batchId in batchIds)
            {
                try
                {
                    var latest = default(DateTime);
                    if (!TryGetLatestDispatchDate(await _batches.GetHistologyDispatchStatusAsync(batchId, ct), ref latest))
                        continue;

                    if (!TryGetLatestDispatchDate(await _batches.GetStainDispatchStatusAsync(batchId, ct), ref latest))
                        continue;

                    if (!TryGetLatestDispatchDate(await _batches.GetAntibodiesDispatchStatusAsync(batchId, ct), ref latest))
                        continue;

                    await _batches.UpdateCompletedDateAsync(batchId, latest, ct);
                    updated++;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to fix completed date for batch {BatchId}.", ex, batchId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve batches linked to blocks.", ex);
        }

        return updated;
    }

    /// <summary>
    /// Checks that every row is dispatched, updating <paramref name="latest"/> to the
    /// latest dispatch date found. Returns <see langword="false"/> (and leaves
    /// <paramref name="latest"/> unchanged) as soon as an un-dispatched row is found.
    /// </summary>
    private static bool TryGetLatestDispatchDate(IReadOnlyList<TestDispatchStatus> rows, ref DateTime latest)
    {
        foreach (var row in rows)
        {
            if (!row.Dispatched)
                return false;

            if (row.DispatchedDate is { } d && d > latest)
                latest = d;
        }
        return true;
    }

    // -----------------------------------------------------------------------
    // Batch-level test type selections
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public async Task<BatchTestSelections> GetBatchTestSelectionsAsync(int batchId, CancellationToken ct = default)
    {
        try { return await _batches.GetBatchTestSelectionsAsync(batchId, ct); }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get batch test selections for batch {BatchId}.", ex, batchId);
            return new BatchTestSelections();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SaveBatchTestSelectionsAsync(
        int batchId,
        IReadOnlyList<string> histologyCodes,
        IReadOnlyList<string> antibodyCodes,
        IReadOnlyList<string> stainCodes,
        int userId,
        CancellationToken ct = default)
    {
        try
        {
            await _batches.SaveBatchTestSelectionsAsync(batchId, histologyCodes, antibodyCodes, stainCodes, userId, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save batch test selections for batch {BatchId}.", ex, batchId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetSubmittedAsCodeAsync(int batchId, CancellationToken ct = default)
    {
        try
        {
            return await _batches.GetSubmittedAsCodeAsync(batchId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read submitted-as code for batch {BatchId}.", ex, batchId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetPostFixationCodesAsync(int batchId, CancellationToken ct = default)
    {
        try { return await _batches.GetPostFixationCodesAsync(batchId, ct); }
        catch (Exception ex)
        {
            _logger.LogError("Failed to get post-fixation codes for batch {BatchId}.", ex, batchId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SavePostFixationCodesAsync(int batchId, IReadOnlyList<string> codes, int userId, CancellationToken ct = default)
    {
        try
        {
            await _batches.SavePostFixationCodesAsync(batchId, codes, userId, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save post-fixation codes for batch {BatchId}.", ex, batchId);
            return false;
        }
    }
}
