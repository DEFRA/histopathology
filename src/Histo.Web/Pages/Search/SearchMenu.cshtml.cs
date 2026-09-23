using Histo.Web.Services;

namespace Histo.Web.Pages.Search;

public class SearchMenuModel : HistoPageModel
{
    public SearchMenuModel(ISessionService session) : base(session) { }
    public void OnGet() { }
}
