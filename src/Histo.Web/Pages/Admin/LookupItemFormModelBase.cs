using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>State and loading shared by the Add/Edit pick-list item pages.</summary>
public abstract class LookupItemFormModelBase : HistoPageModel, ILookupItemForm
{
    protected readonly ILookupService Lookups;

    protected LookupItemFormModelBase(ISessionService session, ILookupService lookups)
        : base(session) => Lookups = lookups;

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }

    /// <summary>Page this form was opened from (Pick list items page's own ReturnUrl), so the
    /// round-trip back to LookupItems can continue returning there.</summary>
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    [BindProperty] public string Code { get; set; } = string.Empty;
    [BindProperty] public string Description { get; set; } = string.Empty;
    [BindProperty] public bool Active { get; set; } = true;
    [BindProperty] public string? Area { get; set; }

    public string TableName { get; private set; } = string.Empty;
    public bool TableHasCodes { get; private set; }
    public bool ShowAreaColumn => LookupTableSchema.ShowAreaColumn(TableId);
    public IReadOnlyList<LookupItem> UserAreas { get; protected set; } = [];

    /// <summary>Field id → message, rendered via the shared clickable _ErrorSummary partial.</summary>
    public Dictionary<string, string> Errors { get; } = new();

    /// <summary>Not tied to a specific field, so shown separately (matches EditQualityDataTest's ConcurrencyError convention).</summary>
    public string? SaveError { get; protected set; }

    protected IReadOnlyList<LookupItem> ExistingItems { get; private set; } = [];

    protected void SetTitle(string title)
    {
        ViewData["Title"] = title;
        ViewData["PageTitle"] = title;
    }

    protected abstract bool DeriveTableHasCodes();

    /// <summary>Mirrors legacy PickListMaintenance.aspx Pager_RowSave: no two rows in the same table may share a Code (case-insensitive).</summary>
    protected virtual bool IsDuplicateCode(LookupItem item, string trimmedCode) =>
        string.Equals(item.Code, trimmedCode, StringComparison.OrdinalIgnoreCase);

    protected void Validate()
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
                if (ExistingItems.Any(i => IsDuplicateCode(i, trimmed)))
                    Errors["Code"] = "The code you have selected is already in use.";
            }
        }
    }

    protected async Task LoadTableAsync()
    {
        var tables = await Lookups.ListEditableLookupsAsync();
        TableName = tables.FirstOrDefault(t => t.ID == TableId)?.TableName ?? string.Empty;

        // Always load ALL items (active and inactive) so the duplicate-code check is exhaustive.
        ExistingItems = await Lookups.GetLookupDataAsync(TableId, includeInactive: true);
        TableHasCodes = DeriveTableHasCodes();

        if (ShowAreaColumn)
            UserAreas = await Lookups.GetUserAreasAsync();
    }
}
