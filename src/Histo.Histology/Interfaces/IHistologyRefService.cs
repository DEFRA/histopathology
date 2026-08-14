using Histo.Histology.Models;

namespace Histo.Histology.Interfaces;

/// <summary>
/// Public service contract for histology reference booking — the module boundary exposed to Histo.Web.
/// Concrete implementation: <see cref="Histo.Histology.Services.HistologyRefService"/>.
/// </summary>
public interface IHistologyRefService
{
    Task<IReadOnlyList<HistologyRef>> GetUnusedRefsAsync(int histologyType, CancellationToken ct = default);
    Task<IReadOnlyList<HistologyRef>> GetUsedRefsByBatchAsync(int batchId, CancellationToken ct = default);
    Task<IReadOnlyList<HistologyRef>> GetUnusedBookedRefsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HistologyRef>> GetAllUnusedRefsAsync(CancellationToken ct = default);

    /// <summary>Books a histology reference to an animal. Returns <see langword="true"/> on success.</summary>
    Task<bool> BookRefAsync(string histologyRef, int animalId, int userId, CancellationToken ct = default);

    /// <summary>Updates a histology reference record. Returns <see langword="true"/> on success.</summary>
    Task<bool> UpdateRefAsync(string histologyRef, int histologyType, int userId, CancellationToken ct = default);
}
