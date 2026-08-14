using Histo.Histology.Models;

namespace Histo.Histology.Interfaces;

/// <summary>
/// Public service contract for block management — the module boundary exposed to Histo.Web.
/// Concrete implementation: <see cref="Histo.Histology.Services.BlockService"/>.
/// </summary>
public interface IBlockService
{
    Task<IReadOnlyList<Block>> GetByBatchAsync(int batchId, CancellationToken ct = default);
    Task<IReadOnlyList<Block>> GetPreBookedByAnimalAsync(int animalId, CancellationToken ct = default);

    Task<int> AddBlockAsync(int batchId, int animalId, string blockRef, IEnumerable<int> existingOrders, int userId,
        string? customerRef = null, string? comment = null, bool repeatBlock = false, CancellationToken ct = default);

    Task<bool> UpdateBlockAsync(Block block, int userId, CancellationToken ct = default);

    Task<int> CopyBlockAsync(Block source, int newBatchId, int newAnimalId,
        IEnumerable<string> existingBlockRefs, IEnumerable<int> existingOrders,
        int userId, CancellationToken ct = default);

    Task<bool> DeleteBlockAsync(int blockId, int userId, CancellationToken ct = default);

    // Search
    Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsByHistologyRefAsync(string histologyRef, CancellationToken ct = default);
    Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsBySenderRefAsync(string senderRef, CancellationToken ct = default);
    Task<IReadOnlyList<BlockArchiveInfo>> GetBlockArchiveAsync(string? senderRef, string? histologyRef, string? blockRef, string? archiveLocation, CancellationToken ct = default);
    Task<IReadOnlyList<SlideArchiveInfo>> GetSlideArchiveAsync(string? senderRef, string? histologyRef, string? archiveLocation, CancellationToken ct = default);
}
