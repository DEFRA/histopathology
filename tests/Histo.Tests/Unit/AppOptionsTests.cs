using Histo.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Histo.Tests.Unit;

/// <summary>
/// Baseline unit tests for <see cref="AppOptions"/> configuration binding.
///
/// Verifies that the options class binds correctly from the <c>AppSettings</c>
/// configuration section, and that default values are safe empty strings.
///
/// Legacy equivalent: <c>ConfigurationSettings.AppSettings("Exports")</c> and
/// other scattered <c>Web.config</c> appSettings reads.
/// </summary>
public class AppOptionsTests
{
    [Fact]
    public void AppOptions_BindsExportPathFromConfiguration()
    {
        var options = BuildOptions(new Dictionary<string, string?>
        {
            [$"{AppOptions.SectionName}:ExportPath"] = @"C:\exports",
        });

        Assert.Equal(@"C:\exports", options.ExportPath);
    }

    [Fact]
    public void AppOptions_BindsApplicationInsightsConnectionStringFromConfiguration()
    {
        var options = BuildOptions(new Dictionary<string, string?>
        {
            [$"{AppOptions.SectionName}:ApplicationInsightsConnectionString"] = "InstrumentationKey=test-key",
        });

        Assert.Equal("InstrumentationKey=test-key", options.ApplicationInsightsConnectionString);
    }

    [Fact]
    public void AppOptions_MissingKeys_DefaultToEmptyString()
    {
        var options = BuildOptions(new Dictionary<string, string?>());

        Assert.Equal(string.Empty, options.ExportPath);
        Assert.Equal(string.Empty, options.ApplicationInsightsConnectionString);
    }

    [Fact]
    public void AppOptions_SectionName_IsAppSettings()
    {
        // Guard: the section name constant must match what appsettings.json uses.
        // Changing this constant is a breaking change — this test acts as a trip-wire.
        Assert.Equal("AppSettings", AppOptions.SectionName);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static AppOptions BuildOptions(Dictionary<string, string?> values)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.Configure<AppOptions>(config.GetSection(AppOptions.SectionName));
        var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<AppOptions>>().Value;
    }
}
