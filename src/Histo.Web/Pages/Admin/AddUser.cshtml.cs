using Histo.Administration.Models;
using Histo.Administration.Services;
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
    private readonly UserService _users;
    private readonly LookupService _lookups;

    public AddUserModel(ISessionService session, UserService users, LookupService lookups)
        : base(session)
    {
        _users = users;
        _lookups = lookups;
    }

    [BindProperty] public string NtLogin { get; set; } = string.Empty;
    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public int GroupCode { get; set; }
    [BindProperty] public int AreaCode { get; set; }
    [BindProperty] public bool Active { get; set; } = true;

    public IReadOnlyList<LookupItem> Groups { get; private set; } = [];
    public IReadOnlyList<LookupItem> Areas { get; private set; } = [];
    public List<string> Errors { get; } = [];

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

        var user = new User
        {
            NtLogin   = NtLogin.Trim(),
            Name      = Name.Trim(),
            Email     = Email.Trim(),
            GroupCode = GroupCode,
            AreaCode  = AreaCode,
            Active    = Active,
        };

        var ok = await _users.CreateUserAsync(user);
        if (!ok)
        {
            Errors.Add("Failed to save the new user. Please try again.");
            return Page();
        }

        TempData["StatusMessage"] = $"User '{user.Name}' was added.";
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
