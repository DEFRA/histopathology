using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for block lifecycle management (<see cref="IBlockService"/>) — the service
/// underneath <c>Blocks/BlockDetails</c>, <c>Blocks/CopyBlocks</c> and <c>Batches/BatchBlocks</c>.
///
/// <see cref="Histo.Web.Pages.Blocks.BlockDetailsModel"/> and <see cref="Histo.Web.Pages.Blocks.CopyBlocksModel"/>
/// each pull in 5-7 collaborating services (submissions, batches, lookups, block-tests, histology
/// refs) to render a single page, which makes a page-model-level chained harness disproportionately
/// expensive to build and maintain versus the value it adds. Block/order allocation and the
/// pre-booked-block claim lifecycle — the two areas called out in the source comments as the
/// trickiest legacy behaviour to get right (duplicate orders, re-claiming a retired placeholder) —
/// live entirely in <see cref="IBlockService"/> itself and are shared by every page that touches
/// blocks, so this suite targets the service contract directly via a stateful
/// <see cref="FakeBlockService"/>, the same pattern used for the Admin/User-maintenance chains.
/// </summary>
public class BlockLifecycleRegressionTests
{
    private readonly FakeBlockService _blocks = new();

    [Fact]
    public async Task AddBlockAsync_SequentialBlocksOnSameBatch_GetIncrementingOrderNoDuplicates()
    {
        const int batchId = 1, animalId = 10;

        var firstId = await _blocks.AddBlockAsync(batchId, animalId, "01", existingOrders: [], userId: 7);
        var existingAfterFirst = (await _blocks.GetByBatchAsync(batchId)).Select(b => b.Order);
        var secondId = await _blocks.AddBlockAsync(batchId, animalId, "02", existingAfterFirst, userId: 7);
        var existingAfterSecond = (await _blocks.GetByBatchAsync(batchId)).Select(b => b.Order);
        var thirdId = await _blocks.AddBlockAsync(batchId, animalId, "03", existingAfterSecond, userId: 7);

        var orders = (await _blocks.GetByBatchAsync(batchId)).ToDictionary(b => b.ID, b => b.Order);
        Assert.Equal(0, orders[firstId]);
        Assert.Equal(1, orders[secondId]);
        Assert.Equal(2, orders[thirdId]);
        Assert.Equal(3, orders.Values.Distinct().Count()); // no duplicate orders
    }

    [Fact]
    public async Task PreBookedBlock_CreateThenClaim_TransitionsToPreBookedUsedAndLeavesThePreBookedList()
    {
        const int animalId = 20, batchId = 2;

        Assert.True(await _blocks.CreatePreBookedBlockAsync(animalId, "05"));
        var preBooked = Assert.Single(await _blocks.GetPreBookedByAnimalAsync(animalId));
        Assert.Equal(BlockStatus.PreBooked, preBooked.Status);

        var claimed = await _blocks.ClaimPreBookedBlockAsync(preBooked, batchId, existingOrders: [], userId: 7);

        Assert.True(claimed);
        Assert.Empty(await _blocks.GetPreBookedByAnimalAsync(animalId)); // retired — never reappears as "available"
        var claimedBlock = Assert.Single(await _blocks.GetByBatchAsync(batchId));
        Assert.Equal(BlockStatus.PreBookedUsed, claimedBlock.Status);
        Assert.Equal("05", claimedBlock.BlockRef);
    }

    [Fact]
    public async Task PreBookedBlock_ClaimedTwice_SecondClaimFails()
    {
        const int animalId = 21, batchId = 3;
        await _blocks.CreatePreBookedBlockAsync(animalId, "06");
        var preBooked = Assert.Single(await _blocks.GetPreBookedByAnimalAsync(animalId));

        var firstClaim = await _blocks.ClaimPreBookedBlockAsync(preBooked, batchId, existingOrders: [], userId: 7);
        var secondClaim = await _blocks.ClaimPreBookedBlockAsync(preBooked, batchId + 1, existingOrders: [], userId: 7);

        Assert.True(firstClaim);
        Assert.False(secondClaim); // already PreBookedUsed — not claimable again
    }

    [Fact]
    public async Task CopyBlockAsync_CopiesOntoAnotherAnimal_WithANewRefAndOrder_OriginalUnchanged()
    {
        const int batchId = 4, sourceAnimalId = 30, targetAnimalId = 31;
        var sourceId = await _blocks.AddBlockAsync(batchId, sourceAnimalId, "01", existingOrders: [], userId: 7, customerRef: "CR1", repeatBlock: true);
        var source = (await _blocks.GetByBatchAsync(batchId)).Single(b => b.ID == sourceId);

        var copyId = await _blocks.CopyBlockAsync(source, batchId, targetAnimalId,
            existingBlockRefs: ["01"], existingOrders: [0], userId: 7);

        var all = await _blocks.GetByBatchAsync(batchId);
        var copy = all.Single(b => b.ID == copyId);
        Assert.NotEqual(sourceId, copyId);
        Assert.Equal(targetAnimalId, copy.AnimalID);
        Assert.NotEqual(source.BlockRef, copy.BlockRef); // new ref assigned, not a duplicate of the source
        Assert.NotEqual(source.Order, copy.Order);
        Assert.Equal("CR1", copy.CustomerRef); // copied fields carried over
        Assert.True(copy.RepeatBlock);

        var unchanged = (await _blocks.GetByBatchAsync(batchId)).Single(b => b.ID == sourceId);
        Assert.Equal(sourceAnimalId, unchanged.AnimalID); // original block untouched by the copy
    }

    [Fact]
    public async Task DeleteBlockAsync_RemovesTheBlockFromTheBatchListing()
    {
        const int batchId = 5, animalId = 40;
        var blockId = await _blocks.AddBlockAsync(batchId, animalId, "01", existingOrders: [], userId: 7);
        Assert.Single(await _blocks.GetByBatchAsync(batchId));

        var deleted = await _blocks.DeleteBlockAsync(blockId, userId: 7);

        Assert.True(deleted);
        Assert.Empty(await _blocks.GetByBatchAsync(batchId));
    }

    [Fact]
    public async Task DeleteBlockAsync_UnknownBlock_ReturnsFalse()
    {
        Assert.False(await _blocks.DeleteBlockAsync(blockId: 9999, userId: 7));
    }

    /// <summary>In-memory stand-in for the block stored procedures, mirroring the real order/ref allocation rules in <see cref="BlockHelpers"/>.</summary>
    private sealed class FakeBlockService : IBlockService
    {
        private int _nextId = 1;
        private readonly Dictionary<int, Block> _store = [];

        public Task<IReadOnlyList<Block>> GetByBatchAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<Block>)_store.Values.Where(b => b.BatchID == batchId).ToList());

        public Task<IReadOnlyList<Block>> GetPreBookedByAnimalAsync(int animalId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<Block>)_store.Values.Where(b => b.AnimalID == animalId && b.Status == BlockStatus.PreBooked).ToList());

        public Task<int> AddBlockAsync(int batchId, int animalId, string blockRef, IEnumerable<int> existingOrders, int userId,
            string? customerRef = null, string? comment = null, bool repeatBlock = false, CancellationToken ct = default)
        {
            var id = _nextId++;
            _store[id] = new Block
            {
                ID = id,
                BatchID = batchId,
                AnimalID = animalId,
                BlockRef = blockRef,
                CustomerRef = customerRef,
                Comment = comment,
                RepeatBlock = repeatBlock,
                Status = BlockStatus.Used,
                Order = BlockHelpers.ComputeNextOrder(existingOrders),
                RowStamp = [1],
            };
            return Task.FromResult(id);
        }

        public Task<bool> CreatePreBookedBlockAsync(int animalId, string blockRef, CancellationToken ct = default)
        {
            var id = _nextId++;
            _store[id] = new Block { ID = id, AnimalID = animalId, BlockRef = blockRef, Status = BlockStatus.PreBooked, RowStamp = [1] };
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<PreBookedBlockResult>?> BookPreBookedBlocksAsync(string senderRef, IReadOnlyList<int> blockRefs, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PreBookedBlockResult>?>(null);

        public Task<bool> ClaimPreBookedBlockAsync(Block preBooked, int batchId, IEnumerable<int> existingOrders, int userId,
            string? customerRef = null, string? comment = null, bool repeatBlock = false, CancellationToken ct = default)
        {
            if (!_store.TryGetValue(preBooked.ID, out var existing) || existing.Status != BlockStatus.PreBooked)
                return Task.FromResult(false);

            _store[preBooked.ID] = new Block
            {
                ID = existing.ID,
                BatchID = batchId,
                AnimalID = existing.AnimalID,
                BlockRef = existing.BlockRef,
                CustomerRef = customerRef ?? existing.CustomerRef,
                Comment = comment ?? existing.Comment,
                RepeatBlock = repeatBlock,
                Status = BlockStatus.PreBookedUsed,
                Order = BlockHelpers.ComputeNextOrder(existingOrders),
                RowStamp = [1],
            };
            return Task.FromResult(true);
        }

        public Task<bool> UpdateBlockAsync(Block block, int userId, CancellationToken ct = default)
        {
            if (!_store.ContainsKey(block.ID)) return Task.FromResult(false);
            _store[block.ID] = block;
            return Task.FromResult(true);
        }

        public Task<int> CopyBlockAsync(Block source, int newBatchId, int newAnimalId,
            IEnumerable<string> existingBlockRefs, IEnumerable<int> existingOrders,
            int userId, CancellationToken ct = default)
        {
            var id = _nextId++;
            _store[id] = new Block
            {
                ID = id,
                BatchID = newBatchId,
                AnimalID = newAnimalId,
                BlockRef = BlockHelpers.ComputeNextBlockRef(existingBlockRefs),
                CustomerRef = source.CustomerRef,
                Comment = source.Comment,
                RepeatBlock = source.RepeatBlock,
                Status = BlockStatus.Used,
                Order = BlockHelpers.ComputeNextOrder(existingOrders),
                RowStamp = [1],
            };
            return Task.FromResult(id);
        }

        public Task<bool> DeleteBlockAsync(int blockId, int userId, CancellationToken ct = default) =>
            Task.FromResult(_store.Remove(blockId));

        public Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsByHistologyRefAsync(string histologyRef, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<UsedBlockRef>)new List<UsedBlockRef>());

        public Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsBySenderRefAsync(string senderRef, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<UsedBlockRef>)new List<UsedBlockRef>());

        public Task<IReadOnlyList<BlockArchiveInfo>> GetBlockArchiveAsync(string? senderRef, string? histologyRef, string? blockRef, string? archiveLocation, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<BlockArchiveInfo>)new List<BlockArchiveInfo>());

        public Task<IReadOnlyList<SlideArchiveInfo>> GetSlideArchiveAsync(string? senderRef, string? histologyRef, string? archiveLocation, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<SlideArchiveInfo>)new List<SlideArchiveInfo>());
    }
}
