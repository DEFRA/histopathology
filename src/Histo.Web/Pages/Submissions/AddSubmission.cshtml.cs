using Histo.Administration.Interfaces;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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

    /// <summary>Alternative to <see cref="SenderRef"/>: start of an "MC######" range (legacy AddSample.aspx/AddSubmission.aspx "Alternatively you can assign mouse ranges...").</summary>
    [BindProperty] public string MouseNumberFrom { get; set; } = string.Empty;

    /// <summary>End of the mouse-number range (inclusive). See <see cref="MouseNumberFrom"/>.</summary>
    [BindProperty] public string MouseNumberTo { get; set; } = string.Empty;

    /// <summary>
    /// Legacy gating (AddSubmission.aspx.vb::InitialiseUserAreaControls) requires BOTH the user
    /// area check AND a "copying a submission" context (legacy: reached via CopyBatch.aspx/
    /// CopyBatchBlocks.aspx). The modern Copy Batch journey (<c>CopyBatchModel</c>) no longer
    /// routes through this page at all, so the closest equivalent "copying" context here is the
    /// per-sample "Copy sample" action (<see cref="SourceAnimalId"/> set) — the plain Create/Edit
    /// Submission journey (brand-new Sender Ref, no SourceAnimalId) never qualifies.
    ///
    /// Legacy's area check also allowed "Mouse Bioassay" — dropped here because that
    /// <c>luUserArea</c> row has since been deactivated (migration
    /// V20260917_01_Deactivate_MouseBioassay_Neuropath_UserAreas.sql; see
    /// docs/Mouse-Bioassay-Neuropath-Removal-ChangeReport.md), so no user can be assigned it any
    /// more — only "Histopath" remains a reachable value.
    /// </summary>
    public bool ShowMouseRange => SourceAnimalId is > 0 && Session.UserArea == "Histopath";

    /// <summary>
    /// Set when this form was reached via "Copy sample" — the animal whose tissues should be
    /// duplicated onto the newly created sample. Round-tripped via a hidden field so it survives
    /// the POST (and the SearchSender picker detour, which only restores <see cref="SenderRef"/>).
    /// </summary>
    [BindProperty] public int? SourceAnimalId { get; set; }
    /// <summary>Index of the row being edited on the copy-submission page. When the user is returned to
    /// <c>/Batches/CopyBatch</c>, this lets us persist the updated sender ref into the staged list before Finish.</summary>
    [BindProperty(SupportsGet = true)] public int? RowIndex { get; set; }
    /// <summary>Explicit return page for this flow, used by the Back/Cancel links when the user arrives from a page other than the default batch list.</summary>
    [BindProperty(SupportsGet = true)] public string? ReturnPage { get; set; }

    /// <summary>Resolved back-link for this page. Falls back to any session-scoped return context, then to the batch list.</summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(ReturnPage)
        ? string.IsNullOrWhiteSpace(Session.ReturnPage) ? "/Batches/BatchesNotReceived" : Session.ReturnPage
        : ReturnPage;

    /// <summary>
    /// True when reached from the "Assign Tissues to Blocks" journey (<c>BatchBlocks.cshtml</c>'s
    /// "Add sample" button), as opposed to Create/Edit Submission. No user-area restriction.
    /// Strict: always requires picking an existing sample, since BatchBlocks is only ever reached
    /// once a Received/InProgress batch's samples already exist.
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
    /// Deliberately checks ONLY <see cref="ReturnPage"/> (fresh every request — from the link's own
    /// query string on GET, the form's hidden field on POST) and never
    /// <see cref="ISessionService.SampleDetailReturnPage"/>. That session value is set by OTHER
    /// pages (<c>BatchBlocks</c>, <c>SampleSummary</c>, and this page's own POST handler) purely to
    /// drive THEIR OWN later back-link, never to describe how THIS page was reached — checking it
    /// here previously let a stale value from an earlier, unrelated visit to the Assign Tissue
    /// journey silently force dropdown mode onto a completely separate Create Submission journey
    /// later in the same browser session.
    ///
    /// Excludes "Copy sample" (<see cref="SourceAnimalId"/> set) — that flow always creates a
    /// genuinely new Animal with copied tissues, never picks an existing one.
    /// </summary>
    public bool IsAssignTissueMode =>
        (SourceAnimalId is null or <= 0)
        && (ReturnPage ?? string.Empty).Contains("/Batches/BatchBlocks", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Every sample in the batch — the pickable set for <see cref="IsAssignTissueMode"/>. Not
    /// filtered to "not yet blocked" samples: a single sample commonly needs several blocks added
    /// one at a time, so this picker must keep offering it back on every visit, matching legacy
    /// <c>AddSample.aspx</c> (a user-reported regression when this was filtered down to a
    /// disappearing "unblocked only" list).
    /// </summary>
    public IReadOnlyList<Animal> AvailableAnimals { get; private set; } = [];

    public string? ModelError { get; private set; }

    /// <summary>True when <see cref="ModelError"/> came from <see cref="OnPostMouseRangeAsync"/> — lets
    /// the view anchor the error summary link and inline error message at the mouse-number fields
    /// instead of Sender Ref.</summary>
    public bool MouseRangeHasError { get; private set; }

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
            AvailableAnimals = await GetAssignableAnimalsAsync(BatchId.Value);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Add sample";

        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage("/Index");

        if (IsAssignTissueMode)
        {
            var available = await GetAssignableAnimalsAsync(batchId.Value);
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

        var hasMouseRangeInput = ShowMouseRange && (!string.IsNullOrWhiteSpace(MouseNumberFrom) || !string.IsNullOrWhiteSpace(MouseNumberTo));
        var usingMouseRange = ShowMouseRange && !string.IsNullOrWhiteSpace(MouseNumberFrom) && !string.IsNullOrWhiteSpace(MouseNumberTo);
        var usingSenderRef = !string.IsNullOrWhiteSpace(SenderRef);
        if (hasMouseRangeInput && !usingMouseRange)
        {
            ModelError = "Enter both the from and to mouse numbers.";
            MouseRangeHasError = true;
            return Page();
        }
        if (usingSenderRef == usingMouseRange)
        {
            // Legacy: exactly one of Sender Ref or the mouse-number range must be filled in.
            ModelError = ShowMouseRange ? "Enter either the Sender ref or the mouse number ranges." : "Enter the sender reference.";
            return Page();
        }

        if (string.Equals(ReturnPage, "/Batches/CopyBatch", StringComparison.OrdinalIgnoreCase)
            && SourceAnimalId is > 0)
        {
            PersistCopyBatchState();
            return RedirectToPage("/Batches/CopyBatch", new { sourceBatchId = batchId });
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

        var batch = await _batches.GetByIdAsync(batchId.Value);

        if (usingMouseRange)
            return await OnPostMouseRangeAsync(batchId.Value, submissionId.Value, batch);

        var (newAnimalId, createError) = await CreateAnimalForSenderAsync(batchId.Value, submissionId.Value, batch, SenderRef);
        if (createError is not null)
        {
            ModelError = createError;
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

                // Copy-submission "Change" flow must return to the batch copy list so the user can
                // still press Finish and complete the copy. Otherwise they are stranded on the
                // sample summary page without a final submission action.
                if (string.Equals(ReturnPage, "/Batches/CopyBatch", StringComparison.OrdinalIgnoreCase))
                {
                    PersistCopyBatchState();
                    return RedirectToPage("/Batches/CopyBatch", new { sourceBatchId = batchId });
                }

                // Copy sample started from Sample Summary — return there so it's clear the new
                // sample was added, rather than continuing straight into its (empty) detail page.
                return RedirectToPage("/Submissions/SampleSummary", new { batchId });
            }

            Session.SampleDetailReturnPage = BackLinkPage;
            return RedirectToPage("/Submissions/SubmissionDetails", new { batchId, animalId = newAnimalId });
        }

        if (SourceAnimalId is > 0)
        {
            if (string.Equals(ReturnPage, "/Batches/CopyBatch", StringComparison.OrdinalIgnoreCase))
            {
                PersistCopyBatchState();
                return RedirectToPage("/Batches/CopyBatch", new { sourceBatchId = batchId });
            }
            return RedirectToPage("/Submissions/SampleSummary", new { batchId });
        }

        Session.SampleDetailReturnPage = BackLinkPage;
        return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId, animalId = newAnimalId });
    }

    private void PersistCopyBatchState()
    {
        if (TempData is null || !string.Equals(ReturnPage, "/Batches/CopyBatch", StringComparison.OrdinalIgnoreCase))
            return;

        if (TempData["CopyBatch_Animals"] is not string savedJson)
            return;

        var animals = JsonSerializer.Deserialize<List<CopyBatchModel.AnimalRow>>(savedJson) ?? [];
        if (RowIndex is >= 0 && RowIndex < animals.Count)
        {
            animals[RowIndex.Value].NewSenderRef = string.IsNullOrWhiteSpace(SenderRef)
                ? string.IsNullOrWhiteSpace(MouseNumberFrom) ? string.Empty : MouseNumberFrom.Trim()
                : SenderRef.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(SenderRef) || !string.IsNullOrWhiteSpace(MouseNumberFrom))
        {
            animals.Add(new CopyBatchModel.AnimalRow
            {
                AnimalId = SourceAnimalId ?? 0,
                SubmissionId = BatchSubmissionId ?? 0,
                SenderRef = string.IsNullOrWhiteSpace(SenderRef) ? MouseNumberFrom.Trim() : SenderRef.Trim(),
                NewSenderRef = string.IsNullOrWhiteSpace(SenderRef) ? MouseNumberFrom.Trim() : SenderRef.Trim(),
            });
        }

        TempData["CopyBatch_Animals"] = JsonSerializer.Serialize(animals);
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

    /// <summary>Every sample in the batch — the pickable set for <see cref="IsAssignTissueMode"/>.</summary>
    private async Task<IReadOnlyList<Animal>> GetAssignableAnimalsAsync(int batchId) =>
        await _submissions.GetAnimalsByBatchAsync(batchId);

    /// <summary>
    /// Creates (or, for pre-cassetted batches, reuses the pre-booked placeholder for) a single
    /// Animal for <paramref name="senderRef"/>. Shared by the single Sender Ref path and the
    /// mouse-range loop in <see cref="OnPostMouseRangeAsync"/>.
    /// </summary>
    private async Task<(int AnimalId, string? Error)> CreateAnimalForSenderAsync(int batchId, int submissionId, Batch? batch, string senderRef)
    {
        // Pre-cassetted samples may only use a block ref already pre-booked for this sender
        // (Book Blocks) — legacy: clsBlock.NewBlock -> GetPreBookedBlock. Checking here, before
        // navigating away, avoids the user only finding out on the block page that nothing is
        // available. Reuses the pre-booked animal's own ID so its pre-booked blocks carry over,
        // rather than creating a separate, unbooked Animal for the same sender.
        // Regression note: this check existed previously and was silently dropped as collateral
        // damage by an unrelated refactor (commit e40a473) that removed the IBlockService
        // dependency this page no longer used for anything else at the time — restored here.
        if (batch?.IsPreCassetted == true)
        {
            // A sender ref can match more than one animal row (e.g. an unrelated past submission
            // with no pre-booked blocks, alongside the actual pre-booked placeholder) — check each
            // candidate rather than assuming the first match is the pre-booked one.
            Block[] preBookedBlocks = [];
            SenderSearchResult? preBookedAnimal = null;
            foreach (var candidate in await _submissions.GetAnimalBySenderAsync(senderRef))
            {
                var candidateBlocks = await _blocks.GetPreBookedByAnimalAsync(candidate.ID);
                if (candidateBlocks.Count == 0) continue;
                preBookedAnimal = candidate;
                preBookedBlocks = [.. candidateBlocks];
                break;
            }

            if (preBookedAnimal is null || preBookedBlocks.Length == 0)
                return (0, "This sender reference has no pre-booked block. Book a block reference for this sender before adding the sample.");

            return (preBookedAnimal.ID, null);
        }

        var newAnimalId = await _submissions.AddAnimalAsync(submissionId, senderRef, Session.UserID, pmDate: null, pmDateSet: false);
        // AddAnimalAsync swallows the underlying SQL exception and returns 0 on failure —
        // redirecting anyway here previously hid the fact that no sample was actually saved.
        return newAnimalId > 0 ? (newAnimalId, null) : (0, "Could not add the sample. Please try again.");
    }

    /// <summary>
    /// Legacy "Alternatively you can assign mouse ranges..." — creates one Animal per MC number in
    /// [<see cref="MouseNumberFrom"/>, <see cref="MouseNumberTo"/>], each with its own BatchSubmission
    /// row, then returns to Sample Summary rather than a single sample's detail page.
    /// </summary>
    private async Task<IActionResult> OnPostMouseRangeAsync(int batchId, int submissionId, Batch? batch)
    {
        const int maxMouseRangeEntries = 1000;

        var from = MouseNumberFrom.Trim().ToUpperInvariant();
        var to = MouseNumberTo.Trim().ToUpperInvariant();

        if (from.Length != 8 || to.Length != 8
            || !ValidationHelpers.ValidateMouseNumber(from) || !ValidationHelpers.ValidateMouseNumber(to)
            || !SenderRefHelpers.TryParseMouseNumber(from, out var fromId) || !SenderRefHelpers.TryParseMouseNumber(to, out var toId))
        {
            ModelError = "The mouse number format is MC followed by 6 digits, i.e. MC000105.";
            MouseRangeHasError = true;
            return Page();
        }

        if (fromId >= toId)
        {
            ModelError = "The from number cannot be greater than the to number.";
            MouseRangeHasError = true;
            return Page();
        }

        var rangeSize = toId - fromId + 1;
        if (rangeSize > maxMouseRangeEntries)
        {
            ModelError = $"The mouse number range cannot exceed {maxMouseRangeEntries} entries. Use a smaller range or create a bulk job.";
            MouseRangeHasError = true;
            return Page();
        }

        // Validate the whole range against samples already in this batch before creating anything —
        // this keeps the duplicate check bounded without materialising a million-item list in memory.
        var existingInBatch = await _submissions.GetAnimalsByBatchAsync(batchId);
        string? duplicate = null;
        for (var current = fromId; current <= toId; current++)
        {
            var candidate = SenderRefHelpers.FormatMouseNumber(current);
            if (existingInBatch.Any(a => string.Equals(a.SenderRef, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                duplicate = candidate;
                break;
            }
        }

        if (duplicate is not null)
        {
            ModelError = $"Mouse number {duplicate} already exists on the submission. Alter the range and try again.";
            MouseRangeHasError = true;
            return Page();
        }

        if (string.Equals(ReturnPage, "/Batches/CopyBatch", StringComparison.OrdinalIgnoreCase)
            && SourceAnimalId is > 0)
        {
            PersistCopyBatchState();
            return RedirectToPage("/Batches/CopyBatch", new { sourceBatchId = batchId });
        }

        var created = await _submissions.CreateMouseRangeAsync(batchId, SourceAnimalId, from, to, Session.UserID);
        if (!created)
        {
            ModelError = "Could not add the sample range. Please try again.";
            MouseRangeHasError = true;
            return Page();
        }

        if (string.Equals(ReturnPage, "/Batches/CopyBatch", StringComparison.OrdinalIgnoreCase))
        {
            PersistCopyBatchState();
            return RedirectToPage("/Batches/CopyBatch", new { sourceBatchId = batchId });
        }

        return RedirectToPage("/Submissions/SampleSummary", new { batchId });
    }
}
