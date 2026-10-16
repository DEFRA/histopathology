using Histo.QualityControl.Models;

namespace Histo.QualityControl.Interfaces;

/// <summary>
/// Public service contract for QC note management — the module boundary exposed to Histo.Web.
/// Concrete implementation: <see cref="Histo.QualityControl.Services.QCNoteService"/>.
/// </summary>
public interface IQCNoteService
{
    Task<QCNote?> GetByIdAsync(int qcNoteId, CancellationToken ct = default);
    Task<IReadOnlyList<QCNote>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Creates a new QC note. Returns the new ID, or 0 on failure.</summary>
    Task<int> AddAsync(int submissionId, int createdByUserId, CancellationToken ct = default);

    /// <summary>
    /// Atomically finds or creates the submission's QC note, linking the test on create only if its RowStamp still matches.
    /// Throws <see cref="QCNoteLinkConflictException"/> (nothing created) when it doesn't; returns null on any other failure.
    /// </summary>
    Task<SubmissionQCNote?> GetOrCreateForSubmissionAsync(int batchId, int testId, string testType, byte[] expectedRowStamp, int userId, CancellationToken ct = default);

    /// <summary>Updates a QC note. Throws <see cref="QCNoteConcurrencyException"/> on concurrent modification.</summary>
    Task UpdateAsync(int qcNoteId, string text, byte[] rowStamp, int userId, CancellationToken ct = default);

    /// <summary>Appends each line not already in the note's text; never truncates (QCText is varchar(4000)).</summary>
    Task<QCNoteAppendResult> AppendAsync(int qcNoteId, IReadOnlyList<string> lines, int userId, CancellationToken ct = default);
}
