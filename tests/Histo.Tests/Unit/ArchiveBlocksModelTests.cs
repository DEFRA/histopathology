using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Pages.Archive;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="ArchiveBlocksModel"/>.</summary>
public class ArchiveBlocksModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockService> _blocks = new();

    public ArchiveBlocksModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
    }

    private ArchiveBlocksModel CreateSut() =>
        new(_session.Object, _blocks.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnGetAsync_NoBatchIdInSession_LeavesBlocksEmpty()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Empty(sut.Blocks);
        _blocks.Verify(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Filters — legacy ddlHistologyRefList / ddlBlockRefList applied via DataView.RowFilter ──

    /// <summary>Three blocks across two animals, giving two histology refs and three block refs.</summary>
    private void SetupFilterFixture()
    {
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42 });
        _blocks.Setup(b => b.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)
            [
                new Block { ID = 1, BlockRef = "01", AnimalID = 7 },
                new Block { ID = 2, BlockRef = "02", AnimalID = 7 },
                new Block { ID = 3, BlockRef = "03", AnimalID = 8 },
            ]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)
            [
                new Animal { ID = 7, SenderRef = "S1", HistologyRef = "24/001" },
                new Animal { ID = 8, SenderRef = "S2", HistologyRef = "24/002" },
            ]);
    }

    [Fact]
    public async Task NoFilter_ReturnsEveryRow()
    {
        SetupFilterFixture();
        var sut = CreateSut();
        sut.BatchId = 42;

        await sut.OnGetAsync();

        Assert.False(sut.FilterApplied);
        Assert.Equal(3, sut.TotalCount);
        Assert.Equal(["24/001", "24/002"], sut.HistologyRefOptions);
        Assert.Equal(["01", "02", "03"], sut.BlockRefOptions);
    }

    [Fact]
    public async Task HistologyRefFilter_KeepsOnlyThatAnimalsBlocks()
    {
        SetupFilterFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        sut.FilterHistologyRef = "24/001";

        await sut.OnGetAsync();

        Assert.True(sut.FilterApplied);
        Assert.Equal(2, sut.TotalCount);
        Assert.All(sut.FilteredRows, r => Assert.Equal("24/001", r.HistologyRef));
    }

    [Fact]
    public async Task BlockRefFilter_KeepsOnlyThatBlock()
    {
        SetupFilterFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        sut.FilterBlockRef = "03";

        await sut.OnGetAsync();

        Assert.Equal(1, sut.TotalCount);
        Assert.Equal("S2", sut.FilteredRows[0].SenderRef);
    }

    [Fact]
    public async Task BothFiltersCombine_AsAnAndCondition()
    {
        SetupFilterFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        sut.FilterHistologyRef = "24/001";
        sut.FilterBlockRef = "03"; // block 03 belongs to 24/002, so the combination matches nothing

        await sut.OnGetAsync();

        Assert.Equal(0, sut.TotalCount);
    }

    [Fact]
    public async Task FilterOptions_AreBuiltFromAllRowsNotTheFilteredSubset()
    {
        SetupFilterFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        sut.FilterBlockRef = "01";

        await sut.OnGetAsync();

        // Options must stay complete so the user can change or clear the filter.
        Assert.Equal(["01", "02", "03"], sut.BlockRefOptions);
        Assert.Equal(1, sut.TotalCount);
    }

    [Fact]
    public async Task OnGetAsync_BatchIdInSession_LoadsBlocks()
    {
        _session.Object.BatchID = 5;
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, BlockRef = "01" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Single(sut.Blocks);
    }

    [Fact]
    public async Task PagedEntries_DefaultSort_OrdersByBlockRefAscending()
    {
        _session.Object.BatchID = 5;
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[
                new Block { ID = 1, BlockRef = "03" },
                new Block { ID = 2, BlockRef = "01" },
                new Block { ID = 3, BlockRef = "02" }]);
        var sut = CreateSut();
        await sut.OnGetAsync();

        Assert.Equal(["01", "02", "03"], sut.PagedEntries.Select(b => b.BlockRef));
    }
}
