using Histo.Submissions.Interfaces;
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

    [BindProperty] public string SenderRef   { get; set; } = string.Empty;
    [BindProperty] public bool   IsNeuropath { get; set; }

    public string? ModelError { get; private set; }

    public Task OnGetAsync()
    {
        ViewData["Title"] = "Add Submission";
        BatchId ??= Session.BatchID;

        // Restore the sender ref chosen via the Search Sender picker (SearchSender.cshtml).
        if (TempData.TryGetValue("SenderRefPicker_Selected", out var chosen) && chosen is string chosenRef)
            SenderRef = chosenRef;

        return Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Add Submission";

        var batchIdForSubmission = BatchId ?? Session.BatchID;
        if (batchIdForSubmission is null or <= 0) return RedirectToPage("/Index");

        // BatchSubmissionID is normally populated by BatchBlockSummary's OnGetAsync, but relying
        // on that alone left this page unable to recover (silently redirecting to Home) if the
        // value was ever missing — resolve/create it here instead of hard-failing.
        var submissionId = Session.BatchSubmissionID;
        if (submissionId is null or <= 0)
        {
            var existing = await _submissions.GetSubmissionsByBatchAsync(batchIdForSubmission.Value);
            submissionId = existing.Count > 0
                ? existing[0].ID
                : await _submissions.AddSubmissionAsync(
                    new Histo.Submissions.Models.BatchSubmission { BatchID = batchIdForSubmission.Value, SubmissionName = "Default", Order = 1 },
                    Session.UserID);

            if (submissionId is null or <= 0)
            {
                ModelError = "Could not add the sample. Please try again.";
                return Page();
            }

            Session.BatchSubmissionID = submissionId;
        }

        var newAnimalId = await _submissions.AddAnimalAsync(
            submissionId.Value, SenderRef, IsNeuropath, Session.UserID);

        if (newAnimalId <= 0) return RedirectToPage("/Submissions/BatchBlockSummary", new { batchId = BatchId ?? Session.BatchID });

        // Legacy AddSubmission.aspx navigates straight into the new sample's block/tissue detail
        // screen after adding it, rather than back to the sample list. Cassetted vs wet-tissue is
        // determined the same way BatchBlockSummary does: whether the block-animal view has rows.
        Session.AnimalID = newAnimalId; // SubmissionDetails (wet-tissue) only reads AnimalID from session
        var batchId = Session.BatchID ?? 0;
        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(batchId);
        return blockAnimals.Any(a => a.ID == newAnimalId)
            ? RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId = BatchId ?? Session.BatchID, animalId = newAnimalId })
            : RedirectToPage("/Submissions/SubmissionDetails"); // wet-tissue page reads Session.AnimalID only
    }
}
