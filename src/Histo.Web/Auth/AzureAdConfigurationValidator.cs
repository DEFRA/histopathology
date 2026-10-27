using Microsoft.Extensions.Configuration;

namespace Histo.Web.Auth;

public static class AzureAdConfigurationValidator
{
    private static readonly string[] RequiredKeys = ["TenantId", "ClientId", "ClientSecret"];

    public static void Validate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var azureAdSection = configuration.GetSection("AzureAd");

        foreach (var key in RequiredKeys)
        {
            var value = azureAdSection[key];
            if (string.IsNullOrWhiteSpace(value) || StartsWithPlaceholder(value))
            {
                throw new InvalidOperationException(
                    $"AzureAd:{key} is not configured. Set it in appsettings.Development.json (local) " +
                    "or via App Service configuration (AzureAd__" + key + ").");
            }
        }

        var publicOrigin = azureAdSection["PublicOrigin"];
        if (StartsWithPlaceholder(publicOrigin))
        {
            throw new InvalidOperationException(
                "AzureAd:PublicOrigin contains a placeholder value. Set it to the public https origin " +
                "for the live site or leave it blank for local development.");
        }
    }

    private static bool StartsWithPlaceholder(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith("__", StringComparison.Ordinal);
}
