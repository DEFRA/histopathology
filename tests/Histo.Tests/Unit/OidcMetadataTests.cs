using Histo.Web.Auth;
using Microsoft.Extensions.Configuration;

namespace Histo.Tests.Unit;

public class OidcMetadataTests
{
    private static IConfigurationSection Section(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build().GetSection("AzureAd");

    [Fact]
    public void AddressFor_ScopesTheDiscoveryDocumentToTheApp()
    {
        var section = Section(new()
        {
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd:TenantId"] = "tenant-1",
            ["AzureAd:ClientId"] = "client-1",
        });

        Assert.Equal(
            "https://login.microsoftonline.com/tenant-1/v2.0/.well-known/openid-configuration?appid=client-1",
            OidcMetadata.AddressFor(section));
    }

    [Fact]
    public void AddressFor_DefaultsTheInstanceWhenNotConfigured()
    {
        var section = Section(new() { ["AzureAd:TenantId"] = "t", ["AzureAd:ClientId"] = "c" });

        Assert.StartsWith("https://login.microsoftonline.com/t/v2.0/", OidcMetadata.AddressFor(section));
    }
}
