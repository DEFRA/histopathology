using Histo.Core.Domain;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;

namespace Histo.Submissions.Services;

/// <summary>
/// Application service for batch (submission header) management.
///
/// Replaces the database-persistence methods of legacy <c>clsBatch.vb</c>.
/// Batch status constants remain in <see cref="BatchStatus"/> (Histo.Core).
/// </summary>
public sealed class BatchService
{
    private readonly IBatchRepository _batches;
    private readonly IAppLogger _logger;

    public BatchService(IBatchRepository batches, IAppLogger logger)
    {
        _batches = batches;
        _logger  = logger;
    }

    /// <summary>Returns a batch by ID, or <see langword="null"/> if not found.</summary>
    public async Task<Batch?> GetByIdAsync(int batchId, CancellationToken ct = default)
    {
        try { return await _batches.GetByIdAsync(batchId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to get batch {BatchId}.", ex, batchId); return null; }
    }

    /// <summary>Returns all received batches.</summary>
    public async Task<IReadOnlyList<Batch>> GetReceivedAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetReceivedAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get received batches.", ex); return []; }
    }

    /// <summary>Returns all in-progress batches.</summary>
    public async Task<IReadOnlyList<Batch>> GetInProgressAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetInProgressAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get in-progress batches.", ex); return []; }
    }

    /// <summary>Returns all batches on hold.</summary>
    public async Task<IReadOnlyList<Batch>> GetOnHoldAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetOnHoldAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get on-hold batches.", ex); return []; }
    }

    /// <summary>Returns batches not yet received.</summary>
    public async Task<IReadOnlyList<Batch>> GetNotReceivedAsync(CancellationToken ct = default)
    {
        try { return await _batches.GetNotReceivedAsync(ct); }
        catch (Exception ex) { _logger.LogError("Failed to get not-received batches.", ex); return []; }
    }

    /// <summary>Creates a new batch and returns its ID.</summary>
    public async Task<int> AddAsync(Batch batch, int userId, CancellationToken ct = default)
    {
        try { return await _batches.AddAsync(batch, userId, ct); }
        catch (Exception ex) { _logger.LogError("Failed to add batch.", ex); return 0; }
    }

    /// <summary>
    /// Updates batch status. Throws <see cref="BatchConcurrencyException"/> on
    /// concurrent modification.
    /// </summary>
    public async Task<bool> UpdateStatusAsync(int batchId, string newStatus, byte[] rowStamp, int userId, CancellationToken ct = default)
    {
        // BatchConcurrencyException propagates — the UI must handle it
        await _batches.UpdateStatusAsync(batchId, newStatus, rowStamp, userId, ct);
        return true;
    }
}
