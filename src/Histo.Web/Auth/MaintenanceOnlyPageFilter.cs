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
            context.Result = new ChallengeResult("saml2");
            return;
        }

        if (user.FindFirst(AppClaimTypes.GroupName)?.Value != MaintenanceGroup)
        {
            AuthorizationAudit.LogDenied(context.HttpContext, "group not permitted (Maintenance required)");
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
