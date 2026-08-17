using Histo.Submissions.Models;

namespace Histo.Submissions.Interfaces;

/// <summary>
/// Public service contract for batch submission, animal, and tissue management — the module boundary exposed to Histo.Web.
/// Concrete implementation: <see cref="Histo.Submissions.Services.SubmissionService"/>.
/// </summary>
public interface ISubmissionService
{
    // Submissions
    Task<IReadOnlyList<BatchSubmission>> GetSubmissionsByBatchAsync(int batchId, CancellationToken ct = default);
    Task<int> AddSubmissionAsync(BatchSubmission submission, int userId, CancellationToken ct = default);
    Task<int> CopySubmissionAsync(BatchSubmission source, int newBatchId, int userId, CancellationToken ct = default);

    // Animals
    Task<IReadOnlyList<Animal>> GetAnimalsByBatchAsync(int batchId, CancellationToken ct = default);
    Task<int> AddAnimalAsync(int batchSubmissionId, string senderRef, bool isNeuropath, int userId, string? pmDate = null, bool pmDateSet = false, CancellationToken ct = default);
    Task<int> CopyAnimalAsync(Animal source, int newBatchSubmissionId, string newSenderRef, int userId, CancellationToken ct = default);
    Task<bool> UpdateAnimalAsync(Animal animal, int userId, CancellationToken ct = default);
    Task<bool> DeleteAnimalAsync(int animalId, int userId, CancellationToken ct = default);

    /// <summary>Renames the Sender Ref. Throws <see cref="AnimalRefUpdateException"/> on conflict.</summary>
    Task UpdateAnimalSenderRefAsync(string senderRef, string newSenderRef, int userId, CancellationToken ct = default);

    /// <summary>Renames (or clears) the Histology Ref. Throws <see cref="AnimalRefUpdateException"/> on conflict.</summary>
    Task UpdateAnimalHistologyRefAsync(string senderRef, string? newHistologyRef, int userId, CancellationToken ct = default);

    // Tissues
    Task<IReadOnlyList<Tissue>> GetTissuesBySubmissionAsync(int submissionId, CancellationToken ct = default);
    Task<IReadOnlyList<Tissue>> GetTissuesByBlockAsync(int blockId, CancellationToken ct = default);
    Task<int> AddTissueAsync(Tissue tissue, int userId, CancellationToken ct = default);
    Task<int> CopyTissueAsync(Tissue source, int newOwnerId, int userId, CancellationToken ct = default);
    Task<bool> UpdateTissueAsync(Tissue tissue, int userId, CancellationToken ct = default);
    Task<bool> DeleteTissueAsync(int tissueId, TissueOwner owner, int userId, CancellationToken ct = default);

    // Search
    Task<IReadOnlyList<PmDateSearchResult>> GetByPmDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task<IReadOnlyList<SenderSearchResult>> GetAnimalsBySenderRefAsync(string senderRef, CancellationToken ct = default);
    Task<IReadOnlyList<TissueArchiveInfo>> GetTissueArchiveAsync(string? senderRef, string? histologyRef, string? archiveLocation, string? tissueCode, CancellationToken ct = default);
    Task<IReadOnlyList<ImportedDataRow>> GetImportedDataAsync(string? selectedTable, CancellationToken ct = default);

    /// <summary>Standalone ViewSamples search — "Tissue Information" mode. See <see cref="Histo.Submissions.Interfaces.ISubmissionRepository.GetAnimalTissuesAsync"/>.</summary>
    Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalTissuesAsync(string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default);

    /// <summary>Standalone ViewSamples search — "Block Information" mode. See <see cref="Histo.Submissions.Interfaces.ISubmissionRepository.GetAnimalBlockTissuesAsync"/>.</summary>
    Task<IReadOnlyList<AnimalTissueSearchResult>> GetAnimalBlockTissuesAsync(string? senderRef, string? histologyRef, string? tissueCode, string? projectDesc, CancellationToken ct = default);
}
