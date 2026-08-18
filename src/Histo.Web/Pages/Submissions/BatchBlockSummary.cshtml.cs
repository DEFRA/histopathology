using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>
/// Replaces legacy <c>BatchSummary.aspx</c> / <c>BatchBlockSummary.aspx</c> — both were
/// sample-list screens for the in-progress batch wizard (non-cassetted vs. cassetted
/// submissions respectively), reached via <c>BatchDetails.aspx</c>'s "Samples" button.
/// Their hierarchical grid, inline histology-ref editing, and paging are superseded by
/// this flat list plus the per-animal <see cref="SubmissionDetailsModel"/>/
/// <see cref="SubmissionDetailsBlockModel"/> detail pages, consistent with the
/// grid-consolidation precedent already established for <c>CopySamplesSummary</c>.
/// The "Edit submission" and "Delete submission" actions from both legacy pages are
/// exposed here via <see cref="OnPostEditAsync"/> and <see cref="OnPostDeleteAsync"/>.
///
/// NOTE: this page is distinct from legacy <c>ViewSamples.aspx</c> (a standalone,
/// non-batch-scoped tissue/block search reached from the Home page) — see
/// <see cref="ViewSamplesModel"/> for that page's migration.
/// </summary>
public class BatchBlockSummaryModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;
    private readonly IBatchService _batches;

    public BatchBlockSummaryModel(ISessionService session, ISubmissionService submissions, IBatchService batches)
        : base(session)
    {
        _submissions = submissions;
        _batches = batches;
    }

    public IReadOnlyList<Animal> Animals { get; private set; } = [];

    public Batch? Batch { get; private set; }

    /// <summary>
    /// Gates Add sample / Delete sample / Copy sample.
    /// Legacy source: <c>BatchSummary.aspx.vb</c> / <c>BatchBlockSummary.aspx.vb</c>::<c>EnableDisableButtons</c> —
    /// these three actions were only enabled in "Editing Batch" and "Creating New Batch" session modes (which
    /// produce identical button availability), and force-disabled in "View Submission" mode. Since the new app has
    /// no session-mode equivalent and this page is shared by the batch-creation wizard (<c>AddSubmission</c>/
    /// <c>AddSample</c>) as well as the read-only "Samples" link from <c>BatchDetails</c>, the legacy modes are
    /// re-derived from <see cref="Batch"/>.Status, matching the existing <c>CanEditSubmission</c> gate used on
    /// <c>ViewSubmissions</c>/<c>SearchSubmissions</c> (Submitted/Rejected = still editable; Received/InProgress/
    /// Completed/OnHold = locked). Edit sample (block details) is intentionally NOT gated by this property — legacy
    /// <c>BatchBlockSummary.aspx.vb</c> keeps Edit selectable in View mode (unlike <c>BatchSummary.aspx.vb</c>,
    /// which force-disables it too), since block assignment/viewing continues after a batch has been received.
    /// </summary>
    public bool CanModifySamples => Batch?.Status is BatchStatus.Submitted or BatchStatus.Rejected;

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Sample summary";
        ViewData["PageTitle"] = "Sample summary";
        if (Session.BatchID is null or <= 0) return RedirectToPage("/Index");
        var batchId = Session.BatchID.Value;
        Batch = await _batches.GetByIdAsync(batchId);
        Animals = await _submissions.GetAnimalsByBatchAsync(batchId);

        // Ensure BatchSubmissionID is populated in session so that AddSubmission / AddSample
        // have a valid parent record when adding a new animal.

        // Primary strategy: if animals were loaded and carry a valid BatchSubmissionID,
        // use the first animal's submission ID directly (avoids the QueryMultiple overhead).
        var firstAnimalSubId = Animals.FirstOrDefault(a => a.BatchSubmissionID > 0)?.BatchSubmissionID;
        if (firstAnimalSubId is > 0)
        {
            Session.BatchSubmissionID = firstAnimalSubId.Value;
        }
        else
        {
            // Secondary: query the batch submissions directly.
            var submissions = await _submissions.GetSubmissionsByBatchAsync(batchId);
            if (submissions.Count > 0)
            {
                Session.BatchSubmissionID = submissions[0].ID;
            }
            else
            {
                // First visit for this batch: create the default batch submission record.
                var sub = new BatchSubmission { BatchID = batchId, SubmissionName = "Default", Order = 1 };
                var subId = await _submissions.AddSubmissionAsync(sub, Session.UserID);
                if (subId > 0) Session.BatchSubmissionID = subId;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSelectAsync(int animalId)
    {
        Session.AnimalID = animalId;
        return RedirectToPage("/Blocks/BlockDetails");
    }

    /// <summary>
    /// Replaces the legacy "Edit submission" action (<c>BatchSummary.aspx.vb</c>::<c>btnEditSubmission_Click</c>),
    /// which stored the selected animal in session and redirected to <c>SubmissionDetails.aspx</c>.
    /// </summary>
    public IActionResult OnPostEditAsync(int animalId)
    {
        Session.AnimalID = animalId;
        return RedirectToPage("/Submissions/SubmissionDetails");
    }

    /// <summary>
    /// Replaces the legacy "Delete submission" action (<c>BatchSummary.aspx.vb</c>::<c>btnDeleteSubmission_Click</c>
    /// / <c>BatchBlockSummary.aspx.vb</c>::<c>btnDeleteSubmission_Click</c>), both of which removed the animal
    /// record from the in-progress batch.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(int animalId)
    {
        await _submissions.DeleteAnimalAsync(animalId, Session.UserID);
        return RedirectToPage();
    }
}
