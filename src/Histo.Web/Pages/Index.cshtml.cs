using Histo.Web.Services;

namespace Histo.Web.Pages;

public class IndexModel : HistoPageModel
{
    public IndexModel(ISessionService session) : base(session) { }

    public void OnGet()
    {
        // Clear workflow state on returning to home — mirrors Session.Clear() in legacy Home.aspx.
        Session.BatchID = null;
        Session.BatchSubmissionID = null;
        Session.AnimalID = null;
        Session.BlockID = null;
    }
}

