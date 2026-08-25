using Histo.Web.Auth;
using Histo.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages;

/// <summary>
/// Base class for all Razor PageModels in Histo.Web.
///
/// Two-gate access model:
///   Gate 1 — Authentication: checks <see cref="HttpContext.User.Identity.IsAuthenticated"/>.
///             If not authenticated, issues a ChallengeResult which redirects the browser to
///             "/Saml2/login" (AuthController) → Entra ID → ACS → back to the requested page.
///   Gate 2 — Authorisation: checks that the Entra ID user has an active row in tblUser
///             (confirmed by the presence of <see cref="AppClaimTypes.GroupName"/> claim).
///             If absent, redirects to AccessDenied.cshtml.
///
/// On first authenticated request, user context (GroupName, UserID, etc.) is populated into
/// the HTTP session from claims baked into the auth cookie at ACS time, so that the rest of
/// the application can continue reading from <see cref="ISessionService"/> without change.
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
        // Gate 1 — Authentication: redirect to Entra ID via SAML if not signed in.
        if (User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult("saml2");
            return;
        }

        // Gate 2 — Authorisation: Entra ID user must have an active row in tblUser.
        var groupName = User.FindFirst(AppClaimTypes.GroupName)?.Value;
        if (string.IsNullOrEmpty(groupName))
        {
            context.Result = new RedirectToPageResult("/AccessDenied");
            return;
        }

        // Populate session from claims on first request after sign-in (session is empty
        // immediately after SAML ACS redirect until the next request populates it).
        if (string.IsNullOrEmpty(Session.GroupName))
            Session.PopulateFromClaims(User);

        await next();
    }
}
