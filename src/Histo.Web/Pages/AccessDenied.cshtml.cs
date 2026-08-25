using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages;

/// <summary>
/// Displayed when an Entra ID-authenticated user has no active row in tblUser.
/// Does not inherit HistoPageModel — intentionally outside the identity-
/// resolution pipeline so it cannot loop back into the same check.
/// </summary>
public class AccessDeniedModel : PageModel
{
    public string? Email { get; private set; }

    public void OnGet()
    {
        Email = HttpContext.User.FindFirst(ClaimTypes.Email)?.Value
             ?? HttpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
             ?? HttpContext.User.FindFirst("preferred_username")?.Value;
    }
}