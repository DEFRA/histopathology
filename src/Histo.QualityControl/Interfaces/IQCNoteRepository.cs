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
    /// Returns the QC notes for a given batch submission.
    /// Maps to <c>GetQCNotes</c> stored procedure.
    /// </summary>
    Task<IReadOnlyList<QCNote>> GetBySubmissionAsync(int submissionId, CancellationToken ct = default);

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
}
