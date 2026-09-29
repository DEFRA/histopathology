using Histo.Administration.Interfaces;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
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
    private readonly IBlockService _blocks;

    public AddSubmissionModel(ISessionService session, ISubmissionService submissions, IBatchService batches, ILookupService lookups, IBlockService blocks)
        : base(session)
    {
        _submissions = submissions;
        _batches = batches;
        _lookups = lookups;
        _blocks = blocks;
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

    /// <summary>Explicit return page for this flow, used by the Back/Cancel links when the user arrives from a page other than the default batch list.</summary>
    [BindProperty(SupportsGet = true)] public string? ReturnPage { get; set; }

    /// <summary>Resolved back-link for this page. Falls back to any session-scoped return context, then to the batch list.</summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(ReturnPage)
        ? string.IsNullOrWhiteSpace(Session.ReturnPage) ? "/Batches/BatchesNotReceived" : Session.ReturnPage
        : ReturnPage;

    /// <summary>
    /// True when reached from the "Assign Tissues to Blocks" journey (<c>BatchBlocks.cshtml</c>'s
    /// "Add sample" button), as opposed to Create/Edit Submission. Matches the same
    /// substring-on-ReturnPage convention already used by
    /// <see cref="Histo.Web.Pages.Submissions.SubmissionDetailsBlockModel.IsAssignTissueMode"/>.
    /// No user-area restriction. Strict: always requires picking an existing sample, since
    /// BatchBlocks is only ever reached once a Received/InProgress batch's samples already exist.
    ///
    /// Legacy had two distinct pages here: <c>AddSubmission.aspx</c> (Create/Edit Submission — types
    /// a brand-new Sender Ref) and <c>AddSample.aspx</c> (Assign Tissues to Blocks — associates an
    /// *existing* sample, found via search, with the current batch; confirmed via
    /// docs/Functionality-Traceability-Matrix.md and this session's own prior finding that
    /// AddSample.aspx "is not a separate sample-creation workflow — it's the landing page for
    /// adding an existing animal to the current batch"). Both were consolidated onto this one page
    /// with a single free-text field, which incorrectly let the Assign Tissues journey type a new
    /// Sender Ref instead of picking one of the batch's own not-yet-blocked samples.
    ///
    /// Excludes "Copy sample" (<see cref="SourceAnimalId"/> set) — that flow always creates a
    /// genuinely new Animal with copied tissues, never picks an existing one, regardless of how
    /// stale <see cref="ISessionService.SampleDetailReturnPage"/> might be at that point.
    /// </summary>
    public bool IsAssignTissueMode =>
        (SourceAnimalId is null or <= 0)
        && ((ReturnPage ?? string.Empty).Contains("/Batches/BatchBlocks", StringComparison.OrdinalIgnoreCase)
            || (Session.SampleDetailReturnPage ?? string.Empty).Contains("/Batches/BatchBlocks", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Samples already in this batch that don't yet have a block — the only valid choices when
    /// <see cref="IsAssignTissueMode"/>, populated in <see cref="OnGetAsync"/>.
    /// </summary>
    public IReadOnlyList<Animal> AvailableAnimals { get; private set; } = [];

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

        if (IsAssignTissueMode && BatchId is > 0)
            AvailableAnimals = await GetUnblockedAnimalsAsync(BatchId.Value);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Add sample";

        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage("/Index");

        if (IsAssignTissueMode)
        {
            var available = await GetUnblockedAnimalsAsync(batchId.Value);
            AvailableAnimals = available;

            // No new Animal is created here — the user is picking one of the batch's own
            // samples that still needs blocks assigned, mirroring legacy AddSample.aspx.
            var chosen = available.FirstOrDefault(a => string.Equals(a.SenderRef, SenderRef, StringComparison.OrdinalIgnoreCase));
            if (chosen is null)
            {
                ModelError = "Select a sample from the list.";
                return Page();
            }

            Session.SampleDetailReturnPage = BackLinkPage;
            return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId, animalId = chosen.ID });
        }

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
        var newAnimalId = await _submissions.AddAnimalAsync(
            submissionId.Value,
            SenderRef,
            Session.UserID,
            pmDate: null,
            pmDateSet: false);
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

                // Copy sample started from Sample Summary — return there so it's clear the new
                // sample was added, rather than continuing straight into its (empty) detail page.
                return RedirectToPage("/Submissions/SampleSummary", new { batchId });
            }

            Session.SampleDetailReturnPage = BackLinkPage;
            return RedirectToPage("/Submissions/SubmissionDetails", new { batchId, animalId = newAnimalId });
        }

        if (SourceAnimalId is > 0)
            return RedirectToPage("/Submissions/SampleSummary", new { batchId });

        Session.SampleDetailReturnPage = BackLinkPage;
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

    /// <summary>Samples in the batch that have no block yet — the pickable set for <see cref="IsAssignTissueMode"/>.</summary>
    private async Task<IReadOnlyList<Animal>> GetUnblockedAnimalsAsync(int batchId)
    {
        var animals = await _submissions.GetAnimalsByBatchAsync(batchId);
        var blockedAnimalIds = (await _blocks.GetByBatchAsync(batchId)).Select(b => b.AnimalID).ToHashSet();
        return animals.Where(a => !blockedAnimalIds.Contains(a.ID)).ToList();
    }
}
