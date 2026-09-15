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
/// GetAnimalsBySenderRef matches via <c>LIKE '%' + @SenderRef + '%'</c>, so an
/// empty filter returns every sender ref — used to list all of them on load.
/// </summary>
public class SearchSenderModel : GridPageModel
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

    /// <summary>Current page of <see cref="Results"/> for the GDS pagination component.</summary>
    public IReadOnlyList<SenderSearchResult> PagedResults =>
        Results.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    /// <summary>True once a search has run (always, after GET/POST) so the view knows
    /// to show the 'no results' message rather than leaving the page blank.</summary>
    public bool HasSearched { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"]     = IsPickerMode ? "Select sender ref" : "Search by Sender";
        ViewData["PageTitle"] = IsPickerMode ? "Select sender ref" : "Search by Sender";
        await RunSearchAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"]     = IsPickerMode ? "Select sender ref" : "Search by Sender";
        ViewData["PageTitle"] = IsPickerMode ? "Select sender ref" : "Search by Sender";
        await RunSearchAsync();
        return Page();
    }

    /// <summary>
    /// Runs the sender-ref search — blank <see cref="SenderRef"/> matches every row (see
    /// class summary), so this both populates the initial "browse all" view and re-runs
    /// after a filtered search or a page change.
    /// </summary>
    private async Task RunSearchAsync()
    {
        HasSearched = true;
        Results = await _submissions.GetAnimalsBySenderRefAsync(SenderRef?.Trim() ?? string.Empty);
        PopulateGridViewData(Results.Count);
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
