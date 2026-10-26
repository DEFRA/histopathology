using System.Text.RegularExpressions;

namespace Histo.Tests.Unit;

/// <summary>Audit logs are Maintenance-only, as in legacy <c>Home.aspx.vb</c>. Allow/deny is covered by <see cref="MaintenanceOnlyPageFilterTests"/>.</summary>
public class AuditLogAccessTests
{
    [Fact]
    public void AuditLogFolder_IsRestrictedToMaintenanceGroup()
    {
        var program = ReadSource("src", "Histo.Web", "Program.cs");

        Assert.Matches(
            new Regex(@"AddFolderApplicationModelConvention\(\s*""/AuditLog""\s*,\s*model\s*=>\s*model\.Filters\.Add\(new\s+Histo\.Web\.Auth\.MaintenanceOnlyPageFilter\(\)\)\)"),
            program);
    }

    [Fact]
    public void HomePage_ShowsAuditLogsLinkOnlyInTheMaintenancePanel()
    {
        var markup = ReadSource("src", "Histo.Web", "Pages", "Index.cshtml");

        var link = markup.IndexOf("asp-page=\"/AuditLog/AuditLogMenu\"", StringComparison.Ordinal);
        var maintenancePanel = markup.IndexOf("@if (Model.Session.IsMaintenance)", StringComparison.Ordinal);

        Assert.True(link >= 0, "Audit logs link not found on the home page.");
        Assert.True(maintenancePanel >= 0, "Maintenance-only panel not found on the home page.");
        Assert.True(link > maintenancePanel, "Audit logs link must sit inside the Maintenance-only panel.");
        Assert.Equal(link, markup.LastIndexOf("asp-page=\"/AuditLog/AuditLogMenu\"", StringComparison.Ordinal));
    }

    [Fact]
    public void HelpPage_ListsAuditLogsUnderMaintenanceOnlyLinks()
    {
        var markup = ReadSource("src", "Histo.Web", "Pages", "Help", "Index.cshtml");

        var maintenanceHeading = markup.IndexOf("Available links (Maintenance only)", StringComparison.Ordinal);
        var auditItem = markup.IndexOf("<strong>Audit logs</strong>", StringComparison.Ordinal);

        Assert.True(maintenanceHeading >= 0, "Maintenance-only links heading not found on the Help page.");
        Assert.True(auditItem > maintenanceHeading, "Audit logs must be listed under the Maintenance-only links.");
    }

    private static string ReadSource(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine([dir.FullName, .. parts])))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine([dir!.FullName, .. parts]));
    }
}
