using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>
/// Replaces <c>ViewSamples.aspx</c> — lists animals for the current batch submission.
///
/// Also serves as the current equivalent of legacy <c>BatchSummary.aspx</c> /
/// <c>BatchBlockSummary.aspx</c> (both were sample-list screens reached from the same
/// in-progress-batch wizard; their hierarchical grid, inline histology-ref editing, and
/// paging were superseded by this flat list plus the per-animal
/// <see cref="SubmissionDetailsModel"/>/<see cref="SubmissionDetailsBlockModel"/> detail
/// pages, consistent with the grid-consolidation precedent already established for
/// <c>CopySamplesSummary</c>). The "Edit submission" and "Delete submission" actions from
/// both legacy pages are exposed here via <see cref="OnPostEditAsync"/> and
/// <see cref="OnPostDeleteAsync"/>.
/// </summary>
public class ViewSamplesModel : HistoPageModel
{
    private readonly SubmissionService _submissions;

    public ViewSamplesModel(ISessionService session, SubmissionService submissions)
        : base(session) => _submissions = submissions;

    public IReadOnlyList<Animal> Animals { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "View Samples";
        ViewData["PageTitle"] = "View Samples";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");
        Animals = await _submissions.GetAnimalsByBatchAsync(Session.BatchID ?? 0);
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
