using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Add-user form. Restores the "Add new" row functionality from the legacy
/// <c>UserMaintenance.aspx</c> inline grid (<c>Pager.AllowAddNew</c> /
/// <c>clsUser.SaveUserData</c> insert path via the <c>AddUser</c> stored procedure).
/// </summary>
public class AddUserModel : HistoPageModel
{
    private readonly IUserService _users;
    private readonly ILookupService _lookups;

    public AddUserModel(ISessionService session, IUserService users, ILookupService lookups)
        : base(session)
    {
        _users = users;
        _lookups = lookups;
    }

    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public int GroupCode { get; set; }
    [BindProperty] public int AreaCode { get; set; }
    [BindProperty] public bool Active { get; set; } = true;

    /// <summary>Submission page to resume after the detour into user maintenance.</summary>
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    /// <summary>Only ever redirect to a path inside this application — blocks open-redirect abuse.</summary>
    public string? SafeReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

    public IReadOnlyList<LookupItem> Groups { get; private set; } = [];
    public IReadOnlyList<LookupItem> Areas { get; private set; } = [];

    /// <summary>Field id → message, rendered via the shared clickable _ErrorSummary partial.</summary>
    public Dictionary<string, string> Errors { get; } = new();

    /// <summary>Not tied to a specific field, so shown separately (matches EditQualityDataTest's ConcurrencyError convention).</summary>
    public string? SaveError { get; private set; }


    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Add user";
        ViewData["PageTitle"] = "Add user";
        Active = true;
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Add user";
        ViewData["PageTitle"] = "Add user";
        await LoadLookupsAsync();

        Validate();
        if (Errors.Count > 0) return Page();

        // NT login is no longer shown, entered, or derived in the UI — Entra ID email is now
        // the sole identity key (see HistopathologyClaimsTransformation). The legacy NtLogin
        // column is left blank for new users rather than mapped from any other field.
        var user = new User
        {
            NtLogin   = string.Empty,
            Name      = Name.Trim(),
            Email     = Email.Trim(),
            GroupCode = GroupCode,
            AreaCode  = AreaCode,
            Active    = Active,
        };

        var ok = await _users.CreateUserAsync(user);
        if (!ok)
        {
            SaveError = "Failed to save the new user. Please try again.";
            return Page();
        }

        TempData["StatusMessage"] = $"User '{user.Name}' was added.";
        return RedirectToPage("/Admin/UserMaintenance", new { returnUrl = SafeReturnUrl });
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) Errors["Name"] = "Enter the user's name.";
        else if (Name.Length > 35) Errors["Name"] = "Name must be 35 characters or less.";

        if (string.IsNullOrWhiteSpace(Email)) Errors["Email"] = "Enter the user's email.";
        else if (Email.Length > 60) Errors["Email"] = "Email must be 60 characters or less.";

        if (GroupCode <= 0) Errors["GroupCode"] = "Select a user group.";
        if (AreaCode <= 0) Errors["AreaCode"] = "Select a user area.";
    }

    private async Task LoadLookupsAsync()
    {
        Groups = await _lookups.GetUserGroupsAsync();
        Areas  = await _lookups.GetUserAreasAsync();
    }
}
