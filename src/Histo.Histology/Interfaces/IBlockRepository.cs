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
}
