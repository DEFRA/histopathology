using System.Security.Claims;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Microsoft.AspNetCore.Authentication;

namespace Histo.Web.Auth;

/// <summary>
/// Resolves the authenticated Entra ID user against <c>tblUser</c> and appends
/// application-specific claims (<see cref="AppClaimTypes"/>) to the principal.
///
/// Called once per cookie deserialization by ASP.NET Core's authentication middleware.
/// The user's row is re-read on every request and any previously issued app claims are
/// replaced, so a Group or Area change takes effect on the next page load rather than
/// persisting until the auth cookie expires. Costs one indexed lookup per request, which
/// matches the legacy behaviour (<c>VLAHeader.GetUserDetails()</c> ran on every page load).
///
/// SECURITY:
///   - Never log raw assertion XML, email addresses, or full claim values.
///   - Log only the opaque Entra object ID — see Defra SDS Logging Standards.
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
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        var email = EntraIdClaims.Email(principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            // Log only the opaque object ID — never the email or display name.
            _logger.LogWarning(
                "Authenticated principal has no email/UPN claim. ObjectId: {ObjectId}",
                EntraIdClaims.ObjectId(principal));
            return principal; // no GroupName added → HistoPageModel redirects to AccessDenied
        }

        var user = await _users.ResolveUserByEmailAsync(email);
        if (user is null)
        {
            _logger.LogWarning(
                "Authenticated principal not found or inactive in application database. ObjectId: {ObjectId}",
                EntraIdClaims.ObjectId(principal));
            return principal; // no GroupName added → HistoPageModel redirects to AccessDenied
        }

        ApplyAppClaims((ClaimsIdentity)principal.Identity!, user);
        return principal;
    }

    /// <summary>Replaces any app claims on <paramref name="identity"/> with the values from <paramref name="user"/>.</summary>
    public static void ApplyAppClaims(ClaimsIdentity identity, User user)
    {
        // AddClaim appends and FindFirst returns the earliest match, so stale claims must go first.
        foreach (var stale in identity.FindAll(c => AppClaimTypes.All.Contains(c.Type)).ToList())
            identity.RemoveClaim(stale);

        identity.AddClaim(new Claim(AppClaimTypes.GroupName,  user.GroupName));
        identity.AddClaim(new Claim(AppClaimTypes.UserDbId,   user.UserID.ToString()));
        identity.AddClaim(new Claim(AppClaimTypes.GroupId,    user.GroupCode.ToString()));
        identity.AddClaim(new Claim(AppClaimTypes.UserArea,   user.AreaName));
        identity.AddClaim(new Claim(AppClaimTypes.UserAreaId, user.AreaCode.ToString()));
    }
}
