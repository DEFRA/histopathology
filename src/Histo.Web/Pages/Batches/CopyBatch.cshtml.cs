using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces <c>CopyBatch.aspx</c> / <c>CopyBatchBlocks.aspx</c> — duplicates an
/// existing submission (batch header, batch submissions, animals and tissues)
/// as the starting point for a new submission.
///
/// Legacy source: entry point was <c>ViewSubmissions.aspx</c> (select a row,
/// click Copy). The migrated entry point is <see cref="BatchDetailsModel"/>,
/// which already has the source batch loaded via <c>Session.BatchID</c>.
///
/// SIMPLIFIED: the legacy pages forked behaviour on <c>IsPreCassetted</c> —
/// pre-cassette batches copied BATCH_SUBMISSION/BATCH_ANIMAL/BATCH_TISSUES rows,
/// already-cassetted batches copied BATCH_BLOCK/BATCH_BLOCK_ANIMAL rows instead.
/// This page always copies at the submission level (batch submissions, animals,
/// tissues) regardless of cassette status. Duplicating an already-cassetted
/// batch's blocks is not reproduced here — the "Copy blocks" and "Copy samples"
/// workflows (see <c>Pages/Blocks/CopyBlocks.cshtml</c> and
/// <c>Pages/Blocks/CopySamples.cshtml</c>) already cover block-level duplication.
/// The per-sample "change sender ref" sub-flow (legacy: <c>AddSubmission.aspx</c>
/// round-trip) is simplified to an inline editable field per row.
/// </summary>
public class CopyBatchModel : HistoPageModel
{
    private readonly IBatchService _batches;
    private readonly ISubmissionService _submissions;

    public CopyBatchModel(ISessionService session, IBatchService batches, ISubmissionService submissions)
        : base(session)
    {
        _batches = batches;
        _submissions = submissions;
    }

    [BindProperty] public int SourceBatchId { get; set; }
    [BindProperty] public string NewCustomerRef { get; set; } = string.Empty;
    [BindProperty] public List<AnimalRow> Animals { get; set; } = [];

    public Batch? SourceBatch { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(int sourceBatchId)
    {
        ViewData["Title"] = "Copy Submission";
        ViewData["PageTitle"] = "Copy Submission";

        SourceBatchId = sourceBatchId;
        SourceBatch = await _batches.GetByIdAsync(sourceBatchId);
        if (SourceBatch is null)
        {
            Error = "The submission to copy could not be found.";
            return Page();
        }

        NewCustomerRef = SourceBatch.CustomerRef;
        var submissions = await _submissions.GetSubmissionsByBatchAsync(sourceBatchId);
        var animals = await _submissions.GetAnimalsByBatchAsync(sourceBatchId);

        Animals = animals
            .OrderBy(a => a.SenderRef)
            .Select(a => new AnimalRow
            {
                AnimalId = a.ID,
                SubmissionId = a.BatchSubmissionID,
                SenderRef = a.SenderRef,
                NewSenderRef = a.SenderRef,
            })
            .ToList();

        _ = submissions; // loaded to confirm the batch has submission data; rows are driven by animals
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Copy Submission";
        ViewData["PageTitle"] = "Copy Submission";

        SourceBatch = await _batches.GetByIdAsync(SourceBatchId);
        if (SourceBatch is null)
        {
            Error = "The submission to copy could not be found.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NewCustomerRef))
        {
            Error = "Enter a customer reference for the new submission.";
            return Page();
        }

        var userId = Session.UserID;
        var batchToCopy = new Batch
        {
            Status = SourceBatch.Status,
            CustomerRef = NewCustomerRef,
            Comments = SourceBatch.Comments,
            SubmittedByUserID = SourceBatch.SubmittedByUserID,
            UserAreaCode = SourceBatch.UserAreaCode,
            IsPreCassetted = SourceBatch.IsPreCassetted,
        };
        var newBatchId = await _batches.CopyBatchHeaderAsync(batchToCopy, userId);
        if (newBatchId <= 0)
        {
            Error = "Failed to create the new submission.";
            return Page();
        }

        var submissions = await _submissions.GetSubmissionsByBatchAsync(SourceBatchId);
        var animals = await _submissions.GetAnimalsByBatchAsync(SourceBatchId);
        var newSenderRefsByAnimalId = Animals.ToDictionary(a => a.AnimalId, a => a.NewSenderRef);

        foreach (var submission in submissions)
        {
            var newSubmissionId = await _submissions.CopySubmissionAsync(submission, newBatchId, userId);
            if (newSubmissionId <= 0) continue;

            var tissues = await _submissions.GetTissuesBySubmissionAsync(submission.ID);
            foreach (var tissue in tissues)
                await _submissions.CopyTissueAsync(tissue, newSubmissionId, userId);

            foreach (var animal in animals.Where(a => a.BatchSubmissionID == submission.ID))
            {
                var newSenderRef = newSenderRefsByAnimalId.GetValueOrDefault(animal.ID, animal.SenderRef);
                await _submissions.CopyAnimalAsync(animal, newSubmissionId, newSenderRef, userId);
            }
        }

        return RedirectToPage("/Batches/CopyBatchSummary", new { newBatchId });
    }

    /// <summary>One editable row of the source submission's samples.</summary>
    public class AnimalRow
    {
        public int AnimalId { get; set; }
        public int SubmissionId { get; set; }
        public string SenderRef { get; set; } = string.Empty;
        public string NewSenderRef { get; set; } = string.Empty;
    }
}
