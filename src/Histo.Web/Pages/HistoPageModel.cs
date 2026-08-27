using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages;

/// <summary>
/// Base class for all Razor PageModels in Histo.Web.
///
/// Acts as a session gate: if the user has not signed in via /Login, every
/// protected page redirects there. Once Login.cshtml.cs calls
/// Session.PopulateFromUser(), GroupName is set and all subsequent requests
/// pass through without a DB round-trip.
///
/// BRIDGE (ADR-006): This is a temporary pre-Entra-ID session gate.
/// Phase 2 (Entra ID): replace the /Login redirect with a SAML authentication
/// challenge once ITfoxtec.Identity.Saml2.MvcCore is wired.
/// See: docs/EntraID-Implementation-plan.md Phase B, Step 5.
/// See: docs/ADR/ADR-006-manual-login-page-bridge.md
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
        // BRIDGE (ADR-006): Redirect to manual login page if no session exists.
        // Replace "/Login" with a SAML challenge redirect when Entra ID is live.
        if (string.IsNullOrEmpty(Session.GroupName))
        {
            context.Result = new RedirectToPageResult("/Login");
            return;
        }

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

