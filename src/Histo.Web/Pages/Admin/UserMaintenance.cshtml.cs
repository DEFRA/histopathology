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
    /// When true, only active users are shown.
    /// Legacy <c>cbActive</c> defaulted to <c>Checked="True"</c> ("Show deactivated items")
    /// which meant ALL users were visible by default.
    /// This property therefore defaults to <c>false</c> (= show all) to match that behaviour.
    /// </summary>
    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public bool ShowActiveOnly { get; set; }

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
            Users = ShowActiveOnly ? all.Where(u => u.Active).ToList() : all.ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
        }
    }
}
