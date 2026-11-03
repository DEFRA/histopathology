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
public class AddLookupItemModel : LookupItemFormModelBase
{
    private const string PageTitle = "Add pick list item";

    public AddLookupItemModel(ISessionService session, ILookupService lookups)
        : base(session, lookups) { }

    protected override bool DeriveTableHasCodes() => !ShowAreaColumn;

    public async Task OnGetAsync()
    {
        SetTitle(PageTitle);
        Active = true;
        await LoadTableAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitle(PageTitle);
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
        var ok = await Lookups.CreateLookupItemAsync(TableId, item, Session.UserID);

        if (!ok)
        {
            SaveError = "Failed to save the pick list item. Please try again.";
            return Page();
        }

        TempData["StatusMessage"] = $"'{Description.Trim()}' was added.";
        return RedirectToPage("/Admin/LookupItems", new { tableId = TableId, ReturnUrl });
    }
}
