using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Edit-user form. Restores the inline "Edit" row functionality from the legacy
/// <c>UserMaintenance.aspx</c> grid (<c>clsUser.SaveUserData</c> update path via
/// the <c>EditUser</c> stored procedure).
/// </summary>
public class EditUserModel : HistoPageModel
{
    private readonly IUserService _users;
    private readonly ILookupService _lookups;

    public EditUserModel(ISessionService session, IUserService users, ILookupService lookups)
        : base(session)
    {
        _users = users;
        _lookups = lookups;
    }

    [BindProperty(SupportsGet = true)] public int UserId { get; set; }
    [BindProperty] public string? NtLogin { get; set; } = string.Empty;
    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public int GroupCode { get; set; }
    [BindProperty] public int AreaCode { get; set; }
    [BindProperty] public bool Active { get; set; }

    /// <summary>Submission page to resume after the detour into user maintenance.</summary>
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    /// <summary>Only ever redirect to a path inside this application — blocks open-redirect abuse.</summary>
    public string? SafeReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

    public IReadOnlyList<LookupItem> Groups { get; private set; } = [];
    public IReadOnlyList<LookupItem> Areas { get; private set; } = [];

    /// <summary>
    /// SelectList for the Group dropdown — ensures the current <see cref="GroupCode"/>
    /// value is pre-selected when the form loads, even when option values are integers
    /// rendered from <c>LookupItem.ID</c> (which maps from the SP's <c>Code</c> column).
    /// </summary>
    public SelectList GroupSelectList => new(Groups, nameof(LookupItem.ID), nameof(LookupItem.Name), GroupCode);

    /// <summary>
    /// SelectList for the Area dropdown — ensures the current <see cref="AreaCode"/>
    /// value is pre-selected when the form loads.
    /// </summary>
    public SelectList AreaSelectList => new(Areas, nameof(LookupItem.ID), nameof(LookupItem.Name), AreaCode);

    /// <summary>Field id → message, rendered via the shared clickable _ErrorSummary partial.</summary>
    public Dictionary<string, string> Errors { get; } = new();

    /// <summary>Not tied to a specific field, so shown separately (matches EditQualityDataTest's ConcurrencyError convention).</summary>
    public string? SaveError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Edit user";
        ViewData["PageTitle"] = "Edit user";
        await LoadLookupsAsync();

        var user = (await _users.GetAllUsersAsync()).FirstOrDefault(u => u.UserID == UserId);
        if (user is null) return RedirectToPage("/Admin/UserMaintenance", new { returnUrl = SafeReturnUrl });

        NtLogin = user.NtLogin;
        Name = user.Name;
        Email = user.Email;
        GroupCode = user.GroupCode;
        AreaCode = user.AreaCode;
        Active = user.Active;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Edit user";
        ViewData["PageTitle"] = "Edit user";
        await LoadLookupsAsync();

        Validate();
        if (Errors.Count > 0) return Page();

        var user = new User
        {
            UserID = UserId,
            NtLogin = NtLogin?.Trim(),
            Name = Name.Trim(),
            Email = Email.Trim(),
            GroupCode = GroupCode,
            AreaCode = AreaCode,
            Active = Active,
        };

        var ok = await _users.UpdateUserAsync(user, Session.UserID);
        if (!ok)
        {
           SaveError= "Failed to save changes. Please try again.";
            return Page();
        }

        TempData["StatusMessage"] = $"User '{user.Name}' was updated.";
        return RedirectToPage("/Admin/UserMaintenance", new { returnUrl = SafeReturnUrl });
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) Errors["Name"] = "Enter the user's name.";
        if (string.IsNullOrWhiteSpace(Email)) Errors["Email"] = "Enter the user's email.";

        if (GroupCode <= 0) Errors["GroupCode"] = "Select a user group.";
        if (AreaCode <= 0) Errors["AreaCode"] = "Select a user area.";

        if (GroupCode > 0 && AreaCode > 0)
        {
            var groupName = Groups.FirstOrDefault(g => g.ID == GroupCode)?.Name;
            var areaName = Areas.FirstOrDefault(a => a.ID == AreaCode)?.Name;
            if (!GroupAreaMappingHelpers.IsAllowedCombination(groupName, areaName))
                Errors["AreaCode"] = "The selected area is not valid for the selected group.";
        }
    }

    private async Task LoadLookupsAsync()
    {
        Groups = await _lookups.GetUserGroupsAsync();
        Areas = await _lookups.GetUserAreasAsync();
    }
}
