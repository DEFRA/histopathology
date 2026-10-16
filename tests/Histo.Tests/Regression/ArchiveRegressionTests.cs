using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Archive;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the Archive workflow — <c>ArchiveMenu</c> → <c>ArchiveBlocks</c>
/// (scenario ARCHIVE-01/02 in .github/agents/histo-testing-agent.agent.md §5). Backed by a
/// stateful <see cref="FakeArchiveBlockService"/> for the part under test (block archive
/// fields); the batch/animal/lookup/user context is read-only display data, so those four
/// collaborators are mocked rather than faked.
/// </summary>
public class ArchiveRegressionTests
{
    private readonly FakeArchiveBlockService _blocks = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public ArchiveRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 1, ReceivedDate = new DateTime(2026, 1, 1) });
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
    }

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private ArchiveMenuModel CreateMenu() => new(_session.Object)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private ArchiveBlocksModel CreateArchiveBlocks(int batchId)
    {
        _session.Setup(s => s.BatchID).Returns(batchId);
        var httpContext = NewHttpContext();
        return new(_session.Object, _blocks, _batches.Object, _submissions.Object, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
        };
    }

    [Fact]
    public void ArchiveMenu_BatchIdInQuery_StoresItInSessionForTheSubPages()
    {
        var menu = CreateMenu();
        menu.BatchId = 42;

        menu.OnGet();

        _session.VerifySet(s => s.BatchID = 42, Times.Once);
    }

    [Fact]
    public async Task ArchiveBlocks_UpdateSingleBlock_SetsLocationDateAndComment()
    {
        var batchId = 1;
        var blockId = _blocks.Seed(batchId, animalId: 1, blockRef: "01");
        var sut = CreateArchiveBlocks(batchId);
        await sut.OnGetAsync();

        sut.SelectedIds = [blockId];
        sut.ArchiveLocationCode = "STORE-A";
        sut.ArchivedDate = new DateTime(2026, 2, 1);
        sut.Comment = "Boxed up";

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Archive information updated for 1 block(s).", sut.SuccessMessage);
        var block = _blocks.Get(blockId);
        Assert.Equal("STORE-A", block.ArchiveLocation);
        Assert.Equal(new DateTime(2026, 2, 1), block.ArchivedDate);
        Assert.Equal("Boxed up", block.ArchiveComment);
    }

    [Fact]
    public async Task ArchiveBlocks_BulkUpdate_BlankFieldsLeaveExistingValuesUnchanged()
    {
        var batchId = 2;
        var block1 = _blocks.Seed(batchId, animalId: 1, blockRef: "01", archiveLocation: "OLD-LOC", archiveComment: "Keep me");
        var block2 = _blocks.Seed(batchId, animalId: 2, blockRef: "01", archiveLocation: "OLD-LOC-2", archiveComment: "Keep me too");
        var sut = CreateArchiveBlocks(batchId);
        await sut.OnGetAsync();

        // Bulk mode (2+ selected): only ArchivedDate is supplied, Location/Comment left blank —
        // each block's own existing Location/Comment must survive untouched.
        sut.SelectedIds = [block1, block2];
        sut.ArchivedDate = new DateTime(2026, 3, 1);

        await sut.OnPostUpdateAsync();

        Assert.Equal("OLD-LOC", _blocks.Get(block1).ArchiveLocation);
        Assert.Equal("Keep me", _blocks.Get(block1).ArchiveComment);
        Assert.Equal(new DateTime(2026, 3, 1), _blocks.Get(block1).ArchivedDate);
        Assert.Equal("OLD-LOC-2", _blocks.Get(block2).ArchiveLocation);
        Assert.Equal(new DateTime(2026, 3, 1), _blocks.Get(block2).ArchivedDate);
    }

    [Fact]
    public async Task ArchiveBlocks_NoRowsSelected_IsRejected()
    {
        var batchId = 3;
        _blocks.Seed(batchId, animalId: 1, blockRef: "01");
        var sut = CreateArchiveBlocks(batchId);
        await sut.OnGetAsync();

        sut.ArchiveLocationCode = "STORE-A"; // a value is supplied, but nothing is selected

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select at least one block to update.", sut.Error);
    }

    [Fact]
    public async Task ArchiveBlocks_DateBeforeSubmissionReceivedDate_IsRejected()
    {
        var batchId = 4;
        var blockId = _blocks.Seed(batchId, animalId: 1, blockRef: "01");
        var sut = CreateArchiveBlocks(batchId);
        await sut.OnGetAsync();

        sut.SelectedIds = [blockId];
        sut.ArchivedDate = new DateTime(2025, 12, 1); // before the batch's ReceivedDate (2026-01-01)

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("must be the same or later than the Submission received date", sut.Error);
        Assert.Null(_blocks.Get(blockId).ArchivedDate); // nothing was saved
    }

    [Fact]
    public async Task ArchiveBlocks_DateInTheFuture_IsRejected()
    {
        var batchId = 5;
        var blockId = _blocks.Seed(batchId, animalId: 1, blockRef: "01");
        var sut = CreateArchiveBlocks(batchId);
        await sut.OnGetAsync();

        sut.SelectedIds = [blockId];
        sut.ArchivedDate = DateTime.Today.AddDays(1);

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Archive date must be today or earlier.", sut.Error);
    }

    /// <summary>In-memory stand-in for the subset of block stored procedures ArchiveBlocksModel calls.</summary>
    private sealed class FakeArchiveBlockService : IBlockService
    {
        private int _nextId = 1;
        private readonly Dictionary<int, Block> _store = [];

        public int Seed(int batchId, int animalId, string blockRef, string? archiveLocation = null, string? archiveComment = null)
        {
            var id = _nextId++;
            _store[id] = new Block { ID = id, BatchID = batchId, AnimalID = animalId, BlockRef = blockRef, ArchiveLocation = archiveLocation, ArchiveComment = archiveComment, RowStamp = [1] };
            return id;
        }

        public Block Get(int id) => _store[id];

        public Task<IReadOnlyList<Block>> GetByBatchAsync(int batchId, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<Block>)_store.Values.Where(b => b.BatchID == batchId).ToList());

        public Task<bool> UpdateBlockAsync(Block block, int userId, CancellationToken ct = default)
        {
            if (!_store.ContainsKey(block.ID)) return Task.FromResult(false);
            _store[block.ID] = block;
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<Block>> GetPreBookedByAnimalAsync(int animalId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Block>)new List<Block>());
        public Task<int> AddBlockAsync(int batchId, int animalId, string blockRef, IEnumerable<int> existingOrders, int userId, string? customerRef = null, string? comment = null, bool repeatBlock = false, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> CreatePreBookedBlockAsync(int animalId, string blockRef, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyList<PreBookedBlockResult>?> BookPreBookedBlocksAsync(string senderRef, IReadOnlyList<int> blockRefs, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PreBookedBlockResult>?>(null);
        public Task<bool> ClaimPreBookedBlockAsync(Block preBooked, int batchId, IEnumerable<int> existingOrders, int userId, string? customerRef = null, string? comment = null, bool repeatBlock = false, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> CopyBlockAsync(Block source, int newBatchId, int newAnimalId, IEnumerable<string> existingBlockRefs, IEnumerable<int> existingOrders, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> DeleteBlockAsync(int blockId, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsByHistologyRefAsync(string histologyRef, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<UsedBlockRef>)new List<UsedBlockRef>());
        public Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsBySenderRefAsync(string senderRef, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<UsedBlockRef>)new List<UsedBlockRef>());
        public Task<IReadOnlyList<BlockArchiveInfo>> GetBlockArchiveAsync(string? senderRef, string? histologyRef, string? blockRef, string? archiveLocation, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BlockArchiveInfo>)new List<BlockArchiveInfo>());
        public Task<IReadOnlyList<SlideArchiveInfo>> GetSlideArchiveAsync(string? senderRef, string? histologyRef, string? archiveLocation, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<SlideArchiveInfo>)new List<SlideArchiveInfo>());
    }
}
