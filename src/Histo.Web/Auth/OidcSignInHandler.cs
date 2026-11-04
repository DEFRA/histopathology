using System.Security.Claims;
using Histo.Administration.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Histo.Web.Auth;

/// <summary>
/// Runs once per Entra ID sign-in (OIDC <c>OnTokenValidated</c>): resolves the <c>tblUser</c> row,
/// bakes the app claims into the auth cookie and issues the single-session stamp.
/// A user with no active row gets no app claims, so <see cref="Pages.HistoPageModel"/> sends them to AccessDenied.
/// </summary>
public sealed class OidcSignInHandler
{
    private readonly IUserService _users;
    private readonly IDistributedCache _cache;
    private readonly ILogger<OidcSignInHandler> _logger;

    public OidcSignInHandler(IUserService users, IDistributedCache cache, ILogger<OidcSignInHandler> logger)
    {
        _users  = users;
        _cache  = cache;
        _logger = logger;
    }

    public async Task OnTokenValidatedAsync(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
            return;

        var objectId = EntraIdClaims.ObjectId(principal);
        var email = EntraIdClaims.Email(principal);
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("OIDC sign-in: ID token contained no email/UPN claim. ObjectId: {ObjectId}", objectId);
            return;
        }

        var user = await _users.ResolveUserByEmailAsync(email);
        if (user is null)
        {
            _logger.LogWarning("OIDC sign-in: user not found or inactive. ObjectId: {ObjectId}", objectId);
            return;
        }

        HistopathologyClaimsTransformation.ApplyAppClaims(identity, user);
        identity.AddClaim(SingleSessionPolicy.StampClaim(
            await SingleSessionPolicy.IssueStampAsync(_cache, user.UserID.ToString())));

        _logger.LogInformation("OIDC sign-in completed. ObjectId: {ObjectId}", objectId);
    }
}
