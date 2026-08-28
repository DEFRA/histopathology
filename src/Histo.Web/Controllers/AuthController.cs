using System.Security.Claims;
using Histo.Administration.Interfaces;
using Histo.Web.Auth;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Controllers;

/// <summary>
/// ITfoxtec SAML 2.0 protocol endpoints — SP-initiated sign-in, ACS, and SLO.
///
/// These are protocol endpoints (not UI pages) — the browser never renders them.
/// Sign-in: GET /Saml2/login → redirect to Entra ID.
/// ACS:     POST /Saml2/acs ← Entra ID posts SAML assertion here.
/// Logout:  GET /Saml2/logout → redirect to Entra ID SLO.
/// SLO:     POST /Saml2/slo ← Entra ID posts logout request/response here.
///
/// SECURITY:
///   - Assertion payloads must NOT be logged — no raw XML, no claim dumps.
///   - Log only the opaque NameIdentifier GUID.
///   - All config values come from appsettings.json — never hard-coded.
/// </summary>
[Route("Saml2")]
public sealed class AuthController : ControllerBase
{
    private readonly Saml2Configuration _saml2Config;
    private readonly IUserService _users;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        Saml2Configuration saml2Config,
        IUserService users,
        ILogger<AuthController> logger)
    {
        _saml2Config = saml2Config;
        _users       = users;
        _logger      = logger;
    }

    // ── SP-initiated login ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a SAML AuthnRequest and redirects the browser to Entra ID.
    /// The cookie LoginPath ("/Saml2/login") routes here when a ChallengeResult is returned.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var binding = new Saml2RedirectBinding();

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            binding.RelayState = returnUrl;

        return binding.Bind(new Saml2AuthnRequest(_saml2Config)).ToActionResult();
    }

    // ── Assertion Consumer Service ──────────────────────────────────────────

    /// <summary>
    /// Receives the SAML assertion POSTed by Entra ID, validates it, resolves the
    /// application user, bakes app claims into the auth cookie, then redirects to
    /// the requested page (RelayState) or the home page.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("Acs")]
    public async Task<IActionResult> AssertionConsumerService()
    {
        var binding            = new Saml2PostBinding();
        var saml2AuthnResponse = new Saml2AuthnResponse(_saml2Config);

        // ReadSamlResponse validates the assertion signature and status.
        binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);

        if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
        {
            // Log status only — never log assertion content or claim values.
            _logger.LogWarning("SAML assertion rejected by SP validation. Status: {Status}", saml2AuthnResponse.Status);
            return LocalRedirect("/AccessDenied");
        }

        var nameId = saml2AuthnResponse.ClaimsIdentity
            .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "(none)";
        _logger.LogInformation("SAML assertion validated. NameIdentifier: {NameId}", nameId);

        // Resolve application user and bake app claims into the cookie session.
        // CreateSessionAsync then calls HttpContext.SignInAsync internally.
        await saml2AuthnResponse.CreateSessionAsync(
            HttpContext,
            lifetime: null,
            isPersistent: false,
            claimsTransform: async (principal) =>
            {
                var email = ExtractEmail(saml2AuthnResponse.ClaimsIdentity);
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var user = await _users.ResolveUserByEmailAsync(email);
                    if (user is null)
                    {
                        _logger.LogWarning("ACS: authenticated user not found or inactive. NameIdentifier: {NameId}", nameId);
                        // Return principal without app claims; HistoPageModel will redirect to AccessDenied.
                        return principal;
                    }

                    var identity = (ClaimsIdentity)principal.Identity!;
                    identity.AddClaim(new Claim(AppClaimTypes.GroupName,  user.GroupName));
                    identity.AddClaim(new Claim(AppClaimTypes.UserDbId,   user.UserID.ToString()));
                    identity.AddClaim(new Claim(AppClaimTypes.GroupId,    user.GroupCode.ToString()));
                    identity.AddClaim(new Claim(AppClaimTypes.UserArea,   user.AreaName));
                    identity.AddClaim(new Claim(AppClaimTypes.UserAreaId, user.AreaCode.ToString()));
                }
                else
                {
                    _logger.LogWarning("ACS: assertion contained no email/UPN claim. NameIdentifier: {NameId}", nameId);
                }
                return principal;
            });

        var returnUrl = binding.RelayState;
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return LocalRedirect("/Index");
    }

    // ── SP-initiated Single Logout ──────────────────────────────────────────

    /// <summary>
    /// Sends a SAML LogoutRequest to Entra ID SLO endpoint and clears the local cookie.
    /// </summary>
    [Authorize]
    [HttpGet("Logout")]
    public IActionResult Logout()
    {
        var binding           = new Saml2RedirectBinding();
        var saml2LogoutRequest = new Saml2LogoutRequest(_saml2Config, User);

        // DeleteSession signs out the cookie and clears the auth ticket.
        saml2LogoutRequest.DeleteSession(HttpContext);

        return binding.Bind(saml2LogoutRequest).ToActionResult();
    }

    // ── SLO callback (IdP-initiated or response to SP-initiated SLO) ────────

    /// <summary>
    /// Receives a SAML LogoutRequest or LogoutResponse from Entra ID at the SloPath.
    /// Clears the local session and redirects to the home page.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("Logout")]
    public IActionResult SingleLogoutService()
    {
        var saml2LogoutRequest = new Saml2LogoutRequest(_saml2Config, User);
        saml2LogoutRequest.DeleteSession(HttpContext);
        return LocalRedirect("/Index");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string? ExtractEmail(ClaimsIdentity identity) =>
        identity.FindFirst(ClaimTypes.Email)?.Value
        ?? identity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
        ?? identity.FindFirst("preferred_username")?.Value;
}
