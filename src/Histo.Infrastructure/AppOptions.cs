namespace Histo.Infrastructure;

/// <summary>
/// Strongly-typed application settings bound from the <c>AppSettings</c>
/// section of <c>appsettings.json</c>.
///
/// Replaces scattered <c>ConfigurationSettings.AppSettings("key")</c> calls
/// and the <c>Web.config</c> <c>&lt;appSettings&gt;</c> section.
///
/// Sensitive values (connection strings, Key Vault URIs) are NOT stored here —
/// they live in the <c>ConnectionStrings</c> section or are injected via
/// Key Vault references at the App Service configuration layer.
/// </summary>
public sealed class AppOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "AppSettings";

    /// <summary>
    /// Local path for Excel export files.
    /// Replaces <c>AppSettings("Exports")</c> (legacy value: <c>C:\export\</c>).
    /// On Azure App Service, this should point to an Azure Files mount path.
    /// </summary>
    public string ExportPath { get; init; } = string.Empty;

    /// <summary>
    /// Application Insights connection string.
    /// In production this is a Key Vault reference injected by App Service.
    /// </summary>
    public string ApplicationInsightsConnectionString { get; init; } = string.Empty;
}
