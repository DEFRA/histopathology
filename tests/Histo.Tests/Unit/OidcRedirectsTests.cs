using Histo.Web.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Histo.Tests.Unit;

public class OidcRedirectsTests
{
    private static readonly PathString Callback = "/signin-oidc";
    private static readonly PathString SignedOut = "/signout-callback-oidc";

    [Fact]
    public void ToIdentityProvider_UsesThePublicOriginAndForcesLogin()
    {
        var message = new OpenIdConnectMessage { RedirectUri = "https://origin.azurewebsites.net/signin-oidc" };

        OidcRedirects.ToIdentityProvider(message, Callback, "https://dev-cde.azure.defra.cloud/", forceLogin: true);

        Assert.Equal("https://dev-cde.azure.defra.cloud/signin-oidc", message.RedirectUri);
        Assert.Equal("login", message.Prompt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("__PUBLIC_ORIGIN__")]
    [InlineData("http://dev-cde.azure.defra.cloud")]
    public void ToIdentityProvider_MissingOrNonHttpsOrigin_KeepsTheRequestRedirectUri(string? publicOrigin)
    {
        var message = new OpenIdConnectMessage { RedirectUri = "https://localhost:7443/signin-oidc" };

        OidcRedirects.ToIdentityProvider(message, Callback, publicOrigin, forceLogin: false);

        Assert.Equal("https://localhost:7443/signin-oidc", message.RedirectUri);
        Assert.Null(message.Prompt);
    }

    [Fact]
    public void ToIdentityProviderForSignOut_UsesThePublicOrigin()
    {
        var message = new OpenIdConnectMessage();

        OidcRedirects.ToIdentityProviderForSignOut(message, SignedOut, "https://dev-cde.azure.defra.cloud");

        Assert.Equal("https://dev-cde.azure.defra.cloud/signout-callback-oidc", message.PostLogoutRedirectUri);
    }
}
