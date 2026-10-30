using System.Security.Claims;
using Histo.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Histo.Tests.Unit;

public class RolePolicyTests
{
    private static ClaimsPrincipal User(string? group, bool authenticated = true)
    {
        var identity = authenticated ? new ClaimsIdentity("test") : new ClaimsIdentity();
        if (group is not null) identity.AddClaim(new Claim(AppClaimTypes.GroupName, group));
        return new ClaimsPrincipal(identity);
    }

    private static async Task<bool> AllowedAsync(string policy, ClaimsPrincipal user)
    {
        var provider = new ServiceCollection().AddLogging().AddAuthorizationBuilder().AddHistoPolicies().Services.BuildServiceProvider();
        return (await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(user, policy)).Succeeded;
    }

    [Theory]
    [InlineData("Maintenance", true)]
    [InlineData("Histopathology User", false)]
    [InlineData("Customer", false)]
    [InlineData(null, false)]
    public async Task MaintenanceOnlyPolicy_AllowsOnlyMaintenance(string? group, bool expected) =>
        Assert.Equal(expected, await AllowedAsync(AuthorizationPolicies.MaintenanceOnly, User(group)));

    [Theory]
    [InlineData("Maintenance", true)]
    [InlineData("Histopathology User", true)]
    [InlineData("Customer", false)]
    [InlineData(null, false)]
    public async Task HistoOrMaintenancePolicy_AllowsHistoAndMaintenance(string? group, bool expected) =>
        Assert.Equal(expected, await AllowedAsync(AuthorizationPolicies.HistoOrMaintenance, User(group)));

    [Theory]
    [InlineData(AuthorizationPolicies.MaintenanceOnly)]
    [InlineData(AuthorizationPolicies.HistoOrMaintenance)]
    public async Task Policies_RejectUnauthenticatedUserEvenWithGroupClaim(string policy) =>
        Assert.False(await AllowedAsync(policy, User("Maintenance", authenticated: false)));
}
