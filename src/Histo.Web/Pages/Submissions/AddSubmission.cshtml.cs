using Histo.Administration.Interfaces;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Submissions;

/// <summary>Replaces <c>AddSubmission.aspx</c>.</summary>
public class AddSubmissionModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;
    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;

    public AddSubmissionModel(ISessionService session, ISubmissionService submissions, IBatchService batches, ILookupService lookups)
        : base(session)
    {
        _submissions = submissions;
        _batches = batches;
        _lookups = lookups;
    }

    /// <summary>Batch ID from the URL (route/query). Falls back to <see cref="ISessionService.BatchID"/>.</summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>Submission ID carried through the form so POST never depends on session alone.</summary>
    [BindProperty(SupportsGet = true)] public int? BatchSubmissionId { get; set; }

    [BindProperty] public string SenderRef   { get; set; } = string.Empty;

    /// <summary>
    /// Set when this form was reached via "Copy sample" — the animal whose tissues should be
    /// duplicated onto the newly created sample. Round-tripped via a hidden field so it survives
    /// the POST (and the SearchSender picker detour, which only restores <see cref="SenderRef"/>).
    /// </summary>
    [BindProperty] public int? SourceAnimalId { get; set; }

    public string? ModelError { get; private set; }

    public async Task OnGetAsync(string? senderRef, int? sourceAnimalId)
    {
        ViewData["Title"] = "Add sample";
        BatchId ??= Session.BatchID;
        if (sourceAnimalId is > 0) SourceAnimalId = sourceAnimalId;

        // Pre-resolve submission ID so the form POST never needs AddSubmissionAsync.
        BatchSubmissionId ??= Session.BatchSubmissionID;
        if ((BatchSubmissionId is null or <= 0) && BatchId is > 0)
        {
            var existing = await _submissions.GetSubmissionsByBatchAsync(BatchId.Value);
            if (existing.Count > 0) BatchSubmissionId = existing[0].ID;
        }

        // Restore the sender ref chosen via the Search Sender picker (SearchSender.cshtml),
        // or pre-fill from the "Copy sample" query parameter (SampleSummary).
        if (TempData.TryGetValue("SenderRefPicker_Selected", out var chosen) && chosen is string chosenRef)
            SenderRef = chosenRef;
        else if (!string.IsNullOrWhiteSpace(senderRef))
            SenderRef = senderRef;
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

        // Legacy source: AddSubmission.aspx.vb — bNeuropath is derived from the user's area
        // (SV_HeaderUserArea = "Neuropath"), never from a manual form control.
        var isNeuropath = Session.UserArea == "Neuropath";
        var newAnimalId = await _submissions.AddAnimalAsync(submissionId.Value, SenderRef, isNeuropath, Session.UserID);
        if (newAnimalId <= 0)
        {
            // AddAnimalAsync swallows the underlying SQL exception and returns 0 on failure —
            // redirecting anyway here previously hid the fact that no sample was actually saved.
            ModelError = "Could not add the sample. Please try again.";
            return Page();
        }

        // Legacy AddSubmission.aspx::btnNext_Click continues straight into the per-sample detail
        // page (SubmissionDetailsBlock.aspx / SubmissionDetails.aspx) rather than back to the list.
        var submittedAsCode = await _batches.GetSubmittedAsCodeAsync(batchId.Value);
        var isWetTissue = await IsWetTissueCodeAsync(submittedAsCode);

        // Every new animal needs its own dedicated BatchSubmission row with AnimalID set — the
        // "GetBatchAnimal" SP (used by both SubmissionDetails and SubmissionDetailsBlock to find the
        // sample) joins on BatchSubmission.AnimalID; it never sees the shared "Default" submission
        // resolved above (that one only exists to satisfy AddAnimalAsync's batchSubmissionId parameter
        // — the AddAnimal SP itself has no BatchSubmissionID column to receive it). Without this row
        // the newly added animal is unreachable from either page, surfacing as "Sample not found"
        // immediately after "Add sample" — previously only fixed for the Wet Tissue branch, but the
        // link requirement is identical for block-type submissions too.
        var siblingSubmissions = await _submissions.GetSubmissionsByBatchAsync(batchId.Value);
        var nextOrder = siblingSubmissions.Count > 0 ? siblingSubmissions.Max(s => s.Order) + 1 : 1;
        var ownSubmissionId = await _submissions.AddSubmissionAsync(
            new BatchSubmission { BatchID = batchId.Value, AnimalID = newAnimalId, SubmissionName = "Default", Order = nextOrder },
            Session.UserID);
        if (ownSubmissionId > 0) Session.BatchSubmissionID = ownSubmissionId;

        if (isWetTissue)
        {
            // "Copy sample" — duplicate the source sample's tissues onto the new one (Wet Tissue only;
            // block-owned tissues on other submission types are copied via the separate Copy Blocks flow).
            if (SourceAnimalId is > 0 && ownSubmissionId > 0)
            {
                var sourceSubmission = siblingSubmissions.FirstOrDefault(s => s.AnimalID == SourceAnimalId);
                if (sourceSubmission is not null)
                {
                    var sourceTissues = await _submissions.GetTissuesBySubmissionAsync(batchId.Value, sourceSubmission.ID);
                    foreach (var tissue in sourceTissues)
                        await _submissions.CopyTissueAsync(tissue, ownSubmissionId, Session.UserID);
                }
            }

            return RedirectToPage("/Submissions/SubmissionDetails", new { batchId, animalId = newAnimalId });
        }

        return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId, animalId = newAnimalId });
    }

    /// <summary>
    /// Resolves a raw "Submitted As" code to its LOOKUP_SUBMITTEDAS (table 11) description and
    /// compares it to "Wet Tissue", matching Cassetted.aspx.vb's actual (description-based, not
    /// code-based) comparison. Replaces a previously hardcoded, unverified <c>code == "4"</c> guess.
    /// </summary>
    private async Task<bool> IsWetTissueCodeAsync(string? submittedAsCode)
    {
        if (string.IsNullOrEmpty(submittedAsCode)) return false;
        var items = await _lookups.GetLookupDataAsync(11); // LOOKUP_SUBMITTEDAS
        var match = items.FirstOrDefault(i => i.Code == submittedAsCode);
        return ValidationHelpers.IsWetTissueDescription(match?.Name);
    }
}
