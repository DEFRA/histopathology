using Histo.Web.Services;

namespace Histo.Web.Pages.Bookings;

public class BookingMenuModel : HistoPageModel
{
    public BookingMenuModel(ISessionService session) : base(session) { }
    public void OnGet()
    {
        // Static page: the view needs no model data.
    }
}
