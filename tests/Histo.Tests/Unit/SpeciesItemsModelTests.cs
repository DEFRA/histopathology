using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SpeciesItemsModel"/> — the read-only Species pick-list grid.
/// </summary>
public class SpeciesItemsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    private SpeciesItemsModel CreateSut() =>
        new(_session.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
        };

    [Fact]
    public async Task OnGetAsync_LoadsItemsFromLookupService()
    {
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[
                new SpeciesItem { SpeciesID = 1, Species = "Bovine", CommonName = "Cattle" },
                new SpeciesItem { SpeciesID = 2, Species = "Ovine", CommonName = "Sheep" },
            ]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(2, sut.TotalCount);
        Assert.Equal("Bovine", sut.PagedEntries[0].Species);
    }

    [Fact]
    public async Task OnGetAsync_MoreThanOnePage_PagedEntriesReturnsOnlyFirstPage()
    {
        var items = Enumerable.Range(1, 15)
            .Select(n => new SpeciesItem { SpeciesID = n, Species = $"Species {n:D2}" })
            .ToList();
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)items);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(15, sut.TotalCount);
        Assert.Equal(10, sut.PagedEntries.Count);
        Assert.Equal("Species 01", sut.PagedEntries[0].Species);
    }

    [Fact]
    public async Task OnGetAsync_SortBySpeciesIDDescending_OrdersCorrectly()
    {
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[
                new SpeciesItem { SpeciesID = 1, Species = "Bovine" },
                new SpeciesItem { SpeciesID = 2, Species = "Ovine" },
            ]);
        var sut = CreateSut();
        sut.SortColumn = "SpeciesID";
        sut.SortDesc = true;

        await sut.OnGetAsync();

        Assert.Equal(2, sut.PagedEntries[0].SpeciesID);
    }

    [Fact]
    public async Task OnGetAsync_WhenStatusMessageExists_LoadsItFromTempData()
    {
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[
                new SpeciesItem { SpeciesID = 1, Species = "Bovine", CommonName = "Cattle" },
            ]);
        var sut = CreateSut();
        sut.TempData["StatusMessage"] = "Saved";

        await sut.OnGetAsync();

        Assert.Equal("Saved", sut.StatusMessage);
        Assert.Single(sut.PagedEntries);
    }

    [Fact]
    public async Task OnGetAsync_WhenLookupReturnsNoItems_LeavesGridEmpty()
    {
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SpeciesItem>());
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(0, sut.TotalCount);
        Assert.Empty(sut.PagedEntries);
    }

    [Fact]
    public async Task OnGetAsync_SortByCommonNameAscending_OrdersCorrectly()
    {
        // Covers the "CommonName" switch branch (ascending) in GetPagedEntries.
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[
                new SpeciesItem { SpeciesID = 1, Species = "Bovine", CommonName = "Zebra" },
                new SpeciesItem { SpeciesID = 2, Species = "Ovine", CommonName = "Antelope" },
            ]);
        var sut = CreateSut();
        sut.SortColumn = "CommonName";

        await sut.OnGetAsync();

        Assert.Equal("Antelope", sut.PagedEntries[0].CommonName);
    }

    [Fact]
    public async Task OnGetAsync_SortByCommonNameDescending_OrdersCorrectly()
    {
        // Covers the "CommonName" switch branch (descending).
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[
                new SpeciesItem { SpeciesID = 1, Species = "Bovine", CommonName = "Zebra" },
                new SpeciesItem { SpeciesID = 2, Species = "Ovine", CommonName = "Antelope" },
            ]);
        var sut = CreateSut();
        sut.SortColumn = "CommonName";
        sut.SortDesc = true;

        await sut.OnGetAsync();

        Assert.Equal("Zebra", sut.PagedEntries[0].CommonName);
    }

    [Fact]
    public async Task OnGetAsync_SortBySpeciesExplicitDescending_OrdersCorrectly()
    {
        // Covers the explicit "Species" switch branch (descending) — distinct from the
        // default (unmatched SortColumn) branch, which also sorts by Species but ascending.
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[
                new SpeciesItem { SpeciesID = 1, Species = "Bovine" },
                new SpeciesItem { SpeciesID = 2, Species = "Ovine" },
            ]);
        var sut = CreateSut();
        sut.SortColumn = "Species";
        sut.SortDesc = true;

        await sut.OnGetAsync();

        Assert.Equal("Ovine", sut.PagedEntries[0].Species);
    }
}
