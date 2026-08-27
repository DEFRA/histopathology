using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
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

    /// <summary>Batch ID from the URL (route/query). Falls back to <see cref="ISessionService.BatchID"/>.</summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>Submission ID carried through the form so POST never depends on session alone.</summary>
    [BindProperty(SupportsGet = true)] public int? BatchSubmissionId { get; set; }

    [BindProperty] public string SenderRef   { get; set; } = string.Empty;
    [BindProperty] public bool   IsNeuropath { get; set; }

    public string? ModelError { get; private set; }

    public async Task OnGetAsync(string? senderRef)
    {
        ViewData["Title"] = "Add Sample";
        ViewData["PageTitle"] = "Add Sample";
        BatchId ??= Session.BatchID;

        // Pre-resolve submission ID so the form POST never needs AddSubmissionAsync.
        BatchSubmissionId = Session.BatchSubmissionID;
        if ((BatchSubmissionId is null or <= 0) && BatchId is > 0)
        {
            var existing = await _submissions.GetSubmissionsByBatchAsync(BatchId.Value);
            if (existing.Count > 0) BatchSubmissionId = existing[0].ID;
        }

        if (!string.IsNullOrWhiteSpace(senderRef)) SenderRef = senderRef;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Add Sample";
        ViewData["PageTitle"] = "Add Sample";

        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage("/Index");

        var submissionId = BatchSubmissionId ?? Session.BatchSubmissionID;
        if (submissionId is null or <= 0)
        {
            // GET-time lookup found nothing — this is a brand-new batch with no submission
            // record yet, so create the default one now rather than failing.
            var existing = await _submissions.GetSubmissionsByBatchAsync(batchId.Value);
            submissionId = existing.Count > 0
                ? existing[0].ID
                : await _submissions.AddSubmissionAsync(
                    new BatchSubmission { BatchID = batchId.Value, SubmissionName = "Default", Order = 1 },
                    Session.UserID);
        }

        if (submissionId is null or <= 0)
        {
            ModelError = "Could not add the sample. Please try again.";
            return Page();
        }

        Session.BatchSubmissionID = submissionId;

        await _submissions.AddAnimalAsync(
            submissionId.Value, SenderRef, IsNeuropath, Session.UserID);

        return RedirectToPage("/Submissions/BatchBlockSummary", new { batchId });
    }
}
