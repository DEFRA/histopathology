using Histo.Web.Services;

namespace Histo.Web.Pages.AuditLog;

public class AuditLogMenuModel : HistoPageModel
{
    public AuditLogMenuModel(ISessionService session) : base(session) { }
    public void OnGet()
    {
        // Static page: the view needs no model data.
    }
}
