using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Histo.Web.Auth;

/// <summary>Adjusts the OIDC sign-in and sign-out requests sent to Entra ID.</summary>
public static class OidcRedirects
{
    public static void ToIdentityProvider(OpenIdConnectMessage message, PathString callbackPath, string? publicOrigin, bool forceLogin)
    {
        // Equivalent of SAML ForceAuthn: re-sign-in after a timeout must not be silent SSO.
        if (forceLogin)
            message.Prompt = OpenIdConnectPrompt.Login;

        // Behind the edge proxy Request.Host can be the App Service origin, which Entra would reject.
        if (TryGetOrigin(publicOrigin, out var origin))
            message.RedirectUri = origin + callbackPath;
    }

    public static void ToIdentityProviderForSignOut(OpenIdConnectMessage message, PathString signedOutCallbackPath, string? publicOrigin)
    {
        if (TryGetOrigin(publicOrigin, out var origin))
            message.PostLogoutRedirectUri = origin + signedOutCallbackPath;
    }

    private static bool TryGetOrigin(string? value, out string origin)
    {
        origin = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;
        origin = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }
}
