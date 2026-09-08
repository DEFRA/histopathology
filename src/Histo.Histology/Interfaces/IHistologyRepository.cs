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
    /// Returns histology refs that were booked but not used (for search/reporting).
    /// Maps to <c>GetUnUsedBookedHistologyRefs</c> stored procedure.
    /// </summary>
    Task<IReadOnlyList<HistologyRef>> GetUnusedBookedRefsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns every unused histology ref, with no type filter.
    /// Maps to the <c>GetUnusedHistologyRefs</c> stored procedure called with no
    /// parameters. Legacy source: HistopathologyLib/clsHistology.vb —
    /// <c>GetUnUsedHistologyRefsTable</c>. Used by SearchUnUsedHistologyRefs.aspx.
    /// </summary>
    Task<IReadOnlyList<HistologyRef>> GetAllUnusedRefsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the current "next histology ref" counter for every histology type.
    /// Maps to the (parameterless) <c>GetHistologyRefs</c> stored procedure — confirmed
    /// against the database directly. Legacy source: clsHistology.vb::GetHistologyRefsTable.
    /// </summary>
    Task<IReadOnlyList<HistologyRefCounter>> GetCountersAsync(CancellationToken ct = default);

    /// <summary>
    /// Overwrites a type's "next histology ref" counter. Maps to <c>EditHistologyRef</c>
    /// stored procedure (real params confirmed against the database: <c>@Type</c>,
    /// <c>@NextHistologyRef</c>, <c>@RowStamp</c> — there is no <c>@UserID</c> param).
    /// Legacy source: clsHistology.vb::UpdateHistologyRefRow.
    /// </summary>
    Task UpdateCounterAsync(int histologyType, string newNextHistologyRef, byte[]? rowStamp, CancellationToken ct = default);
}
