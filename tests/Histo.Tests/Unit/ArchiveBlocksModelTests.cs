using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
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
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public ArchiveBlocksModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
    }

    private ArchiveBlocksModel CreateSut() =>
        new(_session.Object, _blocks.Object, _batches.Object, _submissions.Object, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnGetAsync_NoBatchIdInSession_LeavesRowsEmpty()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Empty(sut.Rows);
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
    public async Task OnGetAsync_BatchIdInSession_LoadsRows()
    {
        _session.Object.BatchID = 5;
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, BlockRef = "01" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Single(sut.Rows);
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

        Assert.Equal(["01", "02", "03"], sut.GetPagedEntries().Select(b => b.BlockRef));
    }

    [Theory]
    [InlineData(null, true, false)]
    [InlineData("L1", false, false)]
    [InlineData(null, false, false)]
    [InlineData(null, true, true)]
    [InlineData("L1", false, true)]
    public async Task OnPostUpdateAsync_MissingLocationOrDate_IsRejectedAndNothingIsSaved(string? location, bool withDate, bool allRows)
    {
        _session.Object.BatchID = 5;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[
                new Block { ID = 1, BlockRef = "01", ArchiveLocation = "L2", ArchivedDate = new DateTime(2024, 1, 2) },
                new Block { ID = 2, BlockRef = "02", ArchiveLocation = "L2", ArchivedDate = new DateTime(2024, 1, 2) }]);
        var sut = CreateSut();
        await sut.OnGetAsync();
        if (allRows) sut.SelectAllAcrossPages = true; else sut.SelectedIds = [1];
        sut.ArchiveLocationCode = location;
        sut.ArchivedDate = withDate ? DateTime.Today : null;

        await sut.OnPostUpdateAsync();

        Assert.Equal("Select an archive location and enter an archived date.", sut.Error);
        Assert.Null(sut.SuccessMessage);
        _blocks.Verify(b => b.UpdateBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_LocationAndDateGiven_UpdatesTheSelectedBlock()
    {
        _session.Object.BatchID = 5;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, BlockRef = "01", ArchiveLocation = "L2", ArchivedDate = new DateTime(2024, 1, 2) }]);
        var sut = CreateSut();
        await sut.OnGetAsync();
        sut.SelectedIds = [1];
        sut.ArchiveLocationCode = "L1";
        sut.ArchivedDate = DateTime.Today;

        await sut.OnPostUpdateAsync();

        Assert.Null(sut.Error);
        _blocks.Verify(b => b.UpdateBlockAsync(It.Is<Block>(x => x.ID == 1 && x.ArchiveLocation == "L1" && x.ArchivedDate == DateTime.Today), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostUpdateAsync_ArchivedDateBeforeReceivedDate_IsRejectedAndNothingIsSaved()
    {
        _session.Object.BatchID = 5;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 5, ReceivedDate = new DateTime(2024, 6, 10) });
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, BlockRef = "01" }]);
        var sut = CreateSut();
        await sut.OnGetAsync();
        sut.SelectedIds = [1];
        sut.ArchiveLocationCode = "L1";
        sut.ArchivedDate = new DateTime(2024, 6, 1);

        await sut.OnPostUpdateAsync();

        Assert.StartsWith("Archive date must be the same or later than the Submission received date of", sut.Error);
        _blocks.Verify(b => b.UpdateBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
