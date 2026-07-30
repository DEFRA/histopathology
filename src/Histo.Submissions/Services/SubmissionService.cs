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
public sealed class SubmissionService
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

    // -----------------------------------------------------------------------
    // Animals
    // -----------------------------------------------------------------------

    /// <summary>Returns all animals for a batch.</summary>
    public async Task<IReadOnlyList<Animal>> GetAnimalsByBatchAsync(int batchId, CancellationToken ct = default)
    {
        try { return await _repo.GetAnimalsByBatchAsync(batchId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get animals for batch {BatchId}.", ex, batchId); return []; }
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

    // -----------------------------------------------------------------------
    // Tissues
    // -----------------------------------------------------------------------

    /// <summary>Returns tissues for a submission.</summary>
    public async Task<IReadOnlyList<Tissue>> GetTissuesBySubmissionAsync(int submissionId, CancellationToken ct = default)
    {
        try { return await _repo.GetTissuesBySubmissionAsync(submissionId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get tissues for submission {SubmissionId}.", ex, submissionId); return []; }
    }

    /// <summary>Adds a tissue record and returns the new ID.</summary>
    public async Task<int> AddTissueAsync(Tissue tissue, int userId, CancellationToken ct = default)
    {
        try { return await _repo.AddTissueAsync(tissue, userId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to add tissue.", ex); return 0; }
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
}
