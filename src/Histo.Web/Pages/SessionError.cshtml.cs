using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages;

/// <summary>
/// Shown after an idle timeout. Does not inherit HistoPageModel — it must stay anonymous so it
/// cannot challenge back into the sign-in flow it is explaining.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class SessionErrorModel : PageModel
{
    public void OnGet()
    {
    }
}
