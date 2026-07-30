using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages;

/// <summary>
/// Base class for all Razor PageModels in Histo.Web.
///
/// Provides access to the typed session service and makes the current user
/// context available to every page without requiring individual injection.
///
/// Replaces the pattern in the legacy application where every ASPX page
/// called <c>VLAHeader1.getUserDetails()</c> in <c>Page_Load</c> and then
/// read from <c>Session(SessionVars.SV_*)</c> constants throughout the page.
/// </summary>
public abstract class HistoPageModel : PageModel
{
    public ISessionService Session { get; }

    protected HistoPageModel(ISessionService session)
    {
        Session = session;
    }
}
