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
    /// Maps to <c>GetBatchBlockDetails</c> stored procedure (@ID = BatchID) — confirmed against
    /// the database directly; the similarly-named <c>GetBatchBlocksByID</c> is a different,
    /// 10-result-set procedure used by <see cref="Histo.Histology.Repositories.BlockTestRepository"/>.
    /// </summary>
    Task<IReadOnlyList<Block>> GetByBatchAsync(int batchId, CancellationToken ct = default);

    /// <summary>
    /// Returns pre-booked (unassigned) blocks for an animal.
    /// Maps to <c>GetAnimalPreBookedBlocks</c> stored procedure (@AnimalID) — confirmed against
    /// the database directly.
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
    /// Creates a new pre-booked block placeholder for an animal, ahead of any batch existing.
    /// Maps to <c>AddBlock</c> with <c>BatchID = NULL</c> and <c>Status = BlockStatus.PreBooked</c> —
    /// confirmed against the database directly; there is no separate <c>BookBlockRef</c> stored
    /// procedure (that name does not exist in the database).
    /// Legacy source: clsBlock.vb::CreatePreBookedBlock.
    /// </summary>
    Task CreatePreBookedBlockAsync(int animalId, string blockRef, CancellationToken ct = default);

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
    /// Maps to <c>clsAnimal.GetAnimalSlideArchiveInformation</c>. Legacy source: SearchArchiveLocation.aspx
    /// (Slide Archive mode) — fans out across <c>GetAnimalStainArchiveInformation</c>,
    /// <c>GetAnimalBatches</c> + per-batch <c>GetAnimalAntibodiesArchiveInformation</c>, and
    /// <c>GetAnimalHistologyArchiveInformation</c> (excluding rows already covered by the Stain/
    /// Antibodies calls), matching the legacy merge exactly.
    /// </summary>
    Task<IReadOnlyList<SlideArchiveInfo>> GetSlideArchiveAsync(
        string? senderRef, string? histologyRef, string? archiveLocation, CancellationToken ct = default);
}
