using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces <c>CopyBatchBlocksSummary.aspx</c> — displays the outcome of a
/// "Copy submission" operation (see <see cref="CopyBatchModel"/>).
/// </summary>
public class CopyBatchSummaryModel : HistoPageModel
{
    private readonly IBatchService _batches;
    private readonly ISubmissionService _submissions;

    public CopyBatchSummaryModel(ISessionService session, IBatchService batches, ISubmissionService submissions)
        : base(session)
    {
        _batches = batches;
        _submissions = submissions;
    }

    public int NewBatchId { get; private set; }
    public Batch? NewBatch { get; private set; }
    public int SubmissionCount { get; private set; }
    public int AnimalCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int newBatchId)
    {
        ViewData["Title"] = "Submission Copied";
        ViewData["PageTitle"] = "Submission Copied";

        // Returning here from SampleSummary/EditBatch carries no newBatchId — SampleSummary's back
        // link sends BatchId and its Finish handler sends no route value at all — so fall back to
        // the copied batch the handlers below stored in session.
        NewBatchId = newBatchId > 0 ? newBatchId : Session.BatchID ?? 0;
        if (NewBatchId <= 0) return RedirectToPage("/Index");

        NewBatch = await _batches.GetByIdAsync(NewBatchId);
        if (NewBatch is null) return RedirectToPage("/Index");

        var submissions = await _submissions.GetSubmissionsByBatchAsync(NewBatchId);
        SubmissionCount = submissions.Count;
        AnimalCount = (await _submissions.GetAnimalsByBatchAsync(NewBatchId)).Count;
        return Page();
    }

    /// <summary>Sets the new batch as the active session batch and navigates to its details.</summary>
    public IActionResult OnPostViewAsync(int newBatchId)
    {
        Session.BatchID = newBatchId;
        // A freshly copied batch is a new, fully editable Submitted-status batch — never inherit a
        // stale read-only flag from an earlier, unrelated ViewSubmissions/SearchSubmissions visit.
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/Batches/BatchDetails");
    }

    public IActionResult OnPostSamplesAsync(int newBatchId)
    {
        Session.BatchID = newBatchId;
        Session.IsViewSubmissionMode = false;
        Session.SampleSummaryReturnPage = "/Batches/CopyBatchSummary";
        return RedirectToPage("/Submissions/SampleSummary", new { batchId = newBatchId });
    }

    public IActionResult OnPostEditAsync(int newBatchId)
    {
        Session.BatchID = newBatchId;
        Session.IsViewSubmissionMode = false;
        Session.SampleSummaryReturnPage = "/Batches/CopyBatchSummary";
        Session.EditBatchReturnPage = "/Batches/CopyBatchSummary";
        return RedirectToPage("/Batches/EditBatch");
    }
}
