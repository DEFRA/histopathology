using Histo.Administration.Models;
using Histo.Administration.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Per-table pick-list item editor. Replaces the legacy <c>PickListMaintenanceID.aspx</c>
/// inline Add/Edit grid, linked from <c>Admin/PickListMaintenance</c> — shows the rows for one
/// editable lookup table and lets the same form either add a new row (no <see cref="ItemId"/>)
/// or edit an existing one (<see cref="ItemId"/> supplied via the "Edit" link on a row).
/// </summary>
public class EditLookupItemModel : HistoPageModel
{
    private readonly LookupService _lookups;

    public EditLookupItemModel(ISessionService session, LookupService lookups)
        : base(session) => _lookups = lookups;

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }
    [BindProperty(SupportsGet = true)] public int? ItemId { get; set; }

    [BindProperty] public string Description { get; set; } = string.Empty;
    [BindProperty] public bool Active { get; set; } = true;

    public string TableName { get; private set; } = string.Empty;
    public IReadOnlyList<LookupItem> Items { get; private set; } = [];
    public List<string> Errors { get; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Edit pick list";
        ViewData["PageTitle"] = "Edit pick list";
        StatusMessage = TempData["StatusMessage"] as string;
        await LoadTableNameAsync();
        await LoadItemsAsync();

        if (ItemId is int id)
        {
            var item = Items.FirstOrDefault(i => i.ID == id);
            if (item is not null)
            {
                Description = item.Name;
                Active = item.Active;
            }
        }
        else
        {
            Active = true;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Edit pick list";
        ViewData["PageTitle"] = "Edit pick list";
        await LoadTableNameAsync();
        await LoadItemsAsync();

        Validate();
        if (Errors.Count > 0) return Page();

        bool ok;
        if (ItemId is int id)
        {
            var item = new LookupItem { ID = id, Name = Description.Trim(), Active = Active };
            ok = await _lookups.UpdateLookupItemAsync(TableId, item, Session.UserID);
        }
        else
        {
            var item = new LookupItem { Name = Description.Trim(), Active = Active };
            ok = await _lookups.CreateLookupItemAsync(TableId, item, Session.UserID);
        }

        if (!ok)
        {
            Errors.Add("Failed to save the pick list item. Please try again.");
            return Page();
        }

        TempData["StatusMessage"] = ItemId is int
            ? $"'{Description.Trim()}' was updated."
            : $"'{Description.Trim()}' was added.";
        return RedirectToPage("/Admin/EditLookupItem", new { tableId = TableId });
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Description)) Errors.Add("Enter a description.");
    }

    private async Task LoadTableNameAsync()
    {
        var tables = await _lookups.ListEditableLookupsAsync();
        TableName = tables.FirstOrDefault(t => t.ID == TableId)?.TableName ?? string.Empty;
    }

    private async Task LoadItemsAsync()
    {
        Items = await _lookups.GetLookupDataAsync(TableId, includeInactive: true);
    }
}
