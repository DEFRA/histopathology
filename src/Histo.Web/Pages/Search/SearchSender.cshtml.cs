using Histo.Submissions.Models;
using Histo.Submissions.Services;
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
    private readonly SubmissionService _submissions;

    public SearchSenderModel(ISessionService session, SubmissionService submissions)
        : base(session) => _submissions = submissions;

    [BindProperty] public string? SenderRef { get; set; }

    public IReadOnlyList<SenderSearchResult> Results { get; private set; } = [];

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
            Results = await _submissions.GetAnimalsBySenderRefAsync(SenderRef);

        return Page();
    }
}
