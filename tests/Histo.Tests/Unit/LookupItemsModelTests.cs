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
/// Unit tests for <see cref="LookupItemsModel"/> — the read-only pick-list item list page
/// (split out from the previous combined Add/Edit/List page per GDS "one thing per page").
/// </summary>
public class LookupItemsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    private LookupItemsModel CreateSut() =>
        new(_session.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()),
        };

    [Fact]
    public async Task OnGetAsync_CodeKeyedTable_SetsTableHasCodesTrueAndNoAreaColumn()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 16, TableName = "Archive Location" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(16, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Code = "A", Name = "Store A", Active = true }]);
        var sut = CreateSut();
        sut.TableId = 16;

        await sut.OnGetAsync();

        Assert.True(sut.TableHasCodes);
        Assert.False(sut.ShowAreaColumn);
        Assert.Equal("Archive Location", sut.TableName);
    }

    [Fact]
    public async Task OnGetAsync_AreaScopedTable_ShowsAreaColumnAndResolvesAreaNames()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 18, TableName = "Pathologists" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Area = "3", Name = "Dr Smith", Active = true }]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 3, Name = "Pathology" }]);
        var sut = CreateSut();
        sut.TableId = 18;

        await sut.OnGetAsync();

        Assert.True(sut.ShowAreaColumn);
        Assert.False(sut.TableHasCodes);
        Assert.Equal("Pathology", sut.AreaNameById["3"]);
    }

    [Fact]
    public async Task OnGetAsync_MoreThanOnePage_PagedEntriesReturnsOnlyFirstPage()
    {
        var items = Enumerable.Range(1, 15)
            .Select(n => new LookupItem { ID = n, Code = $"C{n:D2}", Name = $"Item {n:D2}", Active = true })
            .ToList();
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 16, TableName = "Archive Location" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(16, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)items);
        var sut = CreateSut();
        sut.TableId = 16;

        await sut.OnGetAsync();

        Assert.Equal(15, sut.TotalCount);
        Assert.Equal(10, sut.GetPagedEntries().Count);
        Assert.Equal("C01", sut.GetPagedEntries()[0].Code);
    }

    [Fact]
    public async Task OnGetAsync_SortByDescriptionDescending_OrdersEntries()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 16, TableName = "Archive Location" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(16, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { ID = 1, Code = "A", Name = "Alpha", Active = true },
                new LookupItem { ID = 2, Code = "B", Name = "Beta", Active = true },
            ]);
        var sut = CreateSut();
        sut.TableId = 16;
        sut.SortColumn = "Description";
        sut.SortDesc = true;

        await sut.OnGetAsync();

        Assert.Equal("Beta", sut.GetPagedEntries()[0].Name);
    }

    private async Task<LookupItemsModel> LoadAreaScopedPageAsync(string? sortColumn, bool sortDesc)
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 18, TableName = "Pathologists" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { ID = 1, Area = "3", Name = "Zed", Active = true },
                new LookupItem { ID = 2, Area = "4", Name = "Amy", Active = false },
                new LookupItem { ID = 3, Area = "9", Name = "Mid", Active = true },
            ]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { ID = 3, Name = "Pathology" },
                new LookupItem { ID = 4, Name = "Abattoir" },
            ]);
        var sut = CreateSut();
        sut.TableId = 18;
        sut.SortColumn = sortColumn;
        sut.SortDesc = sortDesc;

        await sut.OnGetAsync();
        return sut;
    }

    [Fact]
    public async Task GetPagedEntries_AreaScopedTable_DefaultSortUsesAreaNameFallingBackToRawArea()
    {
        var sut = await LoadAreaScopedPageAsync(null, false);

        // Area names resolve to Abattoir/Pathology; unmatched area "9" keeps its raw value.
        var entries = sut.GetPagedEntries();
        Assert.Equal([3, 2, 1], entries.Select(i => i.ID).ToArray());
        Assert.Equal("9", sut.FirstColumnValue(entries[0]));
        Assert.Equal("Abattoir", sut.FirstColumnValue(entries[1]));
        Assert.Equal("Pathology", sut.FirstColumnValue(entries[2]));
    }

    [Theory]
    [InlineData("Code", false, new[] { 3, 2, 1 })]
    [InlineData("Code", true, new[] { 1, 2, 3 })]
    [InlineData("Description", false, new[] { 2, 3, 1 })]
    [InlineData("Active", false, new[] { 2, 1, 3 })]
    [InlineData("Active", true, new[] { 1, 3, 2 })]
    public async Task GetPagedEntries_SortColumn_OrdersEntries(string column, bool desc, int[] expectedIds)
    {
        var sut = await LoadAreaScopedPageAsync(column, desc);

        Assert.Equal(expectedIds, sut.GetPagedEntries().Select(i => i.ID).ToArray());
    }

    [Fact]
    public async Task GetPagedEntries_SortByCodeOnCodeKeyedTable_OrdersByCode()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 16, TableName = "Archive Location" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(16, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { ID = 1, Code = "B", Name = "One", Active = true },
                new LookupItem { ID = 2, Code = "A", Name = "Two", Active = true },
            ]);
        var sut = CreateSut();
        sut.TableId = 16;
        sut.SortColumn = "Code";

        await sut.OnGetAsync();

        Assert.Equal([2, 1], sut.GetPagedEntries().Select(i => i.ID).ToArray());
    }

    [Fact]
    public async Task OnGetAsync_ShowDeactivatedFalse_ExcludesInactiveFromCountAndPaging()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 16, TableName = "Archive Location" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(16, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { ID = 1, Code = "A", Name = "Active item", Active = true },
                new LookupItem { ID = 2, Code = "B", Name = "Inactive item", Active = false },
            ]);
        var sut = CreateSut();
        sut.TableId = 16;
        sut.ShowDeactivated = false;

        await sut.OnGetAsync();

        Assert.Equal(1, sut.TotalCount);
        Assert.Single(sut.GetPagedEntries());
    }
}

