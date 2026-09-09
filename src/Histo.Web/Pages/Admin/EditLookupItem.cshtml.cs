using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Dedicated "edit an existing pick-list item" page — split out from the previous combined
/// Add/Edit form (see <see cref="AddLookupItemModel"/> for the Add counterpart) per GDS
/// "one thing per page". Linked from <see cref="LookupItemsModel"/>'s per-row "Change" link.
/// </summary>
public class EditLookupItemModel : HistoPageModel
{
    private readonly ILookupService _lookups;

    public EditLookupItemModel(ISessionService session, ILookupService lookups)
        : base(session) => _lookups = lookups;

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }
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
    [BindProperty] public string? Area { get; set; }

    public string TableName { get; private set; } = string.Empty;
    public bool TableHasCodes { get; private set; }
    public bool ShowAreaColumn => LookupTableSchema.ShowAreaColumn(TableId);
    public IReadOnlyList<LookupItem> UserAreas { get; private set; } = [];

    /// <summary>Field id → message, rendered via the shared clickable _ErrorSummary partial.</summary>
    public Dictionary<string, string> Errors { get; } = new();

    /// <summary>Not tied to a specific field, so shown separately (matches EditQualityDataTest's ConcurrencyError convention).</summary>
    public string? SaveError { get; private set; }

    private IReadOnlyList<LookupItem> _existingItems = [];

    public async Task<IActionResult> OnGetAsync()
    {
        SetTitle();
        await LoadTableAsync();

        LookupItem? item = TableHasCodes && ItemCode is not null
            ? _existingItems.FirstOrDefault(i => string.Equals(i.Code, ItemCode, StringComparison.OrdinalIgnoreCase))
            : ItemId is int id ? _existingItems.FirstOrDefault(i => i.ID == id) : null;

        if (item is null) return RedirectToPage("/Admin/LookupItems", new { tableId = TableId });

        Description = item.Name;
        Active = item.Active;
        Code = item.Code ?? string.Empty;
        OriginalCode = item.Code ?? string.Empty;
        Area = item.Area;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitle();
        await LoadTableAsync();

        Validate();
        if (Errors.Count > 0) return Page();

        bool ok;
        if (TableHasCodes && !string.IsNullOrEmpty(OriginalCode))
        {
            // Edit a Code-keyed row: pass OriginalCode as @Original_Code, new Code as @Code.
            var item = new LookupItem { Name = Description.Trim(), Active = Active, Code = Code.Trim() };
            ok = await _lookups.UpdateLookupItemAsync(TableId, item, Session.UserID, originalCode: OriginalCode);
        }
        else
        {
            // Edit an ID-keyed row.
            var item = new LookupItem { ID = ItemId ?? 0, Name = Description.Trim(), Active = Active };
            ok = await _lookups.UpdateLookupItemAsync(TableId, item, Session.UserID);
        }

        if (!ok)
        {
            SaveError = "Failed to save the pick list item. Please try again.";
            return Page();
        }

        TempData["StatusMessage"] = $"'{Description.Trim()}' was updated.";
        return RedirectToPage("/Admin/LookupItems", new { tableId = TableId });
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Description)) Errors["Description"] = "Enter a description.";

        if (TableHasCodes)
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                Errors["Code"] = "Enter a code.";
            }
            else
            {
                var trimmed = Code.Trim();
                // Mirrors legacy PickListMaintenance.aspx Pager_RowSave: no two rows
                // in the same table may share the same Code (case-insensitive).
                // Exclude the current row by its original code so users can re-save without
                // changing the code (or change it to a different value).
                bool duplicate = _existingItems.Any(i =>
                    !string.Equals(i.Code, OriginalCode, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(i.Code, trimmed, StringComparison.OrdinalIgnoreCase));
                if (duplicate)
                    Errors["Code"] = "The code you have selected is already in use.";
            }
        }
    }

    private async Task LoadTableAsync()
    {
        var tables = await _lookups.ListEditableLookupsAsync();
        TableName = tables.FirstOrDefault(t => t.ID == TableId)?.TableName ?? string.Empty;

        // Always load ALL items (active and inactive) so TableHasCodes is derived from the
        // full table shape and the duplicate-code check is exhaustive.
        _existingItems = await _lookups.GetLookupDataAsync(TableId, includeInactive: true);
        TableHasCodes = LookupTableSchema.HasCodes(_existingItems);

        if (ShowAreaColumn)
            UserAreas = await _lookups.GetUserAreasAsync();
    }

    private void SetTitle()
    {
        ViewData["Title"] = "Edit pick list item";
        ViewData["PageTitle"] = "Edit pick list item";
    }
}

