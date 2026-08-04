using Histo.Administration.Models;
using Histo.Administration.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages.Admin;

/// <summary>Replaces <c>UserMaintenance.aspx</c>.</summary>
public class UserMaintenanceModel : HistoPageModel
{
    private readonly UserService _users;

    public UserMaintenanceModel(ISessionService session, UserService users)
        : base(session) => _users = users;

    /// <summary>
    /// When true, all users (including deactivated) are shown.
    /// Replaces the legacy <c>cbActive</c> "Show deactivated items" checkbox.
    /// </summary>
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public bool ShowInactive { get; set; }

    public IReadOnlyList<User> Users { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "User maintenance";
        ViewData["PageTitle"] = "User maintenance";
        StatusMessage = TempData["StatusMessage"] as string;
        var all = await _users.GetAllUsersAsync();
        Users = ShowInactive ? all : all.Where(u => u.Active).ToList();
    }
}
