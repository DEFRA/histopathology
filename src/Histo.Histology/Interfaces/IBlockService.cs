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

    /// <summary>
    /// Creates a new pre-booked block placeholder for an animal, ahead of any batch existing.
    /// Returns <see langword="true"/> on success. Legacy source: clsBlock.vb::CreatePreBookedBlock.
    /// </summary>
    Task<bool> CreatePreBookedBlockAsync(int animalId, string blockRef, CancellationToken ct = default);

    /// <summary>
    /// Books a range of block refs for one sender ref atomically (duplicate check, sample
    /// resolution and inserts on a single locked transaction). Returns <c>null</c> when the
    /// operation failed, so the caller does not report a non-booking as success.
    /// </summary>
    Task<IReadOnlyList<PreBookedBlockResult>?> BookPreBookedBlocksAsync(
        string senderRef, IReadOnlyList<int> blockRefs, CancellationToken ct = default);

    /// <summary>
    /// Claims an existing pre-booked placeholder block (<paramref name="preBooked"/>) into a real
    /// batch, updating it in place rather than inserting a new row — so the placeholder is retired
    /// (Status PreBooked -&gt; PreBookedUsed) and never reappears in <see cref="GetPreBookedByAnimalAsync"/>.
    /// Legacy source: clsBlock.vb::NewBlock/GetPreBookedBlock + EditPreBookedBlockStatus/SetBatchID.
    /// </summary>
    Task<bool> ClaimPreBookedBlockAsync(Block preBooked, int batchId, IEnumerable<int> existingOrders, int userId,
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
