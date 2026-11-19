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

    /// <summary>Page this list was opened from (e.g. Create/Edit submission), so Back and the
    /// Add/Change round-trip return there instead of always going to Pick list maintenance.</summary>
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    /// <summary>Only ever redirect to a path inside this application — blocks open-redirect abuse.</summary>
    public string? SafeReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

    /// <summary>Defaults to checked on first load, matching legacy's "show all" default.</summary>
    [BindProperty(SupportsGet = true)] public bool ShowDeactivated { get; set; } = true;

    public string TableName { get; private set; } = string.Empty;
    public IReadOnlyList<LookupItem> Items { get; private set; } = [];
    public bool TableHasCodes { get; private set; }
    public bool ShowAreaColumn => LookupTableSchema.ShowAreaColumn(TableId);
    public IReadOnlyDictionary<string, string> AreaNameById { get; private set; } = new Dictionary<string, string>();
    public string? StatusMessage { get; private set; }

    private List<LookupItem> GetFilteredItems() =>
        Items.Where(i => ShowDeactivated || i.Active).ToList();

    public int TotalCount => GetFilteredItems().Count;

    /// <summary>The value shown/sorted in the first column — Area name for area-scoped tables, Code (or ID) otherwise.</summary>
    public string FirstColumnValue(LookupItem i)
    {
        if (!ShowAreaColumn)
            return i.Code ?? i.ID.ToString();

        var area = i.Area ?? string.Empty;
        return (AreaNameById.TryGetValue(area, out var name) ? name : i.Area) ?? string.Empty;
    }

    public IReadOnlyList<LookupItem> GetPagedEntries()
    {
        var filtered = GetFilteredItems();
        IOrderedEnumerable<LookupItem> sorted = SortColumn switch
        {
            "Code" => SortDesc ? filtered.OrderByDescending(FirstColumnValue) : filtered.OrderBy(FirstColumnValue),
            "Description" => SortDesc ? filtered.OrderByDescending(i => i.Name) : filtered.OrderBy(i => i.Name),
            "Active" => SortDesc ? filtered.OrderByDescending(i => i.Active) : filtered.OrderBy(i => i.Active),
            _ => filtered.OrderBy(FirstColumnValue),
        };
        return sorted.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
    }
    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Pick list items";
        ViewData["PageTitle"] = "Pick list items";
        StatusMessage = TempData["StatusMessage"] as string;

        var tables = await _lookups.ListEditableLookupsAsync();
        TableName = tables.FirstOrDefault(t => t.ID == TableId)?.TableName ?? string.Empty;

        // Always load ALL items (active and inactive) so the "show deactivated" filter can
        // reveal them without a second round trip; TableHasCodes is derived from the full set.
        Items = await _lookups.GetLookupDataAsync(TableId, includeInactive: true);
        TableHasCodes = LookupTableSchema.HasCodes(Items);

        if (ShowAreaColumn)
        {
            // includeInactive: true — Contacts/Projects already linked to a since-retired
            // area (Mouse Bioassay/Neuropath) must still display a readable Area name.
            var userAreas = await _lookups.GetUserAreasAsync(includeInactive: true);
            AreaNameById = userAreas.ToDictionary(a => a.ID.ToString(), a => a.Name, StringComparer.OrdinalIgnoreCase);
        }

        PopulateGridViewData(TotalCount);
    }
}
