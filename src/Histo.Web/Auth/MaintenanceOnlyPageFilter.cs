using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Histo.Web.Auth;

/// <summary>Restricts a page folder to the Maintenance group, enforced before any handler (GET or POST) runs.</summary>
public sealed class MaintenanceOnlyPageFilter : IAsyncPageFilter
{
    public const string MaintenanceGroup = "Maintenance";

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        if (user.FindFirst(AppClaimTypes.GroupName)?.Value != MaintenanceGroup)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
