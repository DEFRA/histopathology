using Histo.Submissions.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>
/// Replaces <c>AddSample.aspx</c> — adds an animal/sample to the current batch,
/// reached from a Sender Ref search selection (<see cref="Search.SearchSampleModel"/>)
/// rather than typed in directly (that entry point is already covered by
/// <see cref="AddSubmissionModel"/>).
///
/// SIMPLIFIED: the legacy page's daybook (PG number) lookup, mouse-number range
/// bulk entry, Excel mouse-number upload, project-code override, and TB
/// Diagnostics validation-override features are not ported — this mirrors the
/// reduced scope already established by <see cref="AddSubmissionModel"/>, the
/// migrated equivalent of the legacy non-cassetted <c>AddSubmission.aspx</c>
/// (which duplicates the same core Sender Ref logic).
/// </summary>
public class AddSampleModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;

    public AddSampleModel(ISessionService session, ISubmissionService submissions)
        : base(session) => _submissions = submissions;

    [BindProperty] public string SenderRef   { get; set; } = string.Empty;
    [BindProperty] public bool   IsNeuropath { get; set; }

    public void OnGet(string? senderRef)
    {
        ViewData["Title"] = "Add Sample";
        ViewData["PageTitle"] = "Add Sample";
        if (!string.IsNullOrWhiteSpace(senderRef)) SenderRef = senderRef;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Add Sample";
        ViewData["PageTitle"] = "Add Sample";
        if (Session.BatchSubmissionID <= 0) return RedirectToPage("/Index");

        await _submissions.AddAnimalAsync(
            Session.BatchSubmissionID ?? 0, SenderRef, IsNeuropath, Session.UserID);

        return RedirectToPage("/Submissions/BatchBlockSummary");
    }
}
