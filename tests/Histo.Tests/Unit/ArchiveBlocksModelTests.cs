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
