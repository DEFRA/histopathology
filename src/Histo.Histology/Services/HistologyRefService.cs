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
public sealed class HistologyRefService : IHistologyRefService
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
    public async Task<bool> SetCounterAsync(int histologyType, string newNextHistologyRef, CancellationToken ct = default)
    {
        try
        {
            var counters = await _repo.GetCountersAsync(ct);
            var row = counters.FirstOrDefault(c => c.Type == histologyType);
            if (row is null) return false;

            await _repo.UpdateCounterAsync(histologyType, newNextHistologyRef, row.RowStamp, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update histology ref counter for type {HistologyType}.", ex, histologyType);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HistologyRefCounter>> GetCountersAsync(CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetCountersAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve histology ref counters.", ex);
            return [];
        }
    }

    /// <summary>
    /// Books (reserves) a contiguous range of <paramref name="numberToBook"/> histology refs for
    /// <paramref name="histologyType"/> by incrementing its "next histology ref" counter, enforcing
    /// legacy's per-type upper bound.
    ///
    /// Legacy source: BookHistologyRef.aspx.vb::UpdateHistologyRefs.
    /// </summary>
    public async Task<HistologyBookingResult> BookCounterRangeAsync(int histologyType, int numberToBook, CancellationToken ct = default)
    {
        try
        {
            var counters = await _repo.GetCountersAsync(ct);
            var row = counters.FirstOrDefault(c => c.Type == histologyType);
            if (row is null)
                return new HistologyBookingResult { Success = false, Error = "The selected Histology Ref Type could not be found." };

            if (!int.TryParse(row.NextHistologyRef, out var current))
                return new HistologyBookingResult { Success = false, Error = "The current Histology Ref counter is invalid." };

            var next = current + numberToBook;

            var (upperBound, typeName) = histologyType switch
            {
                HistologyRefTypeCode.Neuropath      => (20000, "neuropath"),
                HistologyRefTypeCode.AbattoirSurvey => (30000, "abattoir survey"),
                HistologyRefTypeCode.TBDiagnostic   => (40000, "TB diagnostic"),
                HistologyRefTypeCode.GeneralPool    => (60000, "general pool"),
                HistologyRefTypeCode.MouseProjects  => (90000, "mouse project"),
                _ => (0, string.Empty),
            };
            if (upperBound > 0 && next >= upperBound)
            {
                return new HistologyBookingResult
                {
                    Success = false,
                    Error = $"Cannot book the required histology numbers as the maximum {typeName} histology number is {upperBound - 1}.",
                };
            }

            await _repo.UpdateCounterAsync(histologyType, next.ToString(), row.RowStamp, ct);
            return new HistologyBookingResult { Success = true, FirstBooked = current, LastBooked = next - 1 };
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to book histology ref range for type {HistologyType}.", ex, histologyType);
            return new HistologyBookingResult { Success = false, Error = "The database has not been updated because an error occurred. Please try again." };
        }
    }
}
