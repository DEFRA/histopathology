using Histo.Web.Services;

namespace Histo.Web.Pages.Help;

/// <summary>
/// Role-sensitive help page — replaces the two legacy static HTML files
/// (HistoHelp_CustomerGroup.htm and HistoHelp_HistoGroup.htm) that were
/// linked from VLAHeader.ascx via lnkHelp.
///
/// Customer group users see the Customer section set only.
/// Histopathology User and Maintenance groups see all sections.
///
/// Why these pages were absent from the initial migration:
/// The originals were .htm static files, not .aspx pages, so they fell
/// outside the page-by-page migration scope. The Help link from VLAHeader.ascx
/// was not carried forward to the new _Layout.cshtml / _NavPartial.cshtml.
/// </summary>
public class HelpModel : HistoPageModel
{
    public HelpModel(ISessionService session) : base(session) { }

    public void OnGet() { }
}
