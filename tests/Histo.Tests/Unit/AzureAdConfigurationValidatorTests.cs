using Histo.Web.Auth;
using Microsoft.Extensions.Configuration;

namespace Histo.Tests.Unit;

public class AzureAdConfigurationValidatorTests
{
    [Fact]
    public void RejectsPlaceholderValuesInAzureAdSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:TenantId"] = "__TENANT_ID__",
                ["AzureAd:ClientId"] = "__CLIENT_ID__",
                ["AzureAd:ClientSecret"] = "__CLIENT_SECRET__",
                ["AzureAd:PublicOrigin"] = "__PUBLIC_ORIGIN__"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => AzureAdConfigurationValidator.Validate(configuration));
        Assert.Contains("AzureAd:TenantId", ex.Message);
    }

    [Fact]
    public void AcceptsRealAzureAdSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:TenantId"] = "11111111-1111-1111-1111-111111111111",
                ["AzureAd:ClientId"] = "22222222-2222-2222-2222-222222222222",
                ["AzureAd:ClientSecret"] = "a-real-secret-value",
                ["AzureAd:PublicOrigin"] = "https://example.defra.gov.uk"
            })
            .Build();

        var ex = Record.Exception(() => AzureAdConfigurationValidator.Validate(configuration));

        Assert.Null(ex);
    }
}
