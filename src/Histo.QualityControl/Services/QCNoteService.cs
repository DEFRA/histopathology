using Histo.Infrastructure;
using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Models;

namespace Histo.QualityControl.Services;

/// <summary>
/// Application service for Quality Control note management.
///
/// Replaces direct calls to <c>clsQCNote</c> from ASPX code-behind files.
/// Surfaces <see cref="QCNoteConcurrencyException"/> for callers to handle
/// the optimistic-concurrency conflict in the UI layer.
/// </summary>
public sealed class QCNoteService : IQCNoteService
{
    private readonly IQCNoteRepository _repo;
    private readonly IAppLogger _logger;

    public QCNoteService(IQCNoteRepository repo, IAppLogger logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>Returns a single QC note, or <see langword="null"/> if not found.</summary>
    public async Task<QCNote?> GetByIdAsync(int qcNoteId, CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetByIdAsync(qcNoteId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve QC note {QcNoteId}.", ex, qcNoteId);
            return null;
        }
    }

    /// <summary>
    /// Updates a QC note. Throws <see cref="QCNoteConcurrencyException"/> when
    /// a concurrent modification is detected.
    /// </summary>
    public async Task UpdateAsync(int qcNoteId, string text, byte[] rowStamp, int userId, CancellationToken ct = default)
    {
        // Let QCNoteConcurrencyException propagate — the UI layer must handle it
        await _repo.UpdateAsync(qcNoteId, text, rowStamp, userId, ct);
    }

    /// <summary>Creates a new QC note and returns the new ID.</summary>
    public async Task<int> AddAsync(int submissionId, int createdByUserId, CancellationToken ct = default)
    {
        try
        {
            return await _repo.AddAsync(submissionId, createdByUserId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to add QC note for submission {SubmissionId}.", ex, submissionId);
            return 0;
        }
    }

    /// <summary>
    /// Returns all QC notes system-wide. Used when no batch is selected in session,
    /// matching legacy <c>QCNotes.aspx</c> global load behaviour.
    /// </summary>
    public async Task<IReadOnlyList<QCNote>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetAllAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve all QC notes.", ex);
            return [];
        }
    }
}
