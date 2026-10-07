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

    /// <summary>
    /// Atomically reads and advances a type's "next histology ref" counter in a single
    /// <c>UPDATE ... OUTPUT</c> statement, returning the claimed (pre-increment) value, or
    /// <see langword="null"/> if the type doesn't exist or the counter is already at/past
    /// <paramref name="upperBoundExclusive"/> (checked in the same statement, so a blocked claim
    /// never advances the counter). The row lock SQL Server holds for the statement's duration
    /// means two concurrent callers can never claim the same value — unlike
    /// a separate read (<see cref="GetCountersAsync"/>) followed by a write
    /// (<see cref="UpdateCounterAsync"/>), which races.
    /// </summary>
    Task<string?> ClaimNextCounterAsync(int histologyType, int upperBoundExclusive, CancellationToken ct = default);

    /// <summary>
    /// Atomically claims (marks used) and returns one pre-booked-but-unused ref of the given
    /// type, or <see langword="null"/> if none remain. Uses <c>UPDLOCK, ROWLOCK, READPAST</c> so
    /// concurrent callers each claim a different row instead of racing to read-then-update the
    /// same one — unlike <see cref="GetUnusedRefsAsync"/> followed by a separate "mark used" write,
    /// which two callers could both read before either write landed.
    /// </summary>
    Task<string?> ClaimUnusedRefAsync(int histologyType, CancellationToken ct = default);
}
