using Histo.Core.Domain;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;

namespace Histo.Submissions.Services;

/// <summary>
/// Application service for batch submission (sample group), animal, and tissue management.
///
/// Replaces direct use of <c>clsBatchSubmission.vb</c>, <c>clsAnimal.vb</c>, and
/// <c>clsTissue.vb</c> from ASPX code-behind files.
///
/// PG-number auto-reversal logic is delegated to <see cref="AnimalHelpers.ComputePgAutoHistologyRef"/>.
/// </summary>
public sealed class SubmissionService : ISubmissionService
{
    private readonly ISubmissionRepository _repo;
    private readonly IAppLogger _logger;

    public SubmissionService(ISubmissionRepository repo, IAppLogger logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // Batch Submissions
    // -----------------------------------------------------------------------

    /// <summary>Returns all submissions (sample groups) for a batch.</summary>
    public async Task<IReadOnlyList<BatchSubmission>> GetSubmissionsByBatchAsync(int batchId, CancellationToken ct = default)
    {
        try { return await _repo.GetSubmissionsByBatchAsync(batchId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get submissions for batch {BatchId}.", ex, batchId); return []; }
    }

    /// <summary>Adds a batch submission and returns the new ID.</summary>
    public async Task<int> AddSubmissionAsync(BatchSubmission submission, int userId, CancellationToken ct = default)
    {
        try { return await _repo.AddSubmissionAsync(submission, userId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to add submission.", ex); return 0; }
    }

    /// <summary>
    /// Creates a copy of an existing batch submission (sample group) under a new
    /// batch. Used by the "Copy batch" workflow.
    /// </summary>
    public async Task<int> CopySubmissionAsync(BatchSubmission source, int newBatchId, int userId, CancellationToken ct = default)
    {
        var submission = new BatchSubmission
        {
            BatchID        = newBatchId,
            SubmissionName = source.SubmissionName,
            Order          = source.Order,
        };

        try { return await _repo.AddSubmissionAsync(submission, userId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to copy submission {SubmissionId}.", ex, source.ID); return 0; }
    }

    // -----------------------------------------------------------------------
    // Animals
    // -----------------------------------------------------------------------

    /// <summary>Returns all animals for a batch.</summary>
    public async Task<IReadOnlyList<Animal>> GetAnimalsByBatchAsync(int batchId, CancellationToken ct = default)
    {
        try { return await _repo.GetAnimalsByBatchAsync(batchId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get animals for batch {BatchId}.", ex, batchId); return []; }
    }

    /// <summary>Returns animals from the block-workflow animal table (BATCH_BLOCK_ANIMAL).</summary>
    public async Task<IReadOnlyList<Animal>> GetBlockAnimalsByBatchAsync(int batchId, CancellationToken ct = default)
    {
        try { return await _repo.GetBlockAnimalsByBatchAsync(batchId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get block animals for batch {BatchId}.", ex, batchId); return []; }
    }

    /// <summary>
    /// Adds a new animal record.
    ///
    /// If <paramref name="isNeuropath"/> is <see langword="true"/> and
    /// <paramref name="senderRef"/> is in PG-number format, the histology
    /// reference is auto-computed via <see cref="AnimalHelpers.ComputePgAutoHistologyRef"/>
    /// before persisting.
    /// </summary>
    public async Task<int> AddAnimalAsync(
        int batchSubmissionId,
        string senderRef,
        bool isNeuropath,
        int userId,
        string? pmDate = null,
        bool pmDateSet = false,
        CancellationToken ct = default)
    {
        // Apply PG-number auto-reversal — mirrors NewRecord() in clsAnimal.vb
        var autoHistologyRef = AnimalHelpers.ComputePgAutoHistologyRef(senderRef, isNeuropath);

        var animal = new Animal
        {
            BatchSubmissionID = batchSubmissionId,
            SenderRef         = senderRef,
            NextBlockRef      = "01",
            HistologyRef      = autoHistologyRef,
            HistoRefSet       = false,
            OnHold            = false,
            PMDate            = pmDate,
            PMDateSet         = pmDateSet,
            IsPGNumber        = autoHistologyRef is not null,
            BookedHistologyRef = false,
        };

        try { return await _repo.AddAnimalAsync(animal, userId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to add animal for submission {SubmissionId}.", ex, batchSubmissionId); return 0; }
    }

    /// <summary>
    /// Creates a copy of an existing animal under a new batch submission,
    /// preserving its histology reference and hold/PM-date state but allowing
    /// the sender reference to be changed. Used by the "Copy batch" workflow.
    ///
    /// Legacy source: HistopathologyLib/clsBatchSubmission.vb — <c>CopyDataToNewBatch()</c>,
    /// which duplicates BATCH_ANIMAL_TABLE rows onto the new batch submission.
    /// </summary>
    public async Task<int> CopyAnimalAsync(Animal source, int newBatchSubmissionId, string newSenderRef, int userId, CancellationToken ct = default)
    {
        var animal = new Animal
        {
            BatchSubmissionID  = newBatchSubmissionId,
            SenderRef          = newSenderRef,
            NextBlockRef       = source.NextBlockRef,
            HistoRefSet        = source.HistoRefSet,
            HistologyRef       = source.HistologyRef,
            OnHold             = source.OnHold,
            PMDate             = source.PMDate,
            PMDateSet          = source.PMDateSet,
            IsPGNumber         = source.IsPGNumber,
            BookedHistologyRef = source.BookedHistologyRef,
        };

        try { return await _repo.AddAnimalAsync(animal, userId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to copy animal {AnimalId}.", ex, source.ID); return 0; }
    }

    /// <summary>Updates an animal record.</summary>
    public async Task<bool> UpdateAnimalAsync(Animal animal, int userId, CancellationToken ct = default)
    {
        try { await _repo.UpdateAnimalAsync(animal, userId, ct); return true; }
        catch (Exception ex) { _logger.LogError("Failed to update animal {AnimalId}.", ex, animal.ID); return false; }
    }

    /// <summary>Deletes an animal record.</summary>
    public async Task<bool> DeleteAnimalAsync(int animalId, int userId, CancellationToken ct = default)
    {
        try { await _repo.DeleteAnimalAsync(animalId, userId, ct); return true; }
        catch (Exception ex) { _logger.LogError("Failed to delete animal {AnimalId}.", ex, animalId); return false; }
    }

    /// <summary>
    /// Renames the Sender Ref of an existing sample. Throws
    /// <see cref="AnimalRefUpdateException"/> when the original Sender Ref is not
    /// found or the new Sender Ref is already in use — the UI layer must handle it.
    /// Legacy source: EditHistologyRef.aspx.vb — <c>cmdEditSenderRef_Click</c>.
    /// </summary>
    public async Task UpdateAnimalSenderRefAsync(string senderRef, string newSenderRef, int userId, CancellationToken ct = default)
        => await _repo.UpdateAnimalSenderRefAsync(senderRef, newSenderRef, userId, ct);

    /// <summary>
    /// Renames (or clears, when <paramref name="newHistologyRef"/> is empty) the
    /// Histology Ref of an existing sample, identified by its Sender Ref. Throws
    /// <see cref="AnimalRefUpdateException"/> when the Sender Ref is not found or
    /// the new Histology Ref is already in use — the UI layer must handle it.
    /// Legacy source: EditHistologyRef.aspx.vb — <c>cmdSaveHistologyRef_Click</c>.
    /// </summary>
    public async Task UpdateAnimalHistologyRefAsync(string senderRef, string? newHistologyRef, int userId, CancellationToken ct = default)
        => await _repo.UpdateAnimalHistologyRefAsync(senderRef, newHistologyRef, userId, ct);

    // -----------------------------------------------------------------------
    // Tissues
    // -----------------------------------------------------------------------

    /// <summary>Returns tissues for a submission.</summary>
    public async Task<IReadOnlyList<Tissue>> GetTissuesBySubmissionAsync(int batchId, int submissionId, CancellationToken ct = default)
    {
        try { return await _repo.GetTissuesBySubmissionAsync(batchId, submissionId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get tissues for submission {SubmissionId}.", ex, submissionId); return []; }
    }

    public async Task<IReadOnlyList<Tissue>> GetBatchSubmissionTissuesAsync(int batchId, CancellationToken ct = default)
    {
        try { return await _repo.GetBatchSubmissionTissuesAsync(batchId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get submission tissues for batch {BatchId}.", ex, batchId); return []; }
    }

    /// <summary>Returns all block-owned tissues for a batch.</summary>
    public async Task<IReadOnlyList<Tissue>> GetTissuesByBatchAsync(int batchId, CancellationToken ct = default)
    {
        try { return await _repo.GetTissuesByBatchAsync(batchId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get block tissues for batch {BatchId}.", ex, batchId); return []; }
    }

    /// <summary>Returns tissues for a block.</summary>
    public async Task<IReadOnlyList<Tissue>> GetTissuesByBlockAsync(int batchId, int blockId, CancellationToken ct = default)
    {
        try { return await _repo.GetTissuesByBlockAsync(batchId, blockId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get tissues for block {BlockId}.", ex, blockId); return []; }
    }

    /// <summary>Adds a tissue record and returns the new ID.</summary>
    public async Task<int> AddTissueAsync(Tissue tissue, int userId, CancellationToken ct = default)
    {
        try { return await _repo.AddTissueAsync(tissue, userId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to add tissue.", ex); return 0; }
    }

    /// <summary>
    /// Creates a copy of an existing tissue under a new owner (submission or
    /// block). Used by the "Copy batch", "Copy blocks", and "Copy samples" workflows.
    /// Archive fields are intentionally not copied — a duplicated tissue starts unarchived.
    /// </summary>
    public async Task<int> CopyTissueAsync(Tissue source, int newOwnerId, int userId, CancellationToken ct = default)
    {
        var tissue = new Tissue
        {
            OwnerID    = newOwnerId,
            Owner      = source.Owner,
            TissueCode = source.TissueCode,
            NoPieces   = source.NoPieces,
            Comment    = source.Comment,
        };

        try { return await _repo.AddTissueAsync(tissue, userId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to copy tissue {TissueId}.", ex, source.ID); return 0; }
    }

    /// <summary>Updates a tissue record.</summary>
    public async Task<bool> UpdateTissueAsync(Tissue tissue, int userId, CancellationToken ct = default)
    {
        try { await _repo.UpdateTissueAsync(tissue, userId, ct); return true; }
        catch (Exception ex) { _logger.LogError("Failed to update tissue {TissueId}.", ex, tissue.ID); return false; }
    }

    /// <summary>Deletes a tissue record.</summary>
    public async Task<bool> DeleteTissueAsync(int tissueId, TissueOwner owner, int userId, CancellationToken ct = default)
    {
        try { await _repo.DeleteTissueAsync(tissueId, owner, userId, ct); return true; }
        catch (Exception ex) { _logger.LogError("Failed to delete tissue {TissueId}.", ex, tissueId); return false; }
    }

    // -----------------------------------------------------------------------
    // Search (read-only)
    // -----------------------------------------------------------------------

    /// <summary>Returns submissions whose PM date falls within the given range.</summary>
    public async Task<IReadOnlyList<PmDateSearchResult>> GetByPmDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        try { return await _repo.GetByPmDateRangeAsync(fromDate, toDate, ct); }
        catch (Exception ex) { _logger.LogError("Failed to search submissions by PM date.", ex); return []; }
    }

    /// <summary>Returns animals/samples matching a (partial) sender reference.</summary>
    public async Task<IReadOnlyList<SenderSearchResult>> GetAnimalsBySenderRefAsync(string senderRef, CancellationToken ct = default)
    {
        try { return await _repo.GetAnimalsBySenderRefAsync(senderRef, ct); }
        catch (Exception ex) { _logger.LogError("Failed to search animals by sender ref {SenderRef}.", ex, senderRef); return []; }
    }

    /// <summary>
    /// Returns the single animal record that exactly matches the given Sender Ref.
    /// Uses the <c>GetAnimalBySender</c> SP (exact match), mirroring the legacy
    /// <c>EditHistologyRef.aspx::getHistologyRef</c> lookup behaviour.
    /// </summary>
    public async Task<IReadOnlyList<SenderSearchResult>> GetAnimalBySenderAsync(string senderRef, CancellationToken ct = default)
    {
        try { return await _repo.GetAnimalBySenderAsync(senderRef, ct); }
        catch (Exception ex) { _logger.LogError("Failed to look up animal by exact sender ref {SenderRef}.", ex, senderRef); return []; }
    }

    /// <summary>Returns archived tissue records matching the given (optional) filters.</summary>
    public async Task<IReadOnlyList<TissueArchiveInfo>> GetTissueArchiveAsync(
        string? senderRef, string? histologyRef, string? archiveLocation, string? tissueCode, CancellationToken ct = default)
    {
        try { return await _repo.GetTissueArchiveAsync(senderRef, histologyRef, archiveLocation, tissueCode, ct); }
        catch (Exception ex) { _logger.LogError("Failed to search tissue archive information.", ex); return []; }
    }

    /// <summary>Returns legacy imported ICC_Sub data rows for the selected imported-table ID.</summary>
    public async Task<IReadOnlyList<ImportedDataRow>> GetImportedDataAsync(string? selectedTable, CancellationToken ct = default)
    {
        try { return await _repo.GetImportedDataAsync(selectedTable, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get imported data for table {SelectedTable}.", ex, selectedTable ?? "(none)"); return []; }
    }

    /// <summary>Standalone ViewSamples search — "Tissue Information" mode.</summary>
    public async Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalTissuesAsync(
        string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default)
    {
        try { return await _repo.GetAnimalTissuesAsync(senderRef, histologyRef, tissueCode, projectDesc, ct); }
        catch (Exception ex) { _logger.LogError("Failed to search animal tissues.", ex); return []; }
    }

    /// <summary>Standalone ViewSamples search — "Block Information" mode.</summary>
    public async Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalBlockTissuesAsync(
        string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default)
    {
        try { return await _repo.GetAnimalBlockTissuesAsync(senderRef, histologyRef, tissueCode, projectDesc, ct); }
        catch (Exception ex) { _logger.LogError("Failed to search animal block tissues.", ex); return []; }
    }
}
