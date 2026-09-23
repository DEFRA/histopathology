using Histo.Core.Domain;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="HelpSectionMap.Resolve"/> — context-sensitive Help navigation.
/// </summary>
public class HelpSectionMapTests
{
    [Theory]
    [InlineData("/Admin/UserMaintenance", "user-maintenance")]
    [InlineData("/admin/usermaintenance", "user-maintenance")] // case-insensitive
    [InlineData("/Batches/Cassetted", "submission-type")]
    [InlineData("/Submissions/ViewSubmissions", "view-submissions")]
    [InlineData("/AuditLog/AuditLogByDate", "audit-logs")]
    public void Resolve_KnownPage_ReturnsExpectedAnchor(string pagePath, string expectedAnchor)
        => Assert.Equal(expectedAnchor, HelpSectionMap.Resolve(pagePath));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/Some/UnmappedPage")]
    public void Resolve_UnknownOrMissingPage_ReturnsNull(string? pagePath)
        => Assert.Null(HelpSectionMap.Resolve(pagePath));
}
