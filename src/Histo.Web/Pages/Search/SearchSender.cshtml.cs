using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>
/// Replaces <c>SearchSender.aspx</c>.
///
/// SIMPLIFIED: the legacy page was a picker populated from a session-stored
/// temporary sender list built by the "lookup" button on AddSubmission.aspx,
/// with row selection redirecting back into AddSubmission.aspx. That workflow
/// does not yet exist in the migrated system, so this page is reimplemented as
/// a standalone read-only sender-ref search using the same repository method
/// that already backs SearchSample.aspx (<c>GetAnimalsBySenderRefAsync</c>).
/// </summary>
public class SearchSenderModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;

    public SearchSenderModel(ISessionService session, ISubmissionService submissions)
        : base(session) => _submissions = submissions;

    [BindProperty(SupportsGet = true)] public string? SenderRef { get; set; }

    // ── Picker mode: set by the calling page (e.g. CopyBatch) via query params. ──
    [BindProperty(SupportsGet = true)] public string? ReturnPage { get; set; }
    [BindProperty(SupportsGet = true)] public int?    ReturnId   { get; set; }
    [BindProperty(SupportsGet = true)] public int     RowIndex   { get; set; } = -1;

    /// <summary>True when the page is launched as a picker from another page.</summary>
    public bool IsPickerMode => !string.IsNullOrEmpty(ReturnPage);

    public IReadOnlyList<SenderSearchResult> Results { get; private set; } = [];

    /// <summary>True once the user has submitted a non-empty search so the view knows
    /// to show the 'no results' message rather than leaving the page blank.</summary>
    public bool HasSearched { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"]     = IsPickerMode ? "Select sender ref" : "Search by Sender";
        ViewData["PageTitle"] = IsPickerMode ? "Select sender ref" : "Search by Sender";

        // Mirrors legacy lbLookup_Click on AddSubmission.aspx: the sender ref already typed on
        // the calling page is carried over and the search runs immediately, landing the user on
        // a populated results grid rather than a second blank search box.
        if (!string.IsNullOrWhiteSpace(SenderRef))
        {
            HasSearched = true;
            Results = await _submissions.GetAnimalsBySenderRefAsync(SenderRef.Trim());
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"]     = IsPickerMode ? "Select sender ref" : "Search by Sender";
        ViewData["PageTitle"] = IsPickerMode ? "Select sender ref" : "Search by Sender";

        if (!string.IsNullOrWhiteSpace(SenderRef))
        {
            HasSearched = true;
            Results = await _submissions.GetAnimalsBySenderRefAsync(SenderRef.Trim());
        }

        return Page();
    }

    /// <summary>
    /// Picker mode: stores the chosen sender ref in TempData and redirects back to the origin page.
    /// Reusable — any page can act as a caller by passing returnPage/returnId/rowIndex.
    /// </summary>
    public IActionResult OnPostSelect(string selectedSenderRef, string? returnPage, int? returnId)
    {
        TempData["SenderRefPicker_Selected"] = selectedSenderRef;
        if (!string.IsNullOrEmpty(returnPage))
        {
            return returnId.HasValue
                ? RedirectToPage(returnPage, new { sourceBatchId = returnId.Value })
                : RedirectToPage(returnPage);
        }
        return RedirectToPage();
    }
}
