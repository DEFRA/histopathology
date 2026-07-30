using Histo.Histology.Models;

namespace Histo.Histology.Interfaces;

/// <summary>
/// Data access contract for histology reference records.
///
/// Legacy source: HistopathologyLib/clsHistology.vb — database SP calls and
/// in-memory DataTable operations translated to async repository pattern.
/// Pure in-memory operations (AddUsedHistologyRef, FindUnusedHistologyRef, etc.)
/// are represented as service-layer helpers and do not require a repository method.
/// </summary>
public interface IHistologyRepository
{
    /// <summary>
    /// Returns all histology refs available for booking (not yet assigned).
    /// Maps to <c>GetUnusedHistologyRefs</c> stored procedure.
    /// </summary>
    Task<IReadOnlyList<HistologyRef>> GetUnusedRefsAsync(int histologyType, CancellationToken ct = default);

    /// <summary>
    /// Returns the histology refs already booked/used for a batch.
    /// Maps to the HISTOLOGY_REFS table (index 12) returned by <c>GetBatchBlocksByID</c>.
    /// </summary>
    Task<IReadOnlyList<HistologyRef>> GetUsedRefsByBatchAsync(int batchId, CancellationToken ct = default);

    /// <summary>
    /// Books (assigns) a histology reference to an animal record.
    /// Maps to <c>BookHistologyRef</c> stored procedure.
    /// </summary>
    Task BookRefAsync(string histologyRef, int animalId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Updates a histology reference record.
    /// Maps to <c>EditHistologyRef</c> stored procedure.
    /// </summary>
    Task UpdateRefAsync(string histologyRef, int histologyType, int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns histology refs that were booked but not used (for search/reporting).
    /// Maps to <c>GetUnUsedBookedHistologyRefs</c> stored procedure.
    /// </summary>
    Task<IReadOnlyList<HistologyRef>> GetUnusedBookedRefsAsync(CancellationToken ct = default);
}
