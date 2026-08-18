using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages.Admin;

/// <summary>Replaces <c>UserMaintenance.aspx</c>.</summary>
public class UserMaintenanceModel : HistoPageModel
{
    private readonly IUserService _users;

    public UserMaintenanceModel(ISessionService session, IUserService users)
        : base(session) => _users = users;

    /// <summary>
    /// When true, deactivated (inactive) users are included in the list alongside active users.
    /// Matches the legacy <c>cbActive</c> ("Show deactivated items") checkbox behaviour:
    /// default unchecked = show active users only; checked = show all users.
    /// </summary>
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public bool ShowDeactivated { get; set; }

    public IReadOnlyList<User> Users { get; private set; } = [];
    public string? StatusMessage { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int TotalFromDb { get; private set; }

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
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
        }
    }
}
