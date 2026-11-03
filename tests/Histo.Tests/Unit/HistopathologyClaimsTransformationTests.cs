using System.Reflection;
using System.Security.Claims;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Auth;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;

namespace Histo.Tests.Unit;

public class HistopathologyClaimsTransformationTests
{
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILogger<HistopathologyClaimsTransformation>> _logger = new();

    private HistopathologyClaimsTransformation CreateSut() => new(_users.Object, _logger.Object);

    private static ClaimsPrincipal Authenticated(params Claim[] claims) => new(new ClaimsIdentity(claims, "test"));

    private static User AppUser() => new()
    {
        UserID = 12, Name = "Ann User", GroupCode = 2, GroupName = "Histopathology User",
        AreaCode = 4, AreaName = "Weybridge", Email = "ann@x.test", Active = true,
    };

    [Fact]
    public async Task TransformAsync_Unauthenticated_ReturnsPrincipalUntouchedWithoutLookup()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await CreateSut().TransformAsync(principal);

        Assert.Same(principal, result);
        _users.Verify(u => u.ResolveUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransformAsync_NoEmailClaim_ReturnsWithoutAppClaims()
    {
        var principal = Authenticated(new Claim(ClaimTypes.NameIdentifier, "guid-1"));

        var result = await CreateSut().TransformAsync(principal);

        Assert.Null(result.FindFirst(AppClaimTypes.GroupName));
        _users.Verify(u => u.ResolveUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransformAsync_UnknownUser_ReturnsWithoutAppClaims()
    {
        _users.Setup(u => u.ResolveUserByEmailAsync("ann@x.test", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var principal = Authenticated(new Claim(ClaimTypes.Email, "ann@x.test"));

        var result = await CreateSut().TransformAsync(principal);

        Assert.Null(result.FindFirst(AppClaimTypes.GroupName));
    }

    [Theory]
    [InlineData(ClaimTypes.Email)]
    [InlineData("preferred_username")]
    public async Task TransformAsync_KnownUser_AddsAllAppClaimsFromAnyEmailClaimType(string emailClaimType)
    {
        _users.Setup(u => u.ResolveUserByEmailAsync("ann@x.test", It.IsAny<CancellationToken>())).ReturnsAsync(AppUser());
        var principal = Authenticated(new Claim(emailClaimType, "ann@x.test"));

        var result = await CreateSut().TransformAsync(principal);

        Assert.Equal("Histopathology User", result.FindFirst(AppClaimTypes.GroupName)?.Value);
        Assert.Equal("12", result.FindFirst(AppClaimTypes.UserDbId)?.Value);
        Assert.Equal("2", result.FindFirst(AppClaimTypes.GroupId)?.Value);
        Assert.Equal("Weybridge", result.FindFirst(AppClaimTypes.UserArea)?.Value);
        Assert.Equal("4", result.FindFirst(AppClaimTypes.UserAreaId)?.Value);
        Assert.Equal("Ann User", result.FindFirst(AppClaimTypes.UserName)?.Value);
    }

    [Fact]
    public async Task TransformAsync_StaleAppClaimsInCookie_AreReplacedNotDuplicated()
    {
        _users.Setup(u => u.ResolveUserByEmailAsync("ann@x.test", It.IsAny<CancellationToken>())).ReturnsAsync(AppUser());
        var principal = Authenticated(
            new Claim(ClaimTypes.Email, "ann@x.test"),
            new Claim(AppClaimTypes.GroupName, "Customer"),
            new Claim(AppClaimTypes.UserAreaId, "99"));

        var result = await CreateSut().TransformAsync(principal);

        Assert.Single(result.FindAll(AppClaimTypes.GroupName));
        Assert.Equal("Histopathology User", result.FindFirst(AppClaimTypes.GroupName)?.Value);
        Assert.Equal("4", Assert.Single(result.FindAll(AppClaimTypes.UserAreaId)).Value);
    }
}

public class GroupRestrictedPagesApplyTests
{
    [Fact]
    public void Apply_AddsGroupFiltersToEveryRestrictedPageOnly()
    {
        var options = new RazorPagesOptions();
        GroupRestrictedPages.Apply(options);

        PageApplicationModel Model(string page)
        {
            var descriptor = new PageActionDescriptor { ViewEnginePath = page, RelativePath = "/Pages" + page + ".cshtml", AreaName = null };
            var model = new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), []);
            foreach (var convention in options.Conventions.OfType<IPageApplicationModelConvention>())
                convention.Apply(model);
            return model;
        }

        foreach (var page in GroupRestrictedPages.HistoOrMaintenance)
            Assert.Contains(Model(page).Filters, f => f is HistoOrMaintenancePageFilter);
        foreach (var page in GroupRestrictedPages.MaintenanceOnly)
            Assert.Contains(Model(page).Filters, f => f is MaintenanceOnlyPageFilter);
        Assert.Empty(Model("/Index").Filters);
    }
}
