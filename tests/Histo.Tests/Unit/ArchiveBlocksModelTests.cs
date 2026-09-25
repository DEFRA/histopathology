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

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="ArchiveBlocksModel"/>.</summary>
public class ArchiveBlocksModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public ArchiveBlocksModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.IsViewSubmissionMode);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
        _session.Setup(s => s.UserID).Returns(99);
        _lookups.Setup(l => l.GetLookupDataAsync(16, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { Code = "A", Name = "Archive A" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(19, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
    }

    private ArchiveBlocksModel CreateSut() =>
        new(_session.Object, _blocks.Object, _batches.Object, _submissions.Object, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnGetAsync_WithBatchId_SetsSessionAndLoadsRows()
    {
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42 });
        _blocks.Setup(b => b.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, BlockRef = "01", AnimalID = 7 }]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 7, SenderRef = "S1", HistologyRef = "24/001" }]);
        var sut = CreateSut();
        sut.BatchId = 42;

        await sut.OnGetAsync();

        Assert.Equal(42, _session.Object.BatchID);
        Assert.Single(sut.Rows);
        Assert.Equal("S1", sut.Rows[0].SenderRef);
        Assert.Equal("/Batches/BatchesForArchiving", _session.Object.ReturnPage);
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
    public async Task OnPostUpdateAsync_NoSelection_ReturnsError()
    {
        _session.Object.BatchID = 42;
        _blocks.Setup(b => b.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        var sut = CreateSut();

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select at least one block to update.", sut.Error);
        _blocks.Verify(b => b.UpdateBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SingleSelection_OverwritesAllFields()
    {
        _session.Object.BatchID = 42;
        var existing = new Block { ID = 1, BlockRef = "01", AnimalID = 7, ArchiveLocation = "OLD", ArchiveComment = "old comment" };
        _blocks.Setup(b => b.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[existing]);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.ArchiveLocationCode = "NEW";
        sut.Comment = null; // deliberately blank — single mode still overwrites to null

        await sut.OnPostUpdateAsync();

        _blocks.Verify(b => b.UpdateBlockAsync(
            It.Is<Block>(x => x.ID == 1 && x.ArchiveLocation == "NEW" && x.ArchiveComment == null), 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostUpdateAsync_BulkSelection_PreservesBlankFields()
    {
        _session.Object.BatchID = 42;
        var a = new Block { ID = 1, BlockRef = "01", AnimalID = 7, ArchiveComment = "keep me" };
        var b = new Block { ID = 2, BlockRef = "02", AnimalID = 8, ArchiveComment = "keep me too" };
        _blocks.Setup(x => x.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[a, b]);
        var sut = CreateSut();
        sut.SelectedIds = [1, 2];
        sut.ArchiveLocationCode = "BULK";
        sut.Comment = null;

        await sut.OnPostUpdateAsync();

        _blocks.Verify(x => x.UpdateBlockAsync(
            It.Is<Block>(blk => blk.ID == 1 && blk.ArchiveLocation == "BULK" && blk.ArchiveComment == "keep me"), 99, It.IsAny<CancellationToken>()), Times.Once);
        _blocks.Verify(x => x.UpdateBlockAsync(
            It.Is<Block>(blk => blk.ID == 2 && blk.ArchiveLocation == "BULK" && blk.ArchiveComment == "keep me too"), 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostUpdateAsync_ArchivedDateBeforeReceivedDate_ReturnsError()
    {
        _session.Object.BatchID = 42;
        _batches.Setup(x => x.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42, ReceivedDate = new DateTime(2026, 1, 10) });
        _blocks.Setup(x => x.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1 }]);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.ArchivedDate = new DateTime(2026, 1, 1);

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Submission received date", sut.Error);
        _blocks.Verify(x => x.UpdateBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_ViewMode_DoesNotUpdate()
    {
        _session.Object.BatchID = 42;
        _session.Object.IsViewSubmissionMode = true;
        _blocks.Setup(x => x.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1 }]);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.ArchiveLocationCode = "X";

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _blocks.Verify(x => x.UpdateBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
