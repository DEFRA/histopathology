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
public class EditLookupItemModel : LookupItemFormModelBase
{
    private const string PageTitle = "Edit pick list item";

    public EditLookupItemModel(ISessionService session, ILookupService lookups)
        : base(session, lookups) { }
    // ID-keyed tables (Contacts 18, Projects 19): identify the row being edited.
    [BindProperty(SupportsGet = true)] public int? ItemId { get; set; }
    // Code-keyed tables (Archive Location 16, QC Code 14, etc.): identify the row by its
    // string code because these tables have no integer ID column.
    [BindProperty(SupportsGet = true)] public string? ItemCode { get; set; }

    // Round-tripped via hidden field on POST so UpdateLookupItemAsync receives @Original_Code.
    [BindProperty] public string OriginalCode { get; set; } = string.Empty;

    protected override bool DeriveTableHasCodes() => LookupTableSchema.HasCodes(ExistingItems);

    // Exclude the current row by its original code so users can re-save without
    // changing the code (or change it to a different value).
    protected override bool IsDuplicateCode(LookupItem item, string trimmedCode) =>
        !string.Equals(item.Code, OriginalCode, StringComparison.OrdinalIgnoreCase)
        && base.IsDuplicateCode(item, trimmedCode);

    private LookupItem? FindExistingItem()
    {
        if (TableHasCodes && ItemCode is not null)
            return ExistingItems.FirstOrDefault(i => string.Equals(i.Code, ItemCode, StringComparison.OrdinalIgnoreCase));

        return ItemId is int id ? ExistingItems.FirstOrDefault(i => i.ID == id) : null;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        SetTitle(PageTitle);
        await LoadTableAsync();

        var item = FindExistingItem();

        if (item is null) return RedirectToPage("/Admin/LookupItems", new { tableId = TableId, ReturnUrl });

        Description = item.Name;
        Active = item.Active;
        Code = item.Code ?? string.Empty;
        OriginalCode = item.Code ?? string.Empty;
        Area = item.Area;
        await EnsureCurrentAreaVisibleAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitle(PageTitle);
        await LoadTableAsync();
        await EnsureCurrentAreaVisibleAsync();

        Validate();
        if (Errors.Count > 0) return Page();

        bool ok;
        if (TableHasCodes && !string.IsNullOrEmpty(OriginalCode))
        {
            // Edit a Code-keyed row: pass OriginalCode as @Original_Code, new Code as @Code.
            var item = new LookupItem { Name = Description.Trim(), Active = Active, Code = Code.Trim() };
            ok = await Lookups.UpdateLookupItemAsync(TableId, item, Session.UserID, originalCode: OriginalCode);
        }
        else
        {
            // Edit an ID-keyed row.
            var item = new LookupItem { ID = ItemId ?? 0, Name = Description.Trim(), Active = Active };
            ok = await Lookups.UpdateLookupItemAsync(TableId, item, Session.UserID);
        }

        if (!ok)
        {
            SaveError = "Failed to save the pick list item. Please try again.";
            return Page();
        }

        TempData["StatusMessage"] = $"'{Description.Trim()}' was updated.";
        return RedirectToPage("/Admin/LookupItems", new { tableId = TableId, ReturnUrl });
    }

    /// <summary>
    /// If this item's stored Area isn't in the active list (a since-retired area like Mouse
    /// Bioassay/Neuropath), fetches its name and adds it to <see cref="UserAreas"/> so the
    /// dropdown displays/preserves the real value instead of silently defaulting away from it.
    /// </summary>
    private async Task EnsureCurrentAreaVisibleAsync()
    {
        if (!ShowAreaColumn || string.IsNullOrEmpty(Area) || UserAreas.Any(a => a.ID.ToString() == Area)) return;

        var allAreas = await Lookups.GetUserAreasAsync(includeInactive: true);
        var retired = allAreas.FirstOrDefault(a => a.ID.ToString() == Area);
        if (retired is not null) UserAreas = [.. UserAreas, retired];
    }
}

