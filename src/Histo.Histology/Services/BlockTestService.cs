using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Infrastructure;

namespace Histo.Histology.Services;

/// <summary>
/// Application service for the quality-control / dispatch test worklist.
///
/// Replaces the per-test data-entry portion of legacy <c>QualityData.aspx.vb</c>.
/// See <see cref="Histo.Histology.Models.BlockTest"/> for scope notes.
/// </summary>
public sealed class BlockTestService : IBlockTestService
{
    private readonly IBlockTestRepository _repo;
    private readonly IAppLogger _logger;

    public BlockTestService(IBlockTestRepository repo, IAppLogger logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>Returns every test for a batch's blocks.</summary>
    public async Task<IReadOnlyList<BlockTest>> GetByBatchAsync(int batchId, CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetByBatchAsync(batchId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve tests for batch {BatchId}.", ex, batchId);
            return [];
        }
    }

    /// <summary>Returns a single test by ID, or <see langword="null"/> if not found.</summary>
    public async Task<BlockTest?> GetByIdAsync(int batchId, int testId, CancellationToken ct = default)
    {
        var tests = await GetByBatchAsync(batchId, ct);
        return tests.FirstOrDefault(t => t.ID == testId);
    }

    /// <summary>
    /// Updates a test record. Throws <see cref="BlockTestConcurrencyException"/> when
    /// a concurrent modification is detected.
    /// </summary>
    public async Task UpdateAsync(BlockTest test, int userId, CancellationToken ct = default)
    {
        // Let BlockTestConcurrencyException propagate — the UI layer must handle it
        await _repo.UpdateAsync(test, userId, ct);
    }

    /// <inheritdoc/>
    public async Task SaveTCCodesAsync(
        int batchId, int testId, string testType,
        IReadOnlyList<TcCode> existing, IReadOnlyList<string> selected,
        int userId, CancellationToken ct = default)
    {
        try
        {
            await _repo.SaveTCCodesAsync(batchId, testId, testType, existing, selected, userId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save TC codes for test {TestId}.", ex, testId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SaveTestSelectionsAsync(int batchId, int blockId,
        IReadOnlyList<string> histologyCodes, IReadOnlyList<string> antibodyCodes, IReadOnlyList<string> stainCodes,
        int userId, CancellationToken ct = default)
    {
        try
        {
            await _repo.SaveTestSelectionsAsync(batchId, blockId, histologyCodes, antibodyCodes, stainCodes, userId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save test selections for block {BlockId}.", ex, blockId);
        }
    }
}
