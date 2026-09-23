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

    /// <summary>Returns the current "next histology ref" counter for every histology type.</summary>
    Task<IReadOnlyList<HistologyRefCounter>> GetCountersAsync(CancellationToken ct = default);

    /// <summary>
    /// Books (reserves) a contiguous range of <paramref name="numberToBook"/> histology refs for
    /// a type by incrementing its counter, enforcing legacy's per-type upper bound.
    /// Legacy source: BookHistologyRef.aspx.vb::UpdateHistologyRefs.
    /// </summary>
    Task<HistologyBookingResult> BookCounterRangeAsync(int histologyType, int numberToBook, CancellationToken ct = default);

    /// <summary>
    /// Directly overwrites a type's "next histology ref" counter to an absolute value,
    /// looking up the current RowStamp itself. Returns <see langword="false"/> on failure
    /// (unknown type, or a concurrency conflict).
    /// </summary>
    Task<bool> SetCounterAsync(int histologyType, string newNextHistologyRef, CancellationToken ct = default);
}
