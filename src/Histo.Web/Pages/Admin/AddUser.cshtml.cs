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
public class AddUserModel : HistoPageModel, IUserFormFields
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

    // Grid state carried through from the User maintenance list so saving can return to the same
    // ordering/filter, and the page the new row falls on can be worked out under that ordering.
    [BindProperty(SupportsGet = true)] public string? SortColumn { get; set; }
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public bool ShowDeactivated { get; set; } = true;

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
        if (Errors.Count == 0 && await EmailAlreadyExistsAsync(Email.Trim())) Errors["Email"] = "A user with this email already exists.";
        if (Errors.Count > 0) return Page();

        // NT login is no longer shown, entered, or derived in the UI — Entra ID email is now
        // the sole identity key (see HistopathologyClaimsTransformation). The legacy NtLogin
        // column is left NULL for new users rather than mapped from any other field — must be
        // a true NULL, not "", since IX_User_NTLogin is a filtered unique index (WHERE NTLogin
        // IS NOT NULL) that still enforces uniqueness across empty-string values.
        var user = new User
        {
            NtLogin   = null,
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
        // AddUser SP returns no identity, so re-read the row to tell the grid which page to open on.
        var created = (await _users.GetAllUsersAsync())
            .FirstOrDefault(u => string.Equals(u.Email, user.Email, StringComparison.OrdinalIgnoreCase));
        if (created is not null) TempData["FocusUserId"] = created.UserID;
        return RedirectToPage("/Admin/UserMaintenance", new { returnUrl = SafeReturnUrl, SortColumn, SortDesc, ShowDeactivated });
    }

    private void Validate()
    {
        UserFormValidator.Validate(Errors, Name, Email, GroupCode, AreaCode);
    }

    /// <summary>Mirrors the DB's unconditional (not Active-filtered) unique index on Email.</summary>
    private async Task<bool> EmailAlreadyExistsAsync(string email)
    {
        var users = await _users.GetAllUsersAsync();
        return users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    private async Task LoadLookupsAsync()
    {
        Groups = await _lookups.GetUserGroupsAsync();
        Areas  = await _lookups.GetUserAreasAsync();
    }
}
