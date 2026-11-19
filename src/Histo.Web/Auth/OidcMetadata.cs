using Microsoft.Extensions.Configuration;

namespace Histo.Web.Auth;

public static class OidcMetadata
{
    private const string DefaultInstance = "https://login.microsoftonline.com/";

    /// <summary>Tenant v2.0 discovery document scoped to the app, so app-specific signing keys are included.</summary>
    public static string AddressFor(IConfigurationSection azureAd)
    {
        var instance = azureAd["Instance"];
        var baseUrl = (string.IsNullOrWhiteSpace(instance) ? DefaultInstance : instance).TrimEnd('/');
        var tenantId = Uri.EscapeDataString(azureAd["TenantId"]!);
        var clientId = Uri.EscapeDataString(azureAd["ClientId"]!);
        return $"{baseUrl}/{tenantId}/v2.0/.well-known/openid-configuration?appid={clientId}";
    }
}
