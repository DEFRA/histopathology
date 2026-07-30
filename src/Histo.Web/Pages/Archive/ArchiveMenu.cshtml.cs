using Histo.Web.Services;

namespace Histo.Web.Pages.Archive;

public class ArchiveMenuModel : HistoPageModel
{
    public ArchiveMenuModel(ISessionService session) : base(session) { }
    public void OnGet() { }
}
