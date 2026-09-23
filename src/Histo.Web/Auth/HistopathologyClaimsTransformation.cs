using System.Security.Claims;
using Histo.Administration.Interfaces;
using Microsoft.AspNetCore.Authentication;

namespace Histo.Web.Auth;

/// <summary>
/// Resolves the authenticated Entra ID user against <c>tblUser</c> and appends
/// application-specific claims (<see cref="AppClaimTypes"/>) to the principal.
///
/// Called once per cookie deserialization by ASP.NET Core's authentication middleware.
/// The idempotency guard (HasClaim check) prevents repeated DB lookups because
/// the app claims are baked into the auth cookie at ACS time by
/// <see cref="Controllers.AuthController.AssertionConsumerService"/>.
///
/// SECURITY:
///   - Never log raw assertion XML, email addresses, or full claim values.
///   - Log only the opaque NameIdentifier GUID — see Defra SDS Logging Standards.
///   - UK GDPR: claim values (name, email) must not be stored beyond session lifetime
///     without a documented lawful basis.
/// </summary>
public sealed class HistopathologyClaimsTransformation : IClaimsTransformation
{
    private readonly IUserService _users;
    private readonly ILogger<HistopathologyClaimsTransformation> _logger;

    public HistopathologyClaimsTransformation(
        IUserService users,
        ILogger<HistopathologyClaimsTransformation> logger)
    {
        _users  = users;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Idempotent — if app claims are already present (baked in at ACS), return immediately.
        if (principal.HasClaim(c => c.Type == AppClaimTypes.GroupName))
            return principal;

        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        var email = ExtractEmail(principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            // Log only the opaque NameIdentifier — never the email or display name.
            _logger.LogWarning(
                "Authenticated principal has no email/UPN claim. NameIdentifier: {NameId}",
                principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "(none)");
            return principal; // no GroupName added → HistoPageModel redirects to AccessDenied
        }

        var user = await _users.ResolveUserByEmailAsync(email);
        if (user is null)
        {
            _logger.LogWarning(
                "Authenticated principal not found or inactive in application database. NameIdentifier: {NameId}",
                principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "(none)");
            return principal; // no GroupName added → HistoPageModel redirects to AccessDenied
        }

        var identity = (ClaimsIdentity)principal.Identity!;
        identity.AddClaim(new Claim(AppClaimTypes.GroupName,  user.GroupName));
        identity.AddClaim(new Claim(AppClaimTypes.UserDbId,   user.UserID.ToString()));
        identity.AddClaim(new Claim(AppClaimTypes.GroupId,    user.GroupCode.ToString()));
        identity.AddClaim(new Claim(AppClaimTypes.UserArea,   user.AreaName));
        identity.AddClaim(new Claim(AppClaimTypes.UserAreaId, user.AreaCode.ToString()));

        return principal;
    }

    // Try email claim first, then UPN — matches both internal and B2B guest accounts.
    private static string? ExtractEmail(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email)
        ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
        ?? principal.FindFirstValue("preferred_username");
}
