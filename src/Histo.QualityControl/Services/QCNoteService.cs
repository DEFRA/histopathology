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

    /// <summary>Matches the <c>QCNotes.QCText</c> / <c>EditQCNote @QCText</c> size.</summary>
    public const int MaxTextLength = 4000;

    /// <inheritdoc/>
    public async Task<QCNoteAppendResult> AppendAsync(int qcNoteId, IReadOnlyList<string> lines, int userId, CancellationToken ct = default)
    {
        var note = await GetByIdAsync(qcNoteId, ct);
        if (note is null) return QCNoteAppendResult.NotFound;

        // A never-edited note's Text is the generated test table, so appending persists that table too.
        var existing = (note.Text ?? string.Empty).TrimEnd();
        var toAdd = lines
            .Where(l => !string.IsNullOrWhiteSpace(l) && !existing.Contains(l, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (toAdd.Count == 0) return QCNoteAppendResult.NothingToAppend;

        var text = existing.Length == 0
            ? string.Join(Environment.NewLine, toAdd)
            : existing + Environment.NewLine + string.Join(Environment.NewLine, toAdd);
        if (text.Length > MaxTextLength) return QCNoteAppendResult.TooLong;

        try
        {
            await _repo.UpdateAsync(qcNoteId, text, note.RowStamp ?? [], userId, ct);
            return QCNoteAppendResult.Appended;
        }
        catch (QCNoteConcurrencyException)
        {
            return QCNoteAppendResult.Concurrency;
        }
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
