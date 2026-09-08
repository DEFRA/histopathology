using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Dedicated "add a new pick-list item" page — split out from the previous combined
/// Add/Edit form on <c>EditLookupItem.cshtml</c> per GDS "one thing per page".
/// Linked from <see cref="LookupItemsModel"/>'s "Add item" button.
/// </summary>
public class AddLookupItemModel : HistoPageModel
{
    private readonly ILookupService _lookups;

    public AddLookupItemModel(ISessionService session, ILookupService lookups)
        : base(session) => _lookups = lookups;

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }

    [BindProperty] public string Code { get; set; } = string.Empty;
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

    public async Task OnGetAsync()
    {
        SetTitle();
        Active = true;
        await LoadTableAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitle();
        await LoadTableAsync();

        Validate();
        if (Errors.Count > 0) return Page();

        // Area is only required for ID-keyed area-scoped tables (Contacts/Projects — 18/19).
        // Code-keyed tables use BuildParamListCommon which expects Code/Description/IsActive
        // only — passing Area to those SPs causes "too many arguments".
        var item = new LookupItem
        {
            Name = Description.Trim(),
            Active = Active,
            Area = TableHasCodes ? null : (Area ?? Session.UserArea),
            Code = TableHasCodes ? Code.Trim() : null,
        };
        var ok = await _lookups.CreateLookupItemAsync(TableId, item, Session.UserID);

        if (!ok)
        {
            SaveError = "Failed to save the pick list item. Please try again.";
            return Page();
        }

        TempData["StatusMessage"] = $"'{Description.Trim()}' was added.";
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
                // Mirrors legacy PickListMaintenance.aspx Pager_RowSave: no two rows in the
                // same table may share the same Code (case-insensitive).
                if (_existingItems.Any(i => string.Equals(i.Code, trimmed, StringComparison.OrdinalIgnoreCase)))
                    Errors["Code"] = "The code you have selected is already in use.";
            }
        }
    }

    private async Task LoadTableAsync()
    {
        var tables = await _lookups.ListEditableLookupsAsync();
        TableName = tables.FirstOrDefault(t => t.ID == TableId)?.TableName ?? string.Empty;

        _existingItems = await _lookups.GetLookupDataAsync(TableId, includeInactive: true);
        TableHasCodes = LookupTableSchema.HasCodes(_existingItems);

        if (ShowAreaColumn)
            UserAreas = await _lookups.GetUserAreasAsync();
    }

    private void SetTitle()
    {
        ViewData["Title"] = "Add pick list item";
        ViewData["PageTitle"] = "Add pick list item";
    }
}
