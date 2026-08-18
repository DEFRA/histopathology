using Histo.Administration.Interfaces;
using Histo.Administration.Models;
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
    [BindProperty] public string NtLogin { get; set; } = string.Empty;
    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public int GroupCode { get; set; }
    [BindProperty] public int AreaCode { get; set; }
    [BindProperty] public bool Active { get; set; }

    public IReadOnlyList<LookupItem> Groups { get; private set; } = [];
    public IReadOnlyList<LookupItem> Areas { get; private set; } = [];
    public List<string> Errors { get; } = [];

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

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Edit user";
        ViewData["PageTitle"] = "Edit user";
        await LoadLookupsAsync();

        var user = (await _users.GetAllUsersAsync()).FirstOrDefault(u => u.UserID == UserId);
        if (user is null) return RedirectToPage("/Admin/UserMaintenance");

        NtLogin   = user.NtLogin;
        Name      = user.Name;
        Email     = user.Email;
        GroupCode = user.GroupCode;
        AreaCode  = user.AreaCode;
        Active    = user.Active;
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
            UserID    = UserId,
            NtLogin   = NtLogin.Trim(),
            Name      = Name.Trim(),
            Email     = Email.Trim(),
            GroupCode = GroupCode,
            AreaCode  = AreaCode,
            Active    = Active,
        };

        var ok = await _users.UpdateUserAsync(user, Session.UserID);
        if (!ok)
        {
            Errors.Add("Failed to save changes. Please try again.");
            return Page();
        }

        TempData["StatusMessage"] = $"User '{user.Name}' was updated.";
        return RedirectToPage("/Admin/UserMaintenance");
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(NtLogin)) Errors.Add("Enter the NT login.");
        if (string.IsNullOrWhiteSpace(Name)) Errors.Add("Enter the user's name.");
        if (GroupCode <= 0) Errors.Add("Select a user group.");
        if (AreaCode <= 0) Errors.Add("Select a user area.");
    }

    private async Task LoadLookupsAsync()
    {
        Groups = await _lookups.GetUserGroupsAsync();
        Areas  = await _lookups.GetUserAreasAsync();
    }
}
