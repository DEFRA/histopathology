using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>
/// Replaces <c>SearchSample.aspx</c>.
///
/// SIMPLIFIED: the legacy page was a picker populated from a session-stored
/// temporary sender list built by the "lookup" button on AddSubmission.aspx,
/// with row selection redirecting to AddSample.aspx. That AddSample flow does
/// not yet exist in the migrated system, so this page is reimplemented as a
/// standalone read-only sender-ref search using the same repository method
/// that already backs SearchSender.aspx (<c>GetAnimalsBySenderRefAsync</c>).
/// </summary>
public class SearchSampleModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;

    public SearchSampleModel(ISessionService session, ISubmissionService submissions)
        : base(session) => _submissions = submissions;

    [BindProperty] public string? SenderRef { get; set; }

    public IReadOnlyList<SenderSearchResult> Results { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Search Samples";
        ViewData["PageTitle"] = "Search Samples";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Search Samples";
        ViewData["PageTitle"] = "Search Samples";

        if (!string.IsNullOrWhiteSpace(SenderRef))
            Results = await _submissions.GetAnimalsBySenderRefAsync(SenderRef);

        return Page();
    }
}
