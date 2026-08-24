using Histo.Administration.Interfaces;
using Histo.Administration.Models;
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
    private readonly ILookupService _lookups;

    public EditLookupItemModel(ISessionService session, ILookupService lookups)
        : base(session) => _lookups = lookups;

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }
    [BindProperty(SupportsGet = true)] public bool ShowDeactivated { get; set; }
    // ID-keyed tables (Contacts 18, Projects 19): identify the row being edited.
    [BindProperty(SupportsGet = true)] public int? ItemId { get; set; }
    // Code-keyed tables (Archive Location 16, QC Code 14, etc.): identify the row by its
    // string code because these tables have no integer ID column.
    [BindProperty(SupportsGet = true)] public string? ItemCode { get; set; }

    [BindProperty] public string Code { get; set; } = string.Empty;
    // Round-tripped via hidden field on POST so UpdateLookupItemAsync receives @Original_Code.
    [BindProperty] public string OriginalCode { get; set; } = string.Empty;
    [BindProperty] public string Description { get; set; } = string.Empty;
    [BindProperty] public bool Active { get; set; } = true;

    public string TableName { get; private set; } = string.Empty;
    public IReadOnlyList<LookupItem> Items { get; private set; } = [];
    /// <summary>True when the loaded table's items carry a distinct string Code column (tables 14, 15, 16, etc.).
    /// False for ID-keyed tables (Contacts 18, Projects 19) which have no Code field.</summary>
    public bool TableHasCodes { get; private set; }
    /// <summary>Area-scoped tables (Contacts/Pathologists = 18, Projects = 19) show the Area column instead of Code.</summary>
    public bool ShowAreaColumn => TableId is 18 or 19;
    public List<string> Errors { get; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Edit pick list";
        ViewData["PageTitle"] = "Edit pick list";
        StatusMessage = TempData["StatusMessage"] as string;
        await LoadTableNameAsync();
        await LoadItemsAsync();

        // Code-keyed tables use ItemCode (string) to identify the row; ID-keyed tables use ItemId (int).
        if (TableHasCodes && ItemCode is not null)
        {
            var item = Items.FirstOrDefault(i => string.Equals(i.Code, ItemCode, StringComparison.OrdinalIgnoreCase));
            if (item is not null)
            {
                Description = item.Name;
                Active = item.Active;
                Code = item.Code ?? string.Empty;
                OriginalCode = item.Code ?? string.Empty;  // round-tripped for POST @Original_Code
            }
        }
        else if (ItemId is int id)
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
        bool isEdit = ItemId is int || (TableHasCodes && !string.IsNullOrEmpty(OriginalCode));

        if (TableHasCodes && !string.IsNullOrEmpty(OriginalCode))
        {
            // Edit a Code-keyed row: pass OriginalCode as @Original_Code, new Code as @Code.
            var item = new LookupItem { Name = Description.Trim(), Active = Active, Code = Code.Trim() };
            ok = await _lookups.UpdateLookupItemAsync(TableId, item, Session.UserID, originalCode: OriginalCode);
        }
        else if (ItemId is int id)
        {
            // Edit an ID-keyed row.
            var item = new LookupItem { ID = id, Name = Description.Trim(), Active = Active };
            ok = await _lookups.UpdateLookupItemAsync(TableId, item, Session.UserID);
        }
        else
        {
            // Add a new row. Area is only required for ID-keyed area-scoped tables
            // (Contacts/Projects — table IDs 18/19). Code-keyed tables (Archive Location 16,
            // QC Code 14, etc.) use BuildParamListCommon which expects Code/Description/IsActive
            // only — passing Area to those SPs causes "too many arguments".
            var item = new LookupItem { Name = Description.Trim(), Active = Active, Area = TableHasCodes ? null : Session.UserArea, Code = TableHasCodes ? Code.Trim() : null };
            ok = await _lookups.CreateLookupItemAsync(TableId, item, Session.UserID);
        }

        if (!ok)
        {
            Errors.Add("Failed to save the pick list item. Please try again.");
            return Page();
        }

        TempData["StatusMessage"] = isEdit
            ? $"'{Description.Trim()}' was updated."
            : $"'{Description.Trim()}' was added.";
        // After save, return to the list view (clear itemId so form resets to "Add" mode).
        return RedirectToPage("/Admin/EditLookupItem", new { tableId = TableId });
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Description)) Errors.Add("Enter a description.");

        if (TableHasCodes)
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                Errors.Add("Enter a code.");
            }
            else
            {
                var trimmed = Code.Trim();
                // Mirrors legacy PickListMaintenance.aspx Pager_RowSave: no two rows
                // in the same table may share the same Code (case-insensitive).
                // When editing, exclude the current row by its original code so users can
                // re-save without changing the code (or change it to a different value).
                bool duplicate = !string.IsNullOrEmpty(OriginalCode)
                    ? Items.Any(i => !string.Equals(i.Code, OriginalCode, StringComparison.OrdinalIgnoreCase)
                                  && string.Equals(i.Code, trimmed, StringComparison.OrdinalIgnoreCase))
                    : Items.Any(i => string.Equals(i.Code, trimmed, StringComparison.OrdinalIgnoreCase));
                if (duplicate)
                    Errors.Add("The code you have selected is already in use.");
            }
        }
    }

    private async Task LoadTableNameAsync()
    {
        var tables = await _lookups.ListEditableLookupsAsync();
        TableName = tables.FirstOrDefault(t => t.ID == TableId)?.TableName ?? string.Empty;
    }

    private async Task LoadItemsAsync()
    {
        // Always load ALL items (active and inactive) so that TableHasCodes is derived
        // from the full table shape and the duplicate-code check is exhaustive.
        // The view filters displayed rows based on ShowDeactivated.
        Items = await _lookups.GetLookupDataAsync(TableId, includeInactive: true);
        TableHasCodes = Items.Any(i => i.Code is not null);
    }
}
