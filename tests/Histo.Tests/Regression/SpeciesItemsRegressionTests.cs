using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Tier 2 gap closure: regression suite for the Species pick-list (Add→List→Edit), which
/// previously only had atomic Unit/ coverage of each page in isolation. Backed by a stateful
/// <see cref="FakeSpeciesLookupService"/> so the chained workflow is genuinely exercised,
/// mirroring the pattern already used for User/PickList Maintenance.
/// </summary>
public class SpeciesItemsRegressionTests
{
    private readonly FakeSpeciesLookupService _lookups = new();
    private readonly Mock<ISessionService> _session = new();

    public SpeciesItemsRegressionTests() => _session.Setup(s => s.UserID).Returns(7);

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private AddSpeciesItemModel CreateAdd()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _lookups)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    private EditSpeciesItemModel CreateEdit()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _lookups)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    private SpeciesItemsModel CreateList()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _lookups)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    [Fact]
    public async Task AddThenList_NewSpeciesAppearsInTheGrid()
    {
        var add = CreateAdd();
        add.Species = "Ovine";
        add.CommonName = "Sheep";

        var result = await add.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var list = CreateList();
        await list.OnGetAsync();
        Assert.Contains(list.PagedEntries, i => i.Species == "Ovine" && i.CommonName == "Sheep");
    }

    [Fact]
    public async Task Add_AssignsIncrementingSpeciesIdsAcrossInserts()
    {
        var first = CreateAdd();
        first.Species = "Bovine";
        await first.OnPostAsync();

        var second = CreateAdd();
        second.Species = "Caprine";
        await second.OnPostAsync();

        var ids = _lookups.Items.Select(i => i.SpeciesID).ToList();
        Assert.Equal(2, ids.Distinct().Count()); // no duplicate IDs assigned
    }

    [Fact]
    public async Task Add_BlankSpecies_IsRejected()
    {
        var add = CreateAdd();
        add.Species = "   ";

        var result = await add.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a species.", add.Errors["Species"]);
        Assert.Empty(_lookups.Items);
    }

    [Fact]
    public async Task AddEdit_RenamesAndUpdatesCommonName()
    {
        var add = CreateAdd();
        add.Species = "Porcine";
        add.CommonName = "Pig";
        await add.OnPostAsync();
        var speciesId = _lookups.Items.Single().SpeciesID;

        var edit = CreateEdit();
        edit.SpeciesID = speciesId;
        await edit.OnGetAsync();
        Assert.Equal("Porcine", edit.Species); // pre-populated

        edit.Species = "Porcine (updated)";
        edit.CommonName = "Domestic pig";
        var result = await edit.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var saved = _lookups.Items.Single();
        Assert.Equal("Porcine (updated)", saved.Species);
        Assert.Equal("Domestic pig", saved.CommonName);
    }

    [Fact]
    public async Task Edit_UnknownSpeciesId_RedirectsToList()
    {
        var edit = CreateEdit();
        edit.SpeciesID = 9999;

        var result = await edit.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/SpeciesItems", redirect.PageName);
    }

    [Fact]
    public async Task Edit_BlankSpecies_IsRejectedAndOriginalRowUnchanged()
    {
        var add = CreateAdd();
        add.Species = "Equine";
        await add.OnPostAsync();
        var speciesId = _lookups.Items.Single().SpeciesID;

        var edit = CreateEdit();
        edit.SpeciesID = speciesId;
        edit.Species = "";

        var result = await edit.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a species.", edit.Errors["Species"]);
        Assert.Equal("Equine", _lookups.Items.Single().Species);
    }

    /// <summary>In-memory stand-in for the Species pick-list stored procedures.</summary>
    private sealed class FakeSpeciesLookupService : ILookupService
    {
        private int _nextId = 1;
        public List<SpeciesItem> Items { get; } = [];

        public Task<IReadOnlyList<SpeciesItem>> GetSpeciesItemsAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<SpeciesItem>)Items.ToList());

        public Task<bool> AddSpeciesItemAsync(int speciesId, string species, string? commonName, CancellationToken ct = default)
        {
            Items.Add(new SpeciesItem { SpeciesID = speciesId > 0 ? speciesId : _nextId, Species = species, CommonName = commonName });
            _nextId = Items.Max(i => i.SpeciesID) + 1;
            return Task.FromResult(true);
        }

        public Task<bool> UpdateSpeciesItemAsync(int speciesId, string species, string? commonName, int userId, CancellationToken ct = default)
        {
            var index = Items.FindIndex(i => i.SpeciesID == speciesId);
            if (index < 0) return Task.FromResult(false);
            Items[index] = new SpeciesItem { SpeciesID = speciesId, Species = species, CommonName = commonName };
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<LookupItem>> GetLookupDataAsync(int tableId, bool includeInactive = false, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetUserAreaDataAsync(int tableId, string userArea, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetContactsByAreaAsync(string area, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetProjectsByAreaAsync(string area, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<EditableLookup>> ListEditableLookupsAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<EditableLookup>)new List<EditableLookup>());
        public Task<IReadOnlyList<LookupItem>> GetUserGroupsAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetUserAreasAsync(bool includeInactive = false, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetImportedTablesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetSpeciesLookupAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetHistologyTypesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetPremiumChargesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetHistologyRefTypesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<bool> CreateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default) => Task.FromResult(false);
        public Task<bool> UpdateLookupItemAsync(int tableId, LookupItem item, int userId, string? originalCode = null, CancellationToken ct = default) => Task.FromResult(false);
    }
}
