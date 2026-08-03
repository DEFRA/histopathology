using Histo.Histology.Models;

namespace Histo.Histology.Interfaces;

/// <summary>
/// Data access contract for block records.
///
/// Legacy source: HistopathologyLib/clsBlock.vb — database persistence methods
/// translated to async Dapper pattern. In-memory DataTable manipulation methods
/// are implemented as pure helpers in <see cref="Histo.Core.Domain.BlockHelpers"/>.
/// </summary>
public interface IBlockRepository
{
    /// <summary>
    /// Returns all blocks for a batch.
    /// Maps to <c>GetBatchBlocksByID</c> stored procedure (table index 6 in the DataSet).
    /// </summary>
    Task<IReadOnlyList<Block>> GetByBatchAsync(int batchId, CancellationToken ct = default);

    /// <summary>
    /// Returns pre-booked (unassigned) blocks for an animal.
    /// Maps to <c>GetPreBookedBlocksByAnimalID</c> stored procedure.
    /// </summary>
    Task<IReadOnlyList<Block>> GetPreBookedByAnimalAsync(int animalId, CancellationToken ct = default);

    /// <summary>
    /// Saves (inserts or updates) a block record.
    /// Maps to <c>AddBlock</c> / <c>EditBlock</c> stored procedures.
    /// Returns the new block ID on insert; returns the existing ID on update.
    /// </summary>
    Task<int> SaveAsync(Block block, int userId, CancellationToken ct = default);

    /// <summary>
    /// Marks a block as deleted.
    /// Maps to <c>DeleteBlock</c> stored procedure.
    /// </summary>
    Task DeleteAsync(int blockId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Books a block reference for pre-cassetting.
    /// Maps to <c>BookBlockRef</c> stored procedure.
    /// </summary>
    Task BookRefAsync(int blockId, string blockRef, int userId, CancellationToken ct = default);

    // -----------------------------------------------------------------------
    // Search (read-only)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the used block refs (with status) for a histology ref.
    /// Maps to <c>GetBlocksForHistoRef</c>. Legacy source: SearchBlockRefs.aspx.
    /// </summary>
    Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsByHistologyRefAsync(string histologyRef, CancellationToken ct = default);

    /// <summary>
    /// Returns the used block refs (with status) for a sender ref.
    /// Maps to <c>GetBlocksForSenderRef</c>. Legacy source: SearchBlockRefs.aspx.
    /// </summary>
    Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsBySenderRefAsync(string senderRef, CancellationToken ct = default);

    /// <summary>
    /// Returns archived block records matching the given (optional) filters.
    /// Maps to <c>GetAnimalBlockArchiveInformation</c>. Legacy source: SearchArchiveLocation.aspx (Block Archive mode).
    /// </summary>
    Task<IReadOnlyList<BlockArchiveInfo>> GetBlockArchiveAsync(
        string? senderRef, string? histologyRef, string? blockRef, string? archiveLocation, CancellationToken ct = default);

    /// <summary>
    /// Returns archived slide records matching the given (optional) filters.
    /// Maps to <c>GetAnimalStainArchiveInformation</c>. Legacy source: SearchArchiveLocation.aspx (Slide Archive mode).
    ///
    /// SIMPLIFIED: the legacy method (<c>clsAnimal.GetAnimalSlideArchiveInformation</c>)
    /// additionally merges in <c>GetAnimalBatches</c> and per-batch-type data — that
    /// merge is not reproduced. See the search module report for details.
    /// </summary>
    Task<IReadOnlyList<SlideArchiveInfo>> GetSlideArchiveAsync(
        string? senderRef, string? histologyRef, string? archiveLocation, CancellationToken ct = default);
}
