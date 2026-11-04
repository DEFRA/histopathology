using System.Security.Claims;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Histo.Tests.Unit;

public class OidcSignInHandlerTests
{
    private const string ObjectIdClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILogger<OidcSignInHandler>> _logger = new();
    private readonly IDistributedCache _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static readonly User ActiveUser = new()
    {
        UserID = 42, GroupName = "Histopathology User", GroupCode = 2, AreaName = "Histopath", AreaCode = 7, Active = true,
    };

    private OidcSignInHandler CreateSut() => new(_users.Object, _cache, _logger.Object);

    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "OpenIdConnect"));

    private void AssertWarningLogged() =>
        _logger.Verify(l => l.Log(
            LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

    [Fact]
    public async Task KnownUser_GetsAppClaimsAndACurrentSessionStamp()
    {
        _users.Setup(u => u.ResolveUserByEmailAsync("a.user@example.gov.uk", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveUser);
        var principal = Principal((ClaimTypes.Email, "a.user@example.gov.uk"), (ObjectIdClaim, "oid-1"));

        await CreateSut().OnTokenValidatedAsync(principal);

        Assert.Equal("Histopathology User", principal.FindFirstValue(AppClaimTypes.GroupName));
        Assert.Equal("42", principal.FindFirstValue(AppClaimTypes.UserDbId));
        Assert.Equal("2", principal.FindFirstValue(AppClaimTypes.GroupId));
        Assert.Equal("Histopath", principal.FindFirstValue(AppClaimTypes.UserArea));
        Assert.Equal("7", principal.FindFirstValue(AppClaimTypes.UserAreaId));
        Assert.Equal(await _cache.GetStringAsync("session-stamp:42"), principal.FindFirstValue(AppClaimTypes.SessionStamp));
    }

    [Theory]
    [InlineData("email")]
    [InlineData("preferred_username")]
    public async Task UnmappedEmailOrUpnClaim_IsUsedForTheLookup(string claimType)
    {
        _users.Setup(u => u.ResolveUserByEmailAsync("a.user@example.gov.uk", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveUser);
        var principal = Principal((claimType, "a.user@example.gov.uk"));

        await CreateSut().OnTokenValidatedAsync(principal);

        Assert.Equal("Histopathology User", principal.FindFirstValue(AppClaimTypes.GroupName));
    }

    [Fact]
    public async Task UnknownOrInactiveUser_GetsNoAppClaims()
    {
        _users.Setup(u => u.ResolveUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var principal = Principal((ClaimTypes.Email, "stranger@example.com"), ("oid", "oid-2"));

        await CreateSut().OnTokenValidatedAsync(principal);

        Assert.Null(principal.FindFirst(AppClaimTypes.GroupName));
        Assert.Null(principal.FindFirst(AppClaimTypes.SessionStamp));
        AssertWarningLogged();
    }

    [Fact]
    public async Task NoEmailOrUpn_SkipsTheLookup()
    {
        var principal = Principal((ObjectIdClaim, "oid-3"));

        await CreateSut().OnTokenValidatedAsync(principal);

        Assert.Null(principal.FindFirst(AppClaimTypes.GroupName));
        _users.Verify(u => u.ResolveUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertWarningLogged();
    }

    [Fact]
    public async Task NullPrincipal_IsIgnored()
    {
        await CreateSut().OnTokenValidatedAsync(null);

        _users.VerifyNoOtherCalls();
    }
}
