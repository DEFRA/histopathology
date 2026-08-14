using Histo.QualityControl.Models;

namespace Histo.QualityControl.Interfaces;

/// <summary>
/// Public service contract for QC note management — the module boundary exposed to Histo.Web.
/// Concrete implementation: <see cref="Histo.QualityControl.Services.QCNoteService"/>.
/// </summary>
public interface IQCNoteService
{
    Task<IReadOnlyList<QCNote>> GetBySubmissionAsync(int submissionId, CancellationToken ct = default);
    Task<QCNote?> GetByIdAsync(int qcNoteId, CancellationToken ct = default);
    Task<IReadOnlyList<QCNote>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Creates a new QC note. Returns the new ID, or 0 on failure.</summary>
    Task<int> AddAsync(int submissionId, int createdByUserId, CancellationToken ct = default);

    /// <summary>Updates a QC note. Throws <see cref="QCNoteConcurrencyException"/> on concurrent modification.</summary>
    Task UpdateAsync(int qcNoteId, string text, byte[] rowStamp, int userId, CancellationToken ct = default);
}
