using System.Security.Claims;
using Histo.Web.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Histo.Tests.Unit;

/// <summary>CDE-06: /Admin pages must be reachable by the Maintenance group only.</summary>
public class MaintenanceOnlyPageFilterTests
{
    private static async Task<(PageHandlerExecutingContext Context, bool NextCalled)> RunAsync(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var pageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()));
        var context = new PageHandlerExecutingContext(
            pageContext, [], new HandlerMethodDescriptor(), new Dictionary<string, object?>(), handlerInstance: new object());
        var nextCalled = false;

        await new MaintenanceOnlyPageFilter().OnPageHandlerExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new PageHandlerExecutedContext(pageContext, [], new HandlerMethodDescriptor(), new object()));
        });

        return (context, nextCalled);
    }

    private static ClaimsPrincipal SignedIn(string? group)
    {
        var identity = new ClaimsIdentity("test");
        if (group is not null) identity.AddClaim(new Claim(AppClaimTypes.GroupName, group));
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task MaintenanceUser_IsAllowedThrough()
    {
        var (context, nextCalled) = await RunAsync(SignedIn("Maintenance"));

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("Histopathology User")]
    [InlineData("Customer")]
    [InlineData(null)]
    public async Task NonMaintenanceUser_IsForbiddenAndTheHandlerDoesNotRun(string? group)
    {
        var (context, nextCalled) = await RunAsync(SignedIn(group));

        Assert.False(nextCalled);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task AnonymousUser_IsChallenged()
    {
        var (context, nextCalled) = await RunAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(nextCalled);
        Assert.IsType<ChallengeResult>(context.Result);
    }
}
