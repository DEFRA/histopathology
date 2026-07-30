using Histo.Administration.Models;
using Histo.Administration.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;

namespace Histo.Web.Pages;

/// <summary>
/// Base class for all Razor PageModels in Histo.Web.
///
/// On every request, ensures the typed session is hydrated with the current
/// user's identity from the database — mirroring the legacy
/// <c>VLAHeader.ascx::getUserDetails()</c> call in every ASPX Page_Load.
///
/// Flow:
///   1. If GroupName is already in session (subsequent requests), skip DB lookup.
///   2. Otherwise call <c>UserService.ResolveUserAsync</c> with the NT login
///      derived from <c>HttpContext.User.Identity.Name</c> (Phase 1)
///      or Entra ID UPN (Phase 2 — ISS-009).
///   3. In Development with no authenticated user, a stub identity is used so
///      all panels render correctly during local testing.
/// </summary>
public abstract class HistoPageModel : PageModel
{
    public ISessionService Session { get; }

    protected HistoPageModel(ISessionService session)
    {
        Session = session;
    }

    public override async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        // Only hydrate when GroupName is not already in session — avoids a DB
        // round-trip on every postback (mirrors the legacy early-exit in getUserDetails).
        if (string.IsNullOrEmpty(Session.GroupName))
        {
            var userService = context.HttpContext.RequestServices
                .GetService(typeof(UserService)) as UserService;

            var env = context.HttpContext.RequestServices
                .GetService(typeof(IHostEnvironment)) as IHostEnvironment;

            if (userService is not null)
            {
                // Phase 2: User.Identity.Name will be the Entra ID UPN.
                // Until then it is the Windows NT login from IIS (or empty in dev).
                var ntLogin = context.HttpContext.User.Identity?.Name ?? string.Empty;

                User? user = null;

                if (!string.IsNullOrWhiteSpace(ntLogin))
                    user = await userService.ResolveUserAsync(ntLogin);

                // Dev/test stub — when no authenticated user is present and the app
                // is running in Development, inject a Maintenance stub so all panels
                // are visible. Remove or restrict this before deploying to any
                // non-development environment.
                if (user is null && (env?.IsDevelopment() ?? false))
                {
                    user = new User
                    {
                        UserID    = 0,
                        Name      = "Dev User (stub)",
                        GroupCode = 3,
                        GroupName = "Maintenance",
                        Email     = "dev@local",
                        AreaCode  = 1,
                        AreaName  = "Development",
                        Active    = true,
                        NtLogin   = "DEV\\stub"
                    };
                }

                if (user is not null)
                    Session.PopulateFromUser(user);
            }
        }

        await next();
    }
}
