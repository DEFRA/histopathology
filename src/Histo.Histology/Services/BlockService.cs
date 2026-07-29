using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Infrastructure;

namespace Histo.Histology.Services;

/// <summary>
/// Application service for block management.
///
/// Replaces the database-persistence methods of legacy <c>clsBlock.vb</c>.
/// Pure in-memory ordering logic delegates to <see cref="BlockHelpers.ComputeNextOrder"/>.
/// </summary>
public sealed class BlockService
{
    private readonly IBlockRepository _repo;
    private readonly IAppLogger _logger;

    public BlockService(IBlockRepository repo, IAppLogger logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>Returns all blocks for a batch.</summary>
    public async Task<IReadOnlyList<Block>> GetByBatchAsync(int batchId, CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetByBatchAsync(batchId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve blocks for batch {BatchId}.", ex, batchId);
            return [];
        }
    }

    /// <summary>Returns pre-booked blocks for an animal.</summary>
    public async Task<IReadOnlyList<Block>> GetPreBookedByAnimalAsync(int animalId, CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetPreBookedByAnimalAsync(animalId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to retrieve pre-booked blocks for animal {AnimalId}.", ex, animalId);
            return [];
        }
    }

    /// <summary>
    /// Creates a new block with the next available order value.
    ///
    /// <paramref name="existingOrders"/> should be the Order values of all existing
    /// blocks in the batch — used by <see cref="BlockHelpers.ComputeNextOrder"/> to
    /// replicate the legacy <c>GetOrder()</c> logic.
    /// </summary>
    public async Task<int> AddBlockAsync(
        int batchId,
        int animalId,
        string blockRef,
        IEnumerable<int> existingOrders,
        int userId,
        string? customerRef = null,
        string? comment = null,
        bool repeatBlock = false,
        CancellationToken ct = default)
    {
        var order = BlockHelpers.ComputeNextOrder(existingOrders);
        var block = new Block
        {
            BatchID     = batchId,
            AnimalID    = animalId,
            BlockRef    = blockRef,
            CustomerRef = customerRef,
            Comment     = comment,
            RepeatBlock = repeatBlock,
            Status      = BlockStatus.Used,
            Order       = order,
        };

        try
        {
            return await _repo.SaveAsync(block, userId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to add block for batch {BatchId}.", ex, batchId);
            return 0;
        }
    }

    /// <summary>Saves changes to an existing block.</summary>
    public async Task<bool> UpdateBlockAsync(Block block, int userId, CancellationToken ct = default)
    {
        try
        {
            await _repo.SaveAsync(block, userId, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to update block {BlockId}.", ex, block.ID);
            return false;
        }
    }

    /// <summary>Deletes a block.</summary>
    public async Task<bool> DeleteBlockAsync(int blockId, int userId, CancellationToken ct = default)
    {
        try
        {
            await _repo.DeleteAsync(blockId, userId, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete block {BlockId}.", ex, blockId);
            return false;
        }
    }
}
