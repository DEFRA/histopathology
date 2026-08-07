using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages;

/// <summary>
/// Displayed when the current Windows login is not found in the database
/// or the account is inactive. Replaces the legacy unauthorized.htm redirect.
/// Does not inherit HistoPageModel — intentionally outside the identity-
/// resolution pipeline so it cannot loop back into the same check.
/// </summary>
public class AccessDeniedModel : PageModel
{
    public string Login { get; private set; } = string.Empty;

    public void OnGet()
    {
        Login = HttpContext.User.Identity?.Name is { Length: > 0 } name
            ? name
            : $@"{Environment.UserDomainName}\{Environment.UserName}";
    }
}