using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Histo.Web.Auth;

/// <summary>
/// Restricts a page to the Histopathology User and Maintenance groups, enforced before any handler (GET or POST) runs.
/// Mirrors legacy <c>CheckPermissions()</c>, which sent every other group (Customer) to <c>Home.aspx</c>.
/// </summary>
public sealed class HistoOrMaintenancePageFilter : IAsyncPageFilter
{
    public const string HistoGroup = "Histopathology User";
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

        var group = user.FindFirst(AppClaimTypes.GroupName)?.Value;
        if (group is not (HistoGroup or MaintenanceGroup))
        {
            context.Result = new RedirectToPageResult("/Index");
            return;
        }

        await next();
    }
}
