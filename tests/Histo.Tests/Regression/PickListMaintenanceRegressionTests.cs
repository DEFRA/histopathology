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
/// Regression suite for the Pick list maintenance workflow (Admin/PickListMaintenance,
/// LookupItems, AddLookupItem, EditLookupItem).
///
/// Mirrors <c>UserMaintenanceRegressionTests</c>: chains Add → List → Edit → Deactivate against
/// a single shared <see cref="FakeLookupService"/> instance so a regression in how one page
/// model's write is read back by another is caught even without a real database (scenario
/// ADMIN-02 in .github/agents/histo-testing-agent.agent.md §5).
/// </summary>
public class PickListMaintenanceRegressionTests
{
    // Archive Location (table 16) — a real Code-keyed pick-list table (LookupTableSchema).
    private const int ArchiveLocationTableId = 16;

    private readonly FakeLookupService _lookups = new();
    private readonly Mock<ISessionService> _session = new();

    public PickListMaintenanceRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.UserArea).Returns("PATH");
        _lookups.Tables.Add(new EditableLookup { ID = ArchiveLocationTableId, TableName = "Archive Location" });
        _lookups.Tables.Add(new EditableLookup { ID = 99, TableName = "User Area" }); // must be filtered out of the overview grid
    }

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private PickListMaintenanceModel CreateOverview()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _lookups)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
        };
    }

    private LookupItemsModel CreateList()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _lookups)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    private AddLookupItemModel CreateAdd()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _lookups)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    private EditLookupItemModel CreateEdit()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _lookups)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    [Fact]
    public async Task Overview_ListsEditableTables_ButExcludesUserArea()
    {
        var overview = CreateOverview();

        await overview.OnGetAsync();

        Assert.Contains(overview.PagedEntries, t => t.TableName == "Archive Location");
        Assert.DoesNotContain(overview.PagedEntries, t => t.TableName == "User Area");
    }

    [Fact]
    public async Task AddEditDeactivate_FullWorkflow_EachStepReflectedInTheList()
    {
        // 1. Add a new pick-list item.
        var add = CreateAdd();
        add.TableId = ArchiveLocationTableId;
        add.Code = "RT1";
        add.Description = "Regression Test Store";

        var addResult = await add.OnPostAsync();
        var addRedirect = Assert.IsType<RedirectToPageResult>(addResult);
        Assert.Equal("/Admin/LookupItems", addRedirect.PageName);

        var created = _lookups.Items[ArchiveLocationTableId].Single(i => i.Code == "RT1");
        Assert.True(created.Active);

        // 2. The list shows the new, active item by default.
        var listAfterAdd = CreateList();
        listAfterAdd.TableId = ArchiveLocationTableId;
        await listAfterAdd.OnGetAsync();
        Assert.Contains(listAfterAdd.PagedEntries, i => i.Code == "RT1" && i.Name == "Regression Test Store");

        // 3. Edit the item's description.
        var edit = CreateEdit();
        edit.TableId = ArchiveLocationTableId;
        edit.ItemCode = "RT1";
        await edit.OnGetAsync();
        edit.Description = "Regression Test Store (renamed)";
        edit.Code = created.Code!;
        edit.OriginalCode = created.Code!;
        edit.Active = true;

        var editResult = await edit.OnPostAsync();
        Assert.IsType<RedirectToPageResult>(editResult);
        Assert.Equal("Regression Test Store (renamed)", _lookups.Items[ArchiveLocationTableId].Single(i => i.Code == "RT1").Name);

        // 4. Deactivate the item.
        var deactivate = CreateEdit();
        deactivate.TableId = ArchiveLocationTableId;
        deactivate.ItemCode = "RT1";
        await deactivate.OnGetAsync();
        deactivate.Active = false;

        await deactivate.OnPostAsync();
        Assert.False(_lookups.Items[ArchiveLocationTableId].Single(i => i.Code == "RT1").Active);

        // 5. Deactivated item disappears from the default (active-only) list view...
        var listActiveOnly = CreateList();
        listActiveOnly.TableId = ArchiveLocationTableId;
        listActiveOnly.ShowDeactivated = false;
        await listActiveOnly.OnGetAsync();
        Assert.DoesNotContain(listActiveOnly.PagedEntries, i => i.Code == "RT1");

        // ...but is still visible when "show deactivated" is on.
        var listShowDeactivated = CreateList();
        listShowDeactivated.TableId = ArchiveLocationTableId;
        listShowDeactivated.ShowDeactivated = true;
        await listShowDeactivated.OnGetAsync();
        Assert.Contains(listShowDeactivated.PagedEntries, i => i.Code == "RT1" && !i.Active);

        // 6. Reactivate.
        var reactivate = CreateEdit();
        reactivate.TableId = ArchiveLocationTableId;
        reactivate.ItemCode = "RT1";
        await reactivate.OnGetAsync();
        reactivate.Active = true;

        await reactivate.OnPostAsync();

        var listAfterReactivate = CreateList();
        listAfterReactivate.TableId = ArchiveLocationTableId;
        listAfterReactivate.ShowDeactivated = false;
        await listAfterReactivate.OnGetAsync();
        Assert.Contains(listAfterReactivate.PagedEntries, i => i.Code == "RT1" && i.Active);
    }

    [Fact]
    public async Task Add_DuplicateCodeCaseInsensitive_IsRejected()
    {
        var first = CreateAdd();
        first.TableId = ArchiveLocationTableId;
        first.Code = "DUP1";
        first.Description = "First store";
        await first.OnPostAsync();

        var second = CreateAdd();
        second.TableId = ArchiveLocationTableId;
        second.Code = "dup1"; // case-insensitive collision
        second.Description = "Impersonating store";

        var result = await second.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The code you have selected is already in use.", second.Errors["Code"]);
        Assert.Single(_lookups.Items[ArchiveLocationTableId]);
    }

    [Fact]
    public async Task Edit_RenamingCodeToAnotherItemsCode_IsRejectedAndOriginalRowUnchanged()
    {
        var add1 = CreateAdd();
        add1.TableId = ArchiveLocationTableId;
        add1.Code = "ONE";
        add1.Description = "Store One";
        await add1.OnPostAsync();

        var add2 = CreateAdd();
        add2.TableId = ArchiveLocationTableId;
        add2.Code = "TWO";
        add2.Description = "Store Two";
        await add2.OnPostAsync();

        var edit = CreateEdit();
        edit.TableId = ArchiveLocationTableId;
        edit.ItemCode = "TWO";
        await edit.OnGetAsync();
        edit.Code = "ONE"; // collides with Store One

        var result = await edit.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The code you have selected is already in use.", edit.Errors["Code"]);
        Assert.Equal("TWO", _lookups.Items[ArchiveLocationTableId].Single(i => i.Name == "Store Two").Code);
    }

    /// <summary>In-memory stand-in for the pick-list stored procedures, shared across page-model instances within a test.</summary>
    private sealed class FakeLookupService : ILookupService
    {
        public List<EditableLookup> Tables { get; } = [];
        public Dictionary<int, List<LookupItem>> Items { get; } = [];

        private List<LookupItem> ItemsFor(int tableId) =>
            Items.TryGetValue(tableId, out var list) ? list : Items[tableId] = [];

        public Task<IReadOnlyList<LookupItem>> GetLookupDataAsync(int tableId, bool includeInactive = false, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)ItemsFor(tableId).Where(i => includeInactive || i.Active).ToList());

        public Task<IReadOnlyList<EditableLookup>> ListEditableLookupsAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<EditableLookup>)Tables.ToList());

        public Task<bool> CreateLookupItemAsync(int tableId, LookupItem item, int userId, CancellationToken ct = default)
        {
            var list = ItemsFor(tableId);
            list.Add(new LookupItem { ID = list.Count + 1, Name = item.Name, Code = item.Code, Area = item.Area, Active = item.Active });
            return Task.FromResult(true);
        }

        public Task<bool> UpdateLookupItemAsync(int tableId, LookupItem item, int userId, string? originalCode = null, CancellationToken ct = default)
        {
            var list = ItemsFor(tableId);
            var index = originalCode is not null
                ? list.FindIndex(i => string.Equals(i.Code, originalCode, StringComparison.OrdinalIgnoreCase))
                : list.FindIndex(i => i.ID == item.ID);
            if (index < 0) return Task.FromResult(false);

            list[index] = new LookupItem { ID = list[index].ID, Name = item.Name, Code = item.Code ?? list[index].Code, Area = list[index].Area, Active = item.Active };
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<LookupItem>> GetUserAreasAsync(bool includeInactive = false, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());

        public Task<IReadOnlyList<LookupItem>> GetUserAreaDataAsync(int tableId, string userArea, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());

        public Task<IReadOnlyList<LookupItem>> GetContactsByAreaAsync(string area, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());

        public Task<IReadOnlyList<LookupItem>> GetProjectsByAreaAsync(string area, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());

        public Task<IReadOnlyList<LookupItem>> GetUserGroupsAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());

        public Task<IReadOnlyList<LookupItem>> GetImportedTablesAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());

        public Task<IReadOnlyList<LookupItem>> GetSpeciesLookupAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());

        public Task<IReadOnlyList<LookupItem>> GetHistologyTypesAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());

        public Task<IReadOnlyList<LookupItem>> GetPremiumChargesAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());

        public Task<IReadOnlyList<LookupItem>> GetHistologyRefTypesAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<LookupItem>)new List<LookupItem>());

        public Task<IReadOnlyList<SpeciesItem>> GetSpeciesItemsAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<SpeciesItem>)new List<SpeciesItem>());

        public Task<bool> AddSpeciesItemAsync(int speciesId, string species, string? commonName, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<bool> UpdateSpeciesItemAsync(int speciesId, string species, string? commonName, int userId, CancellationToken ct = default) =>
            Task.FromResult(true);
    }
}
