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

    /// <summary>
    /// Returns all batches regardless of status. Maps to <c>GetAllBatches</c>.
    /// Legacy source: BatchesForEditing.aspx.vb — <c>clsBatch.GetBatchesWithStatus(0)</c> (status 0 = all).
    /// </summary>
    Task<IReadOnlyList<BatchListResult>> GetAllBatchesAsync(CancellationToken ct = default);

    /// <summary>Returns batches on hold. Maps to <c>GetBatchesOnHold</c>.</summary>
    Task<IReadOnlyList<BatchListResult>> GetOnHoldAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns completed batches available for archiving. Maps to <c>GetCompletedBatches</c>.
    /// Legacy source: BatchesForArchiving.aspx.vb — <c>clsBatch.GetBatchesWithStatus(STATUS_COMPLETED)</c>.
    /// </summary>
    Task<IReadOnlyList<BatchListResult>> GetCompletedAsync(CancellationToken ct = default);

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
    /// Sets the customer received date on a batch without changing any other field.
    /// Maps to <c>EditBatch</c> SP with <c>CustomerReceivedDate</c> added as a parameter.
    ///
    /// NOTE: the <c>EditBatch</c> SP must accept a <c>CustomerReceivedDate</c> named parameter.
    /// Add it to the SP definition if absent before deploying the Date Returned page.
    ///
    /// Legacy source: <c>BatchDetails.aspx.vb::btnSave_Click</c> when
    /// <c>SessionVars.SV_ReceiveBatch = True</c> — the legacy saved all fields via
    /// <c>UpdateBatchDetails</c>; this method loads existing values and only changes the date.
    /// </summary>
    Task SetCustomerReceivedDateAsync(int batchId, DateTime? date, byte[] rowStamp, int userId, CancellationToken ct = default);

    /// <summary>
    /// Updates batch status. Maps to <c>EditBatchStatus</c>.
    /// Throws <see cref="BatchConcurrencyException"/> on rowstamp mismatch.
    /// </summary>
    Task<bool> UpdateStatusAsync(int batchId, string newStatus, int userId, CancellationToken ct = default);

    /// <summary>
    /// Persists the ByPassSort flag. Reloads current batch to supply the full EditBatch parameter set.
    /// Legacy source: <c>BatchBlockSummary.aspx.vb</c>::<c>chkByPassSort_CheckedChanged</c>.
    /// </summary>
    Task SetByPassSortAsync(int batchId, bool byPassSort, int userId, CancellationToken ct = default);

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

    // -----------------------------------------------------------------------
    // Batch-level test type selections (Histology / Antibodies / Special Stains)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the batch-level test type selections (histology types, antibodies,
    /// special stains) for a given batch ID.
    ///
    /// Calls <c>GetCommonBatchTablesByID</c> with <c>QueryMultipleAsync</c> and reads
    /// result-set indices 1 (BATCH_HISTOLOGY_TABLE), 2 (BATCH_ANTIBODIES_TABLE), and
    /// 3 (BATCH_STAIN_TABLE).  Result-set 0 (batch header) is discarded.
    ///
    /// Legacy source: <c>clsBatch.vb::GetCommonBatchDetails</c> —
    /// the DataSet sub-tables populated from <c>GetCommonBatchTablesByID</c>.
    /// </summary>
    Task<BatchTestSelections> GetBatchTestSelectionsAsync(int batchId, CancellationToken ct = default);

    /// <summary>
    /// Persists the batch-level test type selections using a delta strategy:
    /// rows whose code is in the new selection but not in the existing set are inserted;
    /// rows in the existing set whose code is absent from the new selection are deleted.
    ///
    /// Insert/delete stored procedures per type:
    /// <list type="bullet">
    /// <item>Histology: <c>AddHistology</c> / <c>DeleteHistology</c></item>
    /// <item>Antibodies: <c>AddAntibodies</c> / <c>DeleteAntibodies</c></item>
    /// <item>Special stains: <c>AddSpecialStain</c> / <c>DeleteSpecialStain</c></item>
    /// </list>
    ///
    /// Legacy source: <c>clsCheckBoxData.vb::UpdateTable</c> —
    /// called from <c>clsBatch.vb::UpdateBatchDetails</c> for BATCH_HISTOLOGY_TABLE,
    /// BATCH_ANTIBODIES_TABLE, and BATCH_STAIN_TABLE.
    /// </summary>
    Task SaveBatchTestSelectionsAsync(
        int batchId,
        IReadOnlyList<string> histologyCodes,
        IReadOnlyList<string> antibodyCodes,
        IReadOnlyList<string> stainCodes,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the raw "Submitted As" code for a batch from <c>BATCH_SUBMITTEDAS_TABLE</c>
    /// (result-set index 5 of <c>GetCommonBatchTablesByID</c>).
    /// Returns <see langword="null"/> when no record exists or the SP does not return that result set.
    /// Legacy source: <c>BatchDetails.aspx.vb::InitialiseScreenWithBatchDetails</c> —
    /// reads <c>foundRows(0)("Code")</c> then resolves via <c>GetListType(code, LOOKUP_SUBMITTEDAS)</c>.
    /// </summary>
    Task<string?> GetSubmittedAsCodeAsync(int batchId, CancellationToken ct = default);

    /// <summary>Saves (replaces) the SubmittedAs code. Calls <c>AddSubmittedAs</c> SP.</summary>
    Task SaveSubmittedAsAsync(int batchId, string code, int userId, CancellationToken ct = default);
}
