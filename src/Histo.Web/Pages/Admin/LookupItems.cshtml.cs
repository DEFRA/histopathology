using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Read-only list of a single editable lookup table's items, linked from
/// <c>Admin/PickListMaintenance</c>. Add and Edit are separate pages
/// (<see cref="AddLookupItemModel"/>, <see cref="EditLookupItemModel"/>) per GDS
/// "one thing per page" — this page previously also hosted a combined Add/Edit form.
/// </summary>
public class LookupItemsModel : GridPageModel
{
    private readonly ILookupService _lookups;

    public LookupItemsModel(ISessionService session, ILookupService lookups)
        : base(session) => _lookups = lookups;

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }
    [BindProperty(SupportsGet = true)] public bool ShowDeactivated { get; set; }

    public string TableName { get; private set; } = string.Empty;
    public IReadOnlyList<LookupItem> Items { get; private set; } = [];
    public bool TableHasCodes { get; private set; }
    public bool ShowAreaColumn => LookupTableSchema.ShowAreaColumn(TableId);
    public IReadOnlyDictionary<string, string> AreaNameById { get; private set; } = new Dictionary<string, string>();
    public string? StatusMessage { get; private set; }

    private IReadOnlyList<LookupItem> FilteredItems =>
        Items.Where(i => ShowDeactivated || i.Active).ToList();

    public int TotalCount => FilteredItems.Count;

    /// <summary>The value shown/sorted in the first column — Area name for area-scoped tables, Code (or ID) otherwise.</summary>
    private string FirstColumnValue(LookupItem i) =>
        ShowAreaColumn ? (AreaNameById.TryGetValue(i.Area ?? string.Empty, out var name) ? name : i.Area) ?? string.Empty
                       : i.Code ?? i.ID.ToString();

    public IReadOnlyList<LookupItem> PagedEntries
    {
        get
        {
            IOrderedEnumerable<LookupItem> sorted = SortColumn switch
            {
                "Code" => SortDesc ? FilteredItems.OrderByDescending(FirstColumnValue) : FilteredItems.OrderBy(FirstColumnValue),
                "Description" => SortDesc ? FilteredItems.OrderByDescending(i => i.Name) : FilteredItems.OrderBy(i => i.Name),
                "Active" => SortDesc ? FilteredItems.OrderByDescending(i => i.Active) : FilteredItems.OrderBy(i => i.Active),
                _ => FilteredItems.OrderBy(FirstColumnValue),
            };
            return sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
        }
    }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Pick list items";
        ViewData["PageTitle"] = "Pick list items";
        StatusMessage = TempData["StatusMessage"] as string;

        var tables = await _lookups.ListEditableLookupsAsync();
        TableName = tables.FirstOrDefault(t => t.ID == TableId)?.TableName ?? string.Empty;

        TableHasCodes = !ShowAreaColumn;
        if (ShowAreaColumn)
        {
            var userAreas = await _lookups.GetUserAreasAsync();
            AreaNameById = userAreas.ToDictionary(a => a.ID.ToString(), a => a.Name, StringComparer.OrdinalIgnoreCase);
        }

        PopulateGridViewData(TotalCount);
    }
}
