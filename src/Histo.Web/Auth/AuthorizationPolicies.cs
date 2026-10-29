using Microsoft.AspNetCore.Authorization;

namespace Histo.Web.Auth;

public static class AuthorizationPolicies
{
    /// <summary>Applied to every endpoint that has no explicit [AllowAnonymous].</summary>
    public static AuthorizationPolicy FallbackRequireSignedIn() =>
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
}
