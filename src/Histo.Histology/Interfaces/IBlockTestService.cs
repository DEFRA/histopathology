using Histo.Histology.Models;

namespace Histo.Histology.Interfaces;

/// <summary>
/// Public service contract for the quality-control/dispatch test worklist — the module boundary exposed to Histo.Web.
/// Concrete implementation: <see cref="Histo.Histology.Services.BlockTestService"/>.
/// </summary>
public interface IBlockTestService
{
    Task<IReadOnlyList<BlockTest>> GetByBatchAsync(int batchId, CancellationToken ct = default);
    Task<BlockTest?> GetByIdAsync(int batchId, int testId, CancellationToken ct = default);

    /// <summary>Updates a test record. Throws <see cref="BlockTestConcurrencyException"/> on concurrent modification.</summary>
    Task UpdateAsync(BlockTest test, int userId, CancellationToken ct = default);
}
