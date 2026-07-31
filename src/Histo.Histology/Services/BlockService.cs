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

    /// <summary>
    /// Creates a copy of an existing block on a target animal, computing the next
    /// free block reference for that animal from <paramref name="existingBlockRefs"/>.
    /// Used by the "Copy blocks" and "Copy samples" workflows.
    ///
    /// Legacy source: HistopathologyLib/clsBlock.vb — <c>CopyBlock()</c>
    /// (tissue/histology/stain/antibody copying is orchestrated by the caller,
    /// since those records live in the Submissions module).
    /// </summary>
    public async Task<int> CopyBlockAsync(
        Block source,
        int newBatchId,
        int newAnimalId,
        IEnumerable<string> existingBlockRefs,
        IEnumerable<int> existingOrders,
        int userId,
        CancellationToken ct = default)
    {
        var newBlockRef = BlockHelpers.ComputeNextBlockRef(existingBlockRefs);
        return await AddBlockAsync(
            newBatchId, newAnimalId, newBlockRef, existingOrders, userId,
            source.CustomerRef, source.Comment, source.RepeatBlock, ct);
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

    // -----------------------------------------------------------------------
    // Search (read-only)
    // -----------------------------------------------------------------------

    /// <summary>Returns the used block refs (with status) for a histology ref.</summary>
    public async Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsByHistologyRefAsync(string histologyRef, CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetUsedBlockRefsByHistologyRefAsync(histologyRef, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to search used block refs by histology ref {HistologyRef}.", ex, histologyRef);
            return [];
        }
    }

    /// <summary>Returns the used block refs (with status) for a sender ref.</summary>
    public async Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsBySenderRefAsync(string senderRef, CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetUsedBlockRefsBySenderRefAsync(senderRef, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to search used block refs by sender ref {SenderRef}.", ex, senderRef);
            return [];
        }
    }

    /// <summary>Returns archived block records matching the given (optional) filters.</summary>
    public async Task<IReadOnlyList<BlockArchiveInfo>> GetBlockArchiveAsync(
        string? senderRef, string? histologyRef, string? blockRef, string? archiveLocation, CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetBlockArchiveAsync(senderRef, histologyRef, blockRef, archiveLocation, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to search block archive information.", ex);
            return [];
        }
    }

    /// <summary>Returns archived slide records matching the given (optional) filters.</summary>
    public async Task<IReadOnlyList<SlideArchiveInfo>> GetSlideArchiveAsync(
        string? senderRef, string? histologyRef, string? archiveLocation, CancellationToken ct = default)
    {
        try
        {
            return await _repo.GetSlideArchiveAsync(senderRef, histologyRef, archiveLocation, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to search slide archive information.", ex);
            return [];
        }
    }
}
