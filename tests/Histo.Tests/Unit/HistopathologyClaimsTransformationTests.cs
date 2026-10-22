using System.Security.Claims;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Histo.Tests.Unit;

public class HistopathologyClaimsTransformationTests
{
    private readonly Mock<IUserService> _users = new();

    private HistopathologyClaimsTransformation CreateSut() =>
        new(_users.Object, NullLogger<HistopathologyClaimsTransformation>.Instance);

    [Fact]
    public async Task GroupChangeSinceSignIn_ReplacesTheStaleClaim()
    {
        _users.Setup(u => u.ResolveUserByEmailAsync("a.user@example.gov.uk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { UserID = 1, GroupName = "Maintenance", AreaName = "Histopath" });
        var identity = new ClaimsIdentity(
            [new Claim("preferred_username", "a.user@example.gov.uk"), new Claim(AppClaimTypes.GroupName, "Customer")], "Cookies");

        var result = await CreateSut().TransformAsync(new ClaimsPrincipal(identity));

        Assert.Equal(["Maintenance"], result.FindAll(AppClaimTypes.GroupName).Select(c => c.Value));
    }

    [Fact]
    public async Task UserNoLongerActive_KeepsThePrincipalWithoutAddingClaims()
    {
        _users.Setup(u => u.ResolveUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, "x@example.com")], "Cookies"));

        var result = await CreateSut().TransformAsync(principal);

        Assert.Null(result.FindFirst(AppClaimTypes.UserDbId));
    }

    [Fact]
    public async Task UnauthenticatedPrincipal_IsReturnedUntouched()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await CreateSut().TransformAsync(principal);

        Assert.Same(principal, result);
        _users.VerifyNoOtherCalls();
    }
}
