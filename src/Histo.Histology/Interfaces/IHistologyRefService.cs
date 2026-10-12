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

    /// <summary>
    /// Draws the next available ref for a type, formatted as <c>yy/NNNNN</c> with the CURRENT
    /// two-digit year. Takes the lowest-numbered current-year ref from the pre-booked-but-unused
    /// pool (<see cref="GetUnusedRefsAsync"/>) first, then falls back to minting from the type's
    /// counter and advancing it. Returns <see langword="null"/> when the type has no range of its
    /// own (e.g. "use pg number") or no counter row.
    ///
    /// Legacy source: <c>clsHistology.GetNextAvailableHistologyRef</c> (SP <c>GetNextHistologyRef</c>).
    /// </summary>
    Task<string?> GetNextAvailableRefAsync(int histologyType, CancellationToken ct = default);
}
