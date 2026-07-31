using Histo.Administration.Models;
using Histo.Administration.Services;
using Histo.Web.Services;

namespace Histo.Web.Pages.Admin;

/// <summary>Replaces <c>UserMaintenance.aspx</c>.</summary>
public class UserMaintenanceModel : HistoPageModel
{
    private readonly UserService _users;

    public UserMaintenanceModel(ISessionService session, UserService users)
        : base(session) => _users = users;

    public IReadOnlyList<User> Users { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "User Maintenance";
        ViewData["PageTitle"] = "User Maintenance";
        StatusMessage = TempData["StatusMessage"] as string;
        Users = await _users.GetAllUsersAsync();
    }
}
