using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Area-scoped pick-list quick-add editor. Replaces the legacy <c>PickListUserArea.aspx</c>,
/// which is invoked from <c>BatchDetails.aspx</c>'s "New Project"/"New Contact" buttons so a
/// user can add (or edit) a Project/Contact pick-list entry scoped to their own — or a passed —
/// user area without leaving the batch workflow.
///
/// This is a genuinely distinct page from <c>PickListMaintenanceID.aspx</c> (see
/// <c>Admin/EditLookupItem</c>): that page is the unrestricted, Maintenance-group table editor
/// with a "select a table" drop-down and no area filtering; this page is locked to a single
/// table (passed in via the route) and filters rows to one user area (<c>LookupData.GetUserAreaData</c>).
///
/// Invoked from the "Manage project / contract codes" and "Manage pathologists" links on
/// <c>Batches/BatchDetails</c> (create mode) and <c>Batches/EditBatch</c>, which pass a
/// <see cref="ReturnUrl"/> so the user can resume the in-progress submission afterwards.
/// Also reachable directly, e.g. <c>/Admin/PickListUserArea/19?userArea=HISTO</c> for Projects.
/// </summary>
public class PickListUserAreaModel : GridPageModel
{
    private readonly ILookupService _lookups;

    public PickListUserAreaModel(ISessionService session, ILookupService lookups)
        : base(session) => _lookups = lookups;

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }
    [BindProperty(SupportsGet = true)] public string? UserArea { get; set; }
    [BindProperty(SupportsGet = true)] public int? ItemId { get; set; }

    /// <summary>
    /// Page the user came from (e.g. the in-progress Create/Edit Submission form). Carried through
    /// add/edit round-trips so the "Return to submission" button can send the user back.
    /// </summary>
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    /// <summary>Only ever redirect to a path inside this application — blocks open-redirect abuse.</summary>
    public string? SafeReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

    [BindProperty] public string Description { get; set; } = string.Empty;
    [BindProperty] public bool Active { get; set; } = true;

    public string TableName { get; private set; } = string.Empty;
    public IReadOnlyList<LookupItem> Items { get; private set; } = [];
    public List<string> Errors { get; } = [];
    public string? StatusMessage { get; private set; }

    public int TotalCount => Items.Count;

    public IReadOnlyList<LookupItem> PagedEntries =>
        (SortColumn switch
        {
            "Active" => SortDesc ? Items.OrderByDescending(i => i.Active) : Items.OrderBy(i => i.Active),
            _        => SortDesc ? Items.OrderByDescending(i => i.Name)   : Items.OrderBy(i => i.Name),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    private string EffectiveArea => string.IsNullOrEmpty(UserArea) ? Session.UserArea : UserArea;

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Pick List";
        ViewData["PageTitle"] = "Pick List";
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

        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Pick List";
        ViewData["PageTitle"] = "Pick List";
        await LoadTableNameAsync();
        await LoadItemsAsync();

        Validate();
        if (Errors.Count > 0)
        {
            PopulateGridViewData(TotalCount);
            return Page();
        }

        bool ok;
        if (ItemId is int id)
        {
            var item = new LookupItem { ID = id, Name = Description.Trim(), Active = Active };
            ok = await _lookups.UpdateLookupItemAsync(TableId, item, Session.UserID);
        }
        else
        {
            var item = new LookupItem { Name = Description.Trim(), Active = Active, Area = EffectiveArea };
            ok = await _lookups.CreateLookupItemAsync(TableId, item, Session.UserID);
        }

        if (!ok)
        {
            Errors.Add("Failed to save the pick list item. Please try again.");
            PopulateGridViewData(TotalCount);
            return Page();
        }

        TempData["StatusMessage"] = ItemId is int
            ? $"'{Description.Trim()}' was updated."
            : $"'{Description.Trim()}' was added.";
        return RedirectToPage("/Admin/PickListUserArea", new { tableId = TableId, userArea = UserArea, returnUrl = SafeReturnUrl });
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
        Items = await _lookups.GetUserAreaDataAsync(TableId, EffectiveArea);
    }
}
