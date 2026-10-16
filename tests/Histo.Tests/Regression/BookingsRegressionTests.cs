using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Pages.Bookings;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the Bookings workflow — <c>BookBlockRef</c> (pre-booking placeholder
/// blocks for a sender-ref/block-ref range) and <c>BookHistologyRef</c>/<c>EditHistologyRef</c>
/// (the histology-ref counter). Each chain is backed by a stateful fake of the one service it
/// depends on, so duplicate-booking and counter-range-exhaustion regressions are caught without
/// a real database.
/// </summary>
public class BookingsRegressionTests
{
    private readonly Mock<ISessionService> _session = new();

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private BookBlockRefModel CreateBookBlockRef(FakeBlockBookingService blocks) => new(_session.Object, blocks)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private BookHistologyRefModel CreateBookHistologyRef(FakeHistologyRefService refs) => new(_session.Object, refs)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private EditHistologyRefModel CreateEditHistologyRef(FakeHistologyRefService refs) => new(_session.Object, refs)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    // ── BookBlockRef ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task BookBlockRef_PlainSenderRefRange_BooksAllRequestedBlockRefs()
    {
        var blocks = new FakeBlockBookingService();
        var sut = CreateBookBlockRef(blocks);
        sut.SenderRefFrom = "S001";
        sut.BlockRefFrom = "01";
        sut.BlockRefTo = "03";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Blocks booked successfully.", sut.SuccessMessage);
        Assert.Contains(sut.ResultMessages, m => m.Contains("3 blocks booked, 0 blocks not booked"));
        Assert.Equal(3, blocks.BookedCount("S001"));
    }

    [Fact]
    public async Task BookBlockRef_SameRangeBookedTwice_SecondAttemptReportsAlreadyExists()
    {
        var blocks = new FakeBlockBookingService();
        var first = CreateBookBlockRef(blocks);
        first.SenderRefFrom = "S002";
        first.BlockRefFrom = "01";
        first.BlockRefTo = "02";
        await first.OnPostAsync();

        var second = CreateBookBlockRef(blocks);
        second.SenderRefFrom = "S002";
        second.BlockRefFrom = "01";
        second.BlockRefTo = "02";
        await second.OnPostAsync();

        Assert.Null(second.SuccessMessage); // not all booked -> no blanket success message
        Assert.Contains(second.ResultMessages, m => m.Contains("Block 01 not booked as it already exists"));
        Assert.Contains(second.ResultMessages, m => m.Contains("Block 02 not booked as it already exists"));
        Assert.Equal(2, blocks.BookedCount("S002")); // still just the original 2 — no duplicates created
    }

    [Fact]
    public async Task BookBlockRef_InvalidBlockRefFrom_IsRejectedBeforeCallingTheService()
    {
        var blocks = new FakeBlockBookingService();
        var sut = CreateBookBlockRef(blocks);
        sut.SenderRefFrom = "S003";
        sut.BlockRefFrom = "00"; // explicitly rejected by SenderRefHelpers.IsValidBlockRef

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The requested block ref range cannot be created.", sut.Error);
        Assert.Equal(0, blocks.BookedCount("S003"));
    }

    // ── BookHistologyRef / EditHistologyRef ─────────────────────────────────────

    [Fact]
    public async Task BookHistologyRef_BooksARange_AdvancesTheCounterPastIt()
    {
        var refs = new FakeHistologyRefService();
        refs.SeedCounter(HistologyRefTypeCode.TBDiagnostic, "TB Diagnostic", "30001");
        var sut = CreateBookHistologyRef(refs);
        await sut.OnGetAsync();

        sut.HistologyType = HistologyRefTypeCode.TBDiagnostic;
        sut.NumberToBook = 5;
        await sut.OnPostAsync();

        Assert.Null(sut.Error);
        Assert.Equal("You have successfully booked Histology numbers in the range 30001 - 30005, inclusive.", sut.SuccessMessage);
        Assert.Equal("30006", refs.GetCounter(HistologyRefTypeCode.TBDiagnostic).NextHistologyRef);
    }

    [Fact]
    public async Task BookHistologyRef_RequestingMoreThanRemainInTheRange_IsRejectedAndCounterUnchanged()
    {
        var refs = new FakeHistologyRefService();
        refs.SeedCounter(HistologyRefTypeCode.TBDiagnostic, "TB Diagnostic", "39999"); // only 1 left (range ends at 39999 inclusive)
        var sut = CreateBookHistologyRef(refs);

        sut.HistologyType = HistologyRefTypeCode.TBDiagnostic;
        sut.NumberToBook = 5;
        await sut.OnPostAsync();

        Assert.NotNull(sut.Error);
        Assert.Null(sut.SuccessMessage);
        Assert.Equal("39999", refs.GetCounter(HistologyRefTypeCode.TBDiagnostic).NextHistologyRef); // unchanged
    }

    [Fact]
    public async Task EditHistologyRef_OverridesTheCounter_SubsequentBookingStartsFromTheNewValue()
    {
        var refs = new FakeHistologyRefService();
        refs.SeedCounter(HistologyRefTypeCode.TBDiagnostic, "TB Diagnostic", "30001");

        var edit = CreateEditHistologyRef(refs);
        edit.HistologyType = HistologyRefTypeCode.TBDiagnostic;
        edit.NewHistologyRef = "30100";
        var editResult = await edit.OnPostAsync();

        Assert.IsType<PageResult>(editResult);
        Assert.Equal("The Histology Reference has been updated.", edit.SuccessMessage);

        var book = CreateBookHistologyRef(refs);
        book.HistologyType = HistologyRefTypeCode.TBDiagnostic;
        book.NumberToBook = 3;
        await book.OnPostAsync();

        Assert.Equal("You have successfully booked Histology numbers in the range 30100 - 30102, inclusive.", book.SuccessMessage);
    }

    [Fact]
    public async Task EditHistologyRef_ValueOutsideTypesRange_IsRejected()
    {
        var refs = new FakeHistologyRefService();
        refs.SeedCounter(HistologyRefTypeCode.TBDiagnostic, "TB Diagnostic", "30001");
        var sut = CreateEditHistologyRef(refs);
        sut.HistologyType = HistologyRefTypeCode.TBDiagnostic;
        sut.NewHistologyRef = "40000"; // outside TBDiagnostic's 30000-39999 range

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("must be a 5-digit number between 30000 and 39999", sut.Error);
        Assert.Equal("30001", refs.GetCounter(HistologyRefTypeCode.TBDiagnostic).NextHistologyRef); // unchanged
    }

    /// <summary>In-memory stand-in for <see cref="IBlockService.BookPreBookedBlocksAsync"/> — the only member <see cref="BookBlockRefModel"/> calls.</summary>
    private sealed class FakeBlockBookingService : IBlockService
    {
        private readonly HashSet<(string SenderRef, int BlockRef)> _booked = [];

        public int BookedCount(string senderRef) => _booked.Count(b => b.SenderRef == senderRef);

        public Task<IReadOnlyList<PreBookedBlockResult>?> BookPreBookedBlocksAsync(string senderRef, IReadOnlyList<int> blockRefs, CancellationToken ct = default)
        {
            var results = blockRefs.Select(r =>
            {
                var outcome = _booked.Add((senderRef, r)) ? PreBookedBlockOutcome.Booked : PreBookedBlockOutcome.AlreadyExists;
                return new PreBookedBlockResult(r, outcome);
            }).ToList();
            return Task.FromResult<IReadOnlyList<PreBookedBlockResult>?>(results);
        }

        public Task<IReadOnlyList<Block>> GetByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Block>)new List<Block>());
        public Task<IReadOnlyList<Block>> GetPreBookedByAnimalAsync(int animalId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<Block>)new List<Block>());
        public Task<int> AddBlockAsync(int batchId, int animalId, string blockRef, IEnumerable<int> existingOrders, int userId, string? customerRef = null, string? comment = null, bool repeatBlock = false, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> CreatePreBookedBlockAsync(int animalId, string blockRef, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ClaimPreBookedBlockAsync(Block preBooked, int batchId, IEnumerable<int> existingOrders, int userId, string? customerRef = null, string? comment = null, bool repeatBlock = false, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> UpdateBlockAsync(Block block, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> CopyBlockAsync(Block source, int newBatchId, int newAnimalId, IEnumerable<string> existingBlockRefs, IEnumerable<int> existingOrders, int userId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> DeleteBlockAsync(int blockId, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsByHistologyRefAsync(string histologyRef, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<UsedBlockRef>)new List<UsedBlockRef>());
        public Task<IReadOnlyList<UsedBlockRef>> GetUsedBlockRefsBySenderRefAsync(string senderRef, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<UsedBlockRef>)new List<UsedBlockRef>());
        public Task<IReadOnlyList<BlockArchiveInfo>> GetBlockArchiveAsync(string? senderRef, string? histologyRef, string? blockRef, string? archiveLocation, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<BlockArchiveInfo>)new List<BlockArchiveInfo>());
        public Task<IReadOnlyList<SlideArchiveInfo>> GetSlideArchiveAsync(string? senderRef, string? histologyRef, string? archiveLocation, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<SlideArchiveInfo>)new List<SlideArchiveInfo>());
    }

    /// <summary>In-memory stand-in for the histology-ref counter stored procedures.</summary>
    private sealed class FakeHistologyRefService : IHistologyRefService
    {
        private readonly Dictionary<int, HistologyRefCounter> _counters = [];

        public void SeedCounter(int type, string description, string nextRef) =>
            _counters[type] = new HistologyRefCounter { Type = type, Description = description, NextHistologyRef = nextRef, RowStamp = [1] };

        public HistologyRefCounter GetCounter(int type) => _counters[type];

        public Task<IReadOnlyList<HistologyRefCounter>> GetCountersAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<HistologyRefCounter>)_counters.Values.ToList());

        public Task<HistologyBookingResult> BookCounterRangeAsync(int histologyType, int numberToBook, CancellationToken ct = default)
        {
            if (!_counters.TryGetValue(histologyType, out var counter) || !HistologyRefTypeCode.TryGetRange(histologyType, out _, out var upper))
                return Task.FromResult(new HistologyBookingResult { Success = false, Error = "Unknown histology ref type." });

            var first = int.Parse(counter.NextHistologyRef);
            var last = first + numberToBook - 1;
            if (last >= upper)
                return Task.FromResult(new HistologyBookingResult { Success = false, Error = "Not enough numbers remaining in this range." });

            _counters[histologyType] = new HistologyRefCounter { Type = counter.Type, Description = counter.Description, NextHistologyRef = (last + 1).ToString(), RowStamp = [1] };
            return Task.FromResult(new HistologyBookingResult { Success = true, FirstBooked = first, LastBooked = last });
        }

        public Task<bool> SetCounterAsync(int histologyType, string newNextHistologyRef, CancellationToken ct = default)
        {
            if (!_counters.TryGetValue(histologyType, out var counter)) return Task.FromResult(false);
            _counters[histologyType] = new HistologyRefCounter { Type = counter.Type, Description = counter.Description, NextHistologyRef = newNextHistologyRef, RowStamp = [1] };
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<HistologyRef>> GetUnusedRefsAsync(int histologyType, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<HistologyRef>)new List<HistologyRef>());
        public Task<IReadOnlyList<HistologyRef>> GetUsedRefsByBatchAsync(int batchId, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<HistologyRef>)new List<HistologyRef>());
        public Task<IReadOnlyList<HistologyRef>> GetUnusedBookedRefsAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<HistologyRef>)new List<HistologyRef>());
        public Task<IReadOnlyList<HistologyRef>> GetAllUnusedRefsAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<HistologyRef>)new List<HistologyRef>());
        public Task<string?> GetNextAvailableRefAsync(int histologyType, CancellationToken ct = default) => Task.FromResult((string?)null);
        public Task<IReadOnlyList<string>> GetNextAvailableRefRunAsync(int histologyType, int count, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<string>)new List<string>());
    }
}
