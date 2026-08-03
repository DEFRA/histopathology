using Histo.Submissions.Models;

namespace Histo.Submissions.Interfaces;

/// <summary>
/// Data access contract for batch submission (sample group), animal, and tissue records.
///
/// Legacy source: HistopathologyLib/clsBatchSubmission.vb, clsAnimal.vb, clsTissue.vb
/// translated to async Dapper pattern.
/// </summary>
public interface ISubmissionRepository
{
    // -----------------------------------------------------------------------
    // Batch Submissions (sample groups)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns all submissions (sample groups) for a batch.
    /// Maps to <c>GetBatchSubmissionDetailsByBatchID</c> (BATCH_SUBMISSION_TABLE).
    /// </summary>
    Task<IReadOnlyList<BatchSubmission>> GetSubmissionsByBatchAsync(int batchId, CancellationToken ct = default);

    /// <summary>Adds a batch submission. Maps to <c>AddBatchSubmission</c>. Returns new ID.</summary>
    Task<int> AddSubmissionAsync(BatchSubmission submission, int userId, CancellationToken ct = default);

    /// <summary>Updates a batch submission. Maps to <c>EditBatchSubmission</c>.</summary>
    Task UpdateSubmissionAsync(BatchSubmission submission, int userId, CancellationToken ct = default);

    // -----------------------------------------------------------------------
    // Animals
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns all animals for a batch submission.
    /// Maps to the BATCH_ANIMAL_TABLE (index 8) in <c>GetBatchSubmissionDetailsByBatchID</c>.
    /// </summary>
    Task<IReadOnlyList<Animal>> GetAnimalsByBatchAsync(int batchId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new animal record. Maps to <c>AddAnimal</c>.
    /// Returns new animal ID.
    /// </summary>
    Task<int> AddAnimalAsync(Animal animal, int userId, CancellationToken ct = default);

    /// <summary>Updates an animal record. Maps to <c>EditAnimal</c>.</summary>
    Task UpdateAnimalAsync(Animal animal, int userId, CancellationToken ct = default);

    /// <summary>Deletes an animal record. Maps to <c>DeleteAnimal</c>.</summary>
    Task DeleteAnimalAsync(int animalId, int userId, CancellationToken ct = default);

    // -----------------------------------------------------------------------
    // Tissues
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns all tissues for a batch submission (not block tissues).
    /// Maps to BATCH_TISSUES_TABLE (index 7) in <c>GetBatchSubmissionDetailsByBatchID</c>.
    /// </summary>
    Task<IReadOnlyList<Tissue>> GetTissuesBySubmissionAsync(int submissionId, CancellationToken ct = default);

    /// <summary>Adds a tissue record. Maps to <c>AddTissue</c>. Returns new ID.</summary>
    Task<int> AddTissueAsync(Tissue tissue, int userId, CancellationToken ct = default);

    /// <summary>Updates a tissue record. Maps to <c>EditTissue</c> or <c>EditBlockTissue</c>.</summary>
    Task UpdateTissueAsync(Tissue tissue, int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns all tissues for a block.
    /// Maps to <c>GetTissuesByBlockID</c> — a new stored procedure mirroring the
    /// existing <c>GetTissuesBySubmissionID</c> pattern. The legacy application
    /// never queried block tissues in isolation (they were always loaded as part
    /// of the whole-batch DataSet via <c>GetBatchBlocksByID</c> and filtered
    /// in-memory by BlockID) — this method is added to support the "Copy blocks"
    /// and "Copy samples" workflows, which need a single block's tissues without
    /// loading the entire batch.
    /// </summary>
    Task<IReadOnlyList<Tissue>> GetTissuesByBlockAsync(int blockId, CancellationToken ct = default);

    /// <summary>Deletes a tissue record. Maps to <c>DeleteTissue</c> or <c>DeleteBlockTissue</c>.</summary>
    Task DeleteTissueAsync(int tissueId, TissueOwner owner, int userId, CancellationToken ct = default);

    // -----------------------------------------------------------------------
    // Search (read-only)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns submissions whose PM date falls within the given range.
    /// Maps to <c>GetSearchPMDates</c>. Legacy source: SearchPMDates.aspx.
    /// </summary>
    Task<IReadOnlyList<PmDateSearchResult>> GetByPmDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    /// <summary>
    /// Returns animals/samples matching a (partial) sender reference.
    /// Maps to <c>GetAnimalsBySenderRef</c>. Legacy source: SearchSample.aspx, SearchSender.aspx.
    /// </summary>
    Task<IReadOnlyList<SenderSearchResult>> GetAnimalsBySenderRefAsync(string senderRef, CancellationToken ct = default);

    /// <summary>
    /// Returns archived tissue records matching the given (optional) filters.
    /// Maps to <c>GetAnimalTissuesArchiveInformation</c>. Legacy source: SearchArchiveLocation.aspx (Tissue Archive mode).
    /// </summary>
    Task<IReadOnlyList<TissueArchiveInfo>> GetTissueArchiveAsync(
        string? senderRef, string? histologyRef, string? archiveLocation, string? tissueCode, CancellationToken ct = default);

    /// <summary>
    /// Returns legacy imported ICC_Sub data rows for a selected imported-table ID
    /// (an empty/null value returns no rows; an unrecognised ID falls back to the
    /// combined <c>GetAllImportedData</c> result, matching the legacy Select Case behaviour).
    /// Maps to <c>GetImportedData</c>. Legacy source: ViewImportedData.aspx.
    /// </summary>
    Task<IReadOnlyList<ImportedDataRow>> GetImportedDataAsync(string? selectedTable, CancellationToken ct = default);
}
