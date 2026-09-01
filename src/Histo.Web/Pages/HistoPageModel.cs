using Histo.Web.Auth;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
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
        // LOCAL-DEV-ONLY (uncommitted): skips Entra ID sign-in entirely.
        // Enabled only via appsettings.Development.json's "DevAuthBypass" flag (gitignored).
        // Fully hardcoded — no DB/IUserService lookup — so it never depends on GetUsers'
        // SP column shape or a matching row actually existing.
        if (context.HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("DevAuthBypass"))
        {
            // Unconditional — always re-applies the fixed principal/session on every request,
            // so a stale or partially-baked claim from an earlier real SAML attempt can never
            // cause this block to be skipped.
            {
                // Bakes the same claims AuthController's ACS handler would, so _Layout.cshtml's
                // User.Identity.IsAuthenticated checks (nav, user context) behave identically.
                // GroupName = "Histopathology User" grants area-unrestricted access (IsHistoUser)
                // and shows nearly all nav links — change to "Maintenance" for the admin-only pages.
                var identity = new System.Security.Claims.ClaimsIdentity("saml2");
                identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Silambarasan Duraiswamy"));
                identity.AddClaim(new System.Security.Claims.Claim(AppClaimTypes.GroupName, "Maintenance"));
                identity.AddClaim(new System.Security.Claims.Claim(AppClaimTypes.UserDbId, "243"));
                identity.AddClaim(new System.Security.Claims.Claim(AppClaimTypes.GroupId, "3"));
                identity.AddClaim(new System.Security.Claims.Claim(AppClaimTypes.UserArea, "Histopath"));
                identity.AddClaim(new System.Security.Claims.Claim(AppClaimTypes.UserAreaId, "5"));
                var principal = new System.Security.Claims.ClaimsPrincipal(identity);
                // No SignInAsync — the "saml2" scheme has no sign-in handler; setting HttpContext.User
                // directly is sufficient since this block re-runs fresh on every request anyway.
                context.HttpContext.User = principal;
                Session.PopulateFromClaims(principal);
            }
            await next();
            return;
        }

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

    /// <summary>
    /// Object-level access check for batch-scoped pages that accept a batch ID from the
    /// URL (route/query) rather than only from session state. Histo users see every area;
    /// other roles are restricted to their own <see cref="ISessionService.UserAreaID"/>.
    /// Returns <see langword="null"/> when access is allowed, or a Forbid result otherwise.
    /// </summary>
    protected async Task<IActionResult?> CheckBatchAccessAsync(IBatchService batches, int batchId)
    {
        var batch = await batches.GetByIdAsync(batchId);
        var allowed = BatchAccessDecision.IsAllowed(Session.IsHistoUser, batch?.UserAreaCode, Session.UserAreaID);
        return allowed ? null : Forbid();
    }
}

