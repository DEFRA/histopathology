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
/// Regression suite for the area-scoped pick-list quick-add editor (Projects/Contacts, reached
/// from BatchDetails' "New Project"/"New Contact" buttons) — previously had zero test coverage of
/// any kind (Tier 1 gap). Backed by a stateful <see cref="FakeAreaLookupService"/> so Add→List
/// and Edit→List are genuinely exercised, scoped to the correct user area.
/// </summary>
public class PickListUserAreaRegressionTests
{
    private const int ProjectsTableId = 19;
    private readonly FakeAreaLookupService _lookups = new();
    private readonly Mock<ISessionService> _session = new();

    public PickListUserAreaRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.UserArea).Returns("HISTO");
        _lookups.Tables.Add(new EditableLookup { ID = ProjectsTableId, TableName = "Projects" });
    }

    private PickListUserAreaModel CreateSut()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        return new(_session.Object, _lookups)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
            TableId = ProjectsTableId,
        };
    }

    [Fact]
    public async Task Add_NoAreaSpecified_DefaultsToTheCallersSessionArea()
    {
        var sut = CreateSut();
        sut.Description = "New Project A";

        var result = await sut.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var item = Assert.Single(_lookups.Items[ProjectsTableId]);
        Assert.Equal("HISTO", item.Area);
    }

    [Fact]
    public async Task Add_ExplicitArea_OverridesTheSessionArea()
    {
        var sut = CreateSut();
        sut.UserArea = "OTHERVLA";
        sut.Description = "New Project B";

        await sut.OnPostAsync();

        var item = Assert.Single(_lookups.Items[ProjectsTableId]);
        Assert.Equal("OTHERVLA", item.Area);
    }

    [Fact]
    public async Task Add_BlankDescription_IsRejected()
    {
        var sut = CreateSut();
        sut.Description = "   ";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Enter a description.", sut.Errors);
    }

    [Fact]
    public async Task AddThenList_NewItemAppearsScopedToTheSameArea()
    {
        var add = CreateSut();
        add.UserArea = "HISTO";
        add.Description = "Project X";
        await add.OnPostAsync();

        var list = CreateSut();
        list.UserArea = "HISTO";
        await list.OnGetAsync();

        Assert.Contains(list.PagedEntries, i => i.Name == "Project X");
    }

    [Fact]
    public async Task List_DifferentArea_DoesNotSeeAnotherAreasItems()
    {
        var add = CreateSut();
        add.UserArea = "HISTO";
        add.Description = "Histo-only project";
        await add.OnPostAsync();

        var list = CreateSut();
        list.UserArea = "OTHERVLA";
        await list.OnGetAsync();

        Assert.DoesNotContain(list.PagedEntries, i => i.Name == "Histo-only project");
    }

    [Fact]
    public async Task Edit_ExistingItem_UpdatesDescriptionAndActiveFlag()
    {
        var add = CreateSut();
        add.UserArea = "HISTO";
        add.Description = "Original name";
        await add.OnPostAsync();
        var itemId = _lookups.Items[ProjectsTableId].Single().ID;

        var edit = CreateSut();
        edit.UserArea = "HISTO";
        edit.ItemId = itemId;
        await edit.OnGetAsync();
        Assert.Equal("Original name", edit.Description); // pre-populated from the existing row

        edit.Description = "Renamed";
        edit.Active = false;
        await edit.OnPostAsync();

        var saved = _lookups.Items[ProjectsTableId].Single();
        Assert.Equal("Renamed", saved.Name);
        Assert.False(saved.Active);
    }

    /// <summary>In-memory stand-in for the area-scoped pick-list stored procedures.</summary>
    private sealed class FakeAreaLookupService : ILookupService
    {
        private int _nextId = 1;
        public List<EditableLookup> Tables { get; } = [];
        public Dictionary<int, List<LookupItem>> Items { get; } = [];

        private List<LookupItem> ItemsFor(int tableId) => Items.TryGetValue(tableId, out var l) ? l : Items[tableId] = [];

        public Task<IReadOnlyList<EditableLookup>> ListEditableLookupsAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<EditableLookup>)Tables.ToList());

        public Task<IReadOnlyList<LookupItem>> GetUserAreaDataAsync(int tableId, string userArea, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)ItemsFor(tableId).Where(i => string.Equals(i.Area, userArea, StringComparison.OrdinalIgnoreCase)).ToList());

        public Task<bool> CreateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default)
        {
            var list = ItemsFor(tableId);
            list.Add(new LookupItem { ID = _nextId++, Name = item.Name, Area = item.Area, Active = item.Active });
            return Task.FromResult(true);
        }

        public Task<bool> UpdateLookupItemAsync(int tableId, LookupItem item, int userId, string? originalCode = null, CancellationToken ct = default)
        {
            var list = ItemsFor(tableId);
            var index = list.FindIndex(i => i.ID == item.ID);
            if (index < 0) return Task.FromResult(false);
            list[index] = new LookupItem { ID = item.ID, Name = item.Name, Area = list[index].Area, Active = item.Active };
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<LookupItem>> GetLookupDataAsync(int tableId, bool includeInactive = false, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetContactsByAreaAsync(string area, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetProjectsByAreaAsync(string area, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetUserGroupsAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetUserAreasAsync(bool includeInactive = false, CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetImportedTablesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetSpeciesLookupAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetHistologyTypesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetPremiumChargesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<LookupItem>> GetHistologyRefTypesAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());
        public Task<IReadOnlyList<SpeciesItem>> GetSpeciesItemsAsync(CancellationToken ct = default) => Task.FromResult((IReadOnlyList<SpeciesItem>)new List<SpeciesItem>());
        public Task<bool> AddSpeciesItemAsync(int speciesId, string species, string? commonName, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> UpdateSpeciesItemAsync(int speciesId, string species, string? commonName, int userId, CancellationToken ct = default) => Task.FromResult(true);
    }
}
