using Histo.QualityControl.Models;

namespace Histo.QualityControl.Interfaces;

/// <summary>
/// Data access contract for Quality Control notes.
///
/// Legacy source: HistopathologyLib/clsQCNote.vb — update and retrieval methods
/// translated to async Dapper pattern.
/// </summary>
public interface IQCNoteRepository
{
    /// <summary>
    /// Returns a single QC note by ID.
    /// Maps to <c>GetQCNote</c> stored procedure.
    /// </summary>
    Task<QCNote?> GetByIdAsync(int qcNoteId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing QC note text.
    /// Maps to <c>EditQCNote</c> stored procedure.
    ///
    /// Throws <see cref="QCNoteConcurrencyException"/> when the SP returns 1
    /// (concurrent modification detected via rowstamp comparison).
    /// </summary>
    Task UpdateAsync(int qcNoteId, string text, byte[] rowStamp, int userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new QC note record.
    /// Maps to <c>AddQCNote</c> stored procedure.
    /// Returns the new QC note ID.
    /// </summary>
    Task<int> AddAsync(int submissionId, int createdByUserId, CancellationToken ct = default);

    /// <summary>
    /// Returns the submission's QC note, creating it and linking <paramref name="testId"/> to it if none exists.
    /// The link is guarded by the test's RowStamp (no locks, as in legacy); a stale stamp throws <c>QCNoteLinkConflictException</c>.
    /// Two simultaneous first saves can still each create a note, as in legacy.
    /// </summary>
    Task<SubmissionQCNote> GetOrCreateForSubmissionAsync(int batchId, int testId, string testType, byte[] expectedRowStamp, int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns all QC notes system-wide (no batch filter).
    /// Maps to <c>GetBatchQCNotes</c> with no ID parameter — matches legacy <c>QCNotes.aspx</c>
    /// global load behaviour.
    /// </summary>
    Task<IReadOnlyList<QCNote>> GetAllAsync(CancellationToken ct = default);
}
