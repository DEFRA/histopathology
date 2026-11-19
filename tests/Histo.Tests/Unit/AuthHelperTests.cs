using Histo.Web.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Histo.Tests.Unit;

public class AuthHelperTests
{
    [Fact]
    public void StampClaim_CarriesTheStampUnderTheSessionStampClaimType()
    {
        var claim = SingleSessionPolicy.StampClaim("abc123");

        Assert.Equal(AppClaimTypes.SessionStamp, claim.Type);
        Assert.Equal("abc123", claim.Value);
    }

    [Fact]
    public async Task MaintenanceOnlyPageFilter_HandlerSelection_CompletesWithoutChangingTheResult()
    {
        var pageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor()));
        var context = new PageHandlerSelectedContext(pageContext, [], new object());

        await new MaintenanceOnlyPageFilter().OnPageHandlerSelectionAsync(context);

        Assert.Null(context.HandlerMethod);
    }
}
