using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>Replaces <c>AddSubmission.aspx</c>.</summary>
public class AddSubmissionModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;

    public AddSubmissionModel(ISessionService session, ISubmissionService submissions)
        : base(session) => _submissions = submissions;

    /// <summary>Batch ID from the URL (route/query). Falls back to <see cref="ISessionService.BatchID"/>.</summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>Submission ID carried through the form so POST never depends on session alone.</summary>
    [BindProperty(SupportsGet = true)] public int? BatchSubmissionId { get; set; }

    [BindProperty] public string SenderRef   { get; set; } = string.Empty;
    [BindProperty] public bool   IsNeuropath { get; set; }

    public string? ModelError { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Add sample";
        BatchId ??= Session.BatchID;

        // Pre-resolve submission ID so the form POST never needs AddSubmissionAsync.
        BatchSubmissionId ??= Session.BatchSubmissionID;
        if ((BatchSubmissionId is null or <= 0) && BatchId is > 0)
        {
            var existing = await _submissions.GetSubmissionsByBatchAsync(BatchId.Value);
            if (existing.Count > 0) BatchSubmissionId = existing[0].ID;
        }

        // Restore the sender ref chosen via the Search Sender picker (SearchSender.cshtml).
        if (TempData.TryGetValue("SenderRefPicker_Selected", out var chosen) && chosen is string chosenRef)
            SenderRef = chosenRef;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Add sample";

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

        await _submissions.AddAnimalAsync(submissionId.Value, SenderRef, IsNeuropath, Session.UserID);

        return RedirectToPage("/Submissions/BatchBlockSummary", new { batchId });
    }
}
