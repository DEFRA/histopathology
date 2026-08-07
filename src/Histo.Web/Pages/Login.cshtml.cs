// BRIDGE (ADR-006): Temporary pre-Entra-ID manual login page.
// DECOMMISSION when: ITfoxtec SAML ACS endpoint is live and calls Session.PopulateFromUser().
// See: docs/EntraID-Implementation-plan.md — Phase B, Step 5.
// Files to delete at decommission: Login.cshtml, Login.cshtml.cs
// Files to update at decommission: HistoPageModel.cs — redirect /Login → SAML challenge

using Histo.Administration.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages;

/// <summary>
/// Manual NTLogin entry page — pre-Entra-ID authentication bridge (ADR-006).
///
/// Does NOT inherit HistoPageModel — intentionally outside the session gate
/// to prevent a redirect loop (HistoPageModel redirects here when GroupName
/// is empty; if this page also gated itself, the redirect would be infinite).
///
/// Flow:
///   GET  /Login — render the form (redirect to /Index if already signed in).
///   POST /Login — validate the entered NTLogin against the database.
///                 On success: populate session and redirect to /Index.
///                 On failure: redisplay the form with an error message.
///
/// Phase 2 (Entra ID): decommission this page — the SAML ACS endpoint will
/// call Session.PopulateFromUser() directly after validating the assertion.
/// </summary>
public class LoginModel : PageModel
{
    private readonly UserService _userService;
    private readonly ISessionService _session;

    public LoginModel(UserService userService, ISessionService session)
    {
        _userService = userService;
        _session = session;
    }

    [BindProperty]
    public string NtLogin { get; set; } = string.Empty;

    public string ErrorMessage { get; private set; } = string.Empty;

    public IActionResult OnGet()
    {
        // Already signed in — skip the login page.
        if (!string.IsNullOrEmpty(_session.GroupName))
            return RedirectToPage("/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var login = NtLogin?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(login))
        {
            ErrorMessage = "Enter your Windows username";
            return Page();
        }

        // Strip any domain prefix the user may have typed (e.g. "COGNIZANT\sd000106" → "sd000106").
        // The GetUserByNTLogin stored procedure stores bare usernames without the domain prefix.
        if (login.Contains('\\'))
            login = login[(login.LastIndexOf('\\') + 1)..];

        var user = await _userService.ResolveUserAsync(login);

        if (user is null)
        {
            ErrorMessage = $"'{NtLogin?.Trim()}' was not found in the Histopathology System, or the account is inactive. Contact your system administrator.";
            return Page();
        }

        _session.PopulateFromUser(user);
        return RedirectToPage("/Index");
    }
}
