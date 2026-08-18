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

    [BindProperty] public string? SenderRef { get; set; }

    public IReadOnlyList<SenderSearchResult> Results { get; private set; } = [];

    /// <summary>True once the user has submitted a non-empty search so the view knows
    /// to show the 'no results' message rather than leaving the page blank.</summary>
    public bool HasSearched { get; private set; }

    public void OnGet()
    {
        ViewData["Title"] = "Search by Sender";
        ViewData["PageTitle"] = "Search by Sender";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Search by Sender";
        ViewData["PageTitle"] = "Search by Sender";

        if (!string.IsNullOrWhiteSpace(SenderRef))
        {
            HasSearched = true;
            Results = await _submissions.GetAnimalsBySenderRefAsync(SenderRef.Trim());
        }

        return Page();
    }
}
