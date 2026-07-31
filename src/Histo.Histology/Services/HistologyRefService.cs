using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Infrastructure;

namespace Histo.Histology.Services;

/// <summary>
/// Application service for histology reference booking and lookup.
///
/// Replaces the database-persistence methods of legacy <c>clsHistology.vb</c>.
/// The in-memory DataTable manipulation methods (AddUsedHistologyRef,
/// FindUnusedHistologyRef, etc.) are now implemented as simple LINQ queries
/// over the collections returned by this service and do not require a service method.
/// </summary>
public sealed class HistologyRefService
{
    private readonly IHistologyRepository _repo;
    private readonly IAppLogger _logger;

    public HistologyRefService(IHistologyRepository repo, IAppLogger logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>Returns histology refs available for booking for the given type.</summary>
    public async Task<IReadOnlyList<HistologyRef>> GetUnusedRefsAsync(int histologyType, CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetUnusedRefsAsync(histologyType, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve unused histology refs.", ex);
            return [];
        }
    }

    /// <summary>Returns the histology refs already used/booked for a batch.</summary>
    public async Task<IReadOnlyList<HistologyRef>> GetUsedRefsByBatchAsync(int batchId, CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetUsedRefsByBatchAsync(batchId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve used histology refs for batch {BatchId}.", ex, batchId);
            return [];
        }
    }

    /// <summary>
    /// Books a histology reference to an animal.
    /// Returns <see langword="true"/> on success.
    /// </summary>
    public async Task<bool> BookRefAsync(string histologyRef, int animalId, int userId, CancellationToken ct = default)
    {
        try
        {
            await _repo.BookRefAsync(histologyRef, animalId, userId, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to book histology ref {HistologyRef}.", ex, histologyRef);
            return false;
        }
    }

    /// <summary>Returns booked histology refs that were never used.</summary>
    public async Task<IReadOnlyList<HistologyRef>> GetUnusedBookedRefsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetUnusedBookedRefsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve unused booked histology refs.", ex);
            return [];
        }
    }

    /// <summary>
    /// Returns every unused histology ref, with no type filter.
    /// Used by SearchUnUsedHistologyRefs.aspx.
    /// </summary>
    public async Task<IReadOnlyList<HistologyRef>> GetAllUnusedRefsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetAllUnusedRefsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve all unused histology refs.", ex);
            return [];
        }
    }

    /// <summary>
    /// Updates a histology reference record for the given type.
    /// Replaces the legacy <c>clsHistology.UpdateHistologyRefs</c> call.
    /// Returns <see langword="true"/> on success.
    /// </summary>
    public async Task<bool> UpdateRefAsync(string histologyRef, int histologyType, int userId, CancellationToken ct = default)
    {
        try
        {
            await _repo.UpdateRefAsync(histologyRef, histologyType, userId, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update histology ref {HistologyRef}.", ex, histologyRef);
            return false;
        }
    }
}
