using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>Replaces <c>AddSubmission.aspx</c>.</summary>
public class AddSubmissionModel : HistoPageModel
{
    private readonly SubmissionService _submissions;

    public AddSubmissionModel(ISessionService session, SubmissionService submissions)
        : base(session) => _submissions = submissions;

    [BindProperty] public string SenderRef   { get; set; } = string.Empty;
    [BindProperty] public bool   IsNeuropath { get; set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Add Submission";
        await Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Add Submission";
        if (Session.BatchSubmissionID <= 0) return RedirectToPage("/Index");

        await _submissions.AddAnimalAsync(
            Session.BatchSubmissionID ?? 0, SenderRef, IsNeuropath, Session.UserID);

        return RedirectToPage("/Submissions/ViewSamples");
    }
}
