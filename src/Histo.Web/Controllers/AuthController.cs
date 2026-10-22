using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Controllers;

/// <summary>
/// Entry points into the Entra ID OpenID Connect flow. The protocol callbacks themselves
/// (/signin-oidc, /signout-oidc, /signout-callback-oidc) are handled by the OIDC middleware.
/// </summary>
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private const string HomePage = "/Index";

    /// <summary>The cookie LoginPath; sends the browser to Entra ID and back to <paramref name="returnUrl"/>.</summary>
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var redirectUri = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : HomePage;
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>Clears the local cookie and ends the Entra ID session.</summary>
    [Authorize]
    [HttpPost("signout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout() =>
        SignOut(
            new AuthenticationProperties { RedirectUri = HomePage },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
}
