using System.Security.Claims;
using Microsoft.Identity.Web;

namespace Histo.Web.Auth;

/// <summary>Reads the Entra ID claims the app relies on, whether or not inbound claim mapping is on.</summary>
public static class EntraIdClaims
{
    // Email first, then UPN — matches both internal and B2B guest accounts.
    public static string? Email(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email)
        ?? principal.FindFirstValue("email")
        ?? principal.FindFirstValue("preferred_username");

    /// <summary>The user's Entra object ID — the only identity value that may be logged.</summary>
    public static string ObjectId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimConstants.ObjectId)
        ?? principal.FindFirstValue(ClaimConstants.Oid)
        ?? "(none)";
}
