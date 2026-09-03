using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages.Admin;

/// <summary>Replaces <c>UserMaintenance.aspx</c>.</summary>
public class UserMaintenanceModel : HistoPageModel
{
    private readonly IUserService _users;
    private readonly ILookupService _lookups;

    public UserMaintenanceModel(ISessionService session, IUserService users, ILookupService lookups)
        : base(session)
    {
        _users = users;
        _lookups = lookups;
    }

    /// <summary>
    /// When true, deactivated (inactive) users are included in the list alongside active users.
    /// Matches the legacy <c>cbActive</c> ("Show deactivated items") checkbox behaviour:
    /// legacy default = all users shown (checked); unchecked = active users only.
    /// Default <c>true</c> so first visit matches legacy (no filtering applied).
    /// </summary>
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public bool ShowDeactivated { get; set; } = true;

    /// <summary>
    /// Page the user came from (e.g. the in-progress Create/Edit Submission form), so the
    /// "Return to submission" button can send them back without losing entered data.
    /// </summary>
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>Only ever redirect to a path inside this application — blocks open-redirect abuse.</summary>
    public string? SafeReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

    public IReadOnlyList<User> Users { get; private set; } = [];
    public string? StatusMessage { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int TotalFromDb { get; private set; }

    /// <summary>
    /// Group code → display name fallback map. Populated from <c>GetluUserGroup</c>
    /// when the <c>GetUsers</c> SP does not return <c>GroupName</c> inline.
    /// </summary>
    public IReadOnlyDictionary<int, string> GroupNames { get; private set; }
        = new Dictionary<int, string>();

    /// <summary>
    /// Area code → display name fallback map. Populated from <c>GetluUserArea</c>
    /// when the <c>GetUsers</c> SP does not return <c>AreaName</c> inline.
    /// </summary>
    public IReadOnlyDictionary<int, string> AreaNames { get; private set; }
        = new Dictionary<int, string>();

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "User maintenance";
        ViewData["PageTitle"] = "User maintenance";
        StatusMessage = TempData["StatusMessage"] as string;
        try
        {
            var all = await _users.GetAllUsersAsync();
            TotalFromDb = all.Count;
            Users = ShowDeactivated ? all.ToList() : all.Where(u => u.Active).ToList();

            // Load lookup names as a fallback in case the GetUsers SP does not return
            // GroupName / AreaName columns (older SP versions return only integer codes).
            var groupsTask = _lookups.GetUserGroupsAsync();
            var areasTask = _lookups.GetUserAreasAsync();
            await Task.WhenAll(groupsTask, areasTask);

            GroupNames = groupsTask.Result.ToDictionary(g => g.ID, g => g.Name);
            AreaNames = areasTask.Result.ToDictionary(a => a.ID, a => a.Name);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// Returns the display name for a user group code.
    /// Uses the value returned by <c>GetUsers</c> SP when available;
    /// falls back to the lookup dictionary when that column is empty.
    /// </summary>
    public string ResolveGroupName(User u)
        => !string.IsNullOrWhiteSpace(u.GroupName) ? u.GroupName
           : GroupNames.TryGetValue(u.GroupCode, out var n) ? n
           : u.GroupCode.ToString();

    /// <summary>
    /// Returns the display name for a user area code.
    /// Uses the value returned by <c>GetUsers</c> SP when available;
    /// falls back to the lookup dictionary when that column is empty.
    /// </summary>
    public string ResolveAreaName(User u)
        => !string.IsNullOrWhiteSpace(u.AreaName) ? u.AreaName
           : AreaNames.TryGetValue(u.AreaCode, out var n) ? n
           : u.AreaCode.ToString();
}
