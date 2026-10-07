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
    private readonly IBlockTestService _blockTests;
    private readonly IHistologyRefService _histologyRefs;

    public AddSubmissionModel(ISessionService session, ISubmissionService submissions, IBatchService batches, ILookupService lookups,
        IBlockService blocks, IBlockTestService blockTests, IHistologyRefService histologyRefs)
        : base(session)
    {
        _submissions = submissions;
        _batches = batches;
        _lookups = lookups;
        _blocks = blocks;
        _blockTests = blockTests;
        _histologyRefs = histologyRefs;
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
    /// True when reached from the Copy Submission "Change" button. Matched on the path only:
    /// <c>CopyBatchModel.OnPostPick</c> sends <c>/Batches/CopyBatch?sourceBatchId=N</c>, so an exact
    /// string comparison never matched and the flow fell through to the normal create-and-redirect
    /// path, writing samples to the source batch and stranding the user on Sample Summary with no
    /// way back to Finish.
    /// </summary>
    private bool IsCopyBatchReturn =>
        (ReturnPage ?? string.Empty).Split('?')[0]
            .Equals("/Batches/CopyBatch", StringComparison.OrdinalIgnoreCase);

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
            return await HandleAssignTissueModeAsync(batchId.Value);

        var inputError = ValidateSubmissionInput();
        if (inputError is not null)
            return inputError;

        if (ShouldRedirectBackToCopyBatch())
        {
            // Staging only — the copy journey writes nothing until the Create Submission form is submitted.
            if (ShouldUseMouseRange())
            {
                var (rangeRefs, rangeError) = await ResolveMouseRangeRefsAsync();
                if (rangeError is not null)
                {
                    ModelError = rangeError;
                    MouseRangeHasError = true;
                    return Page();
                }
                PersistCopyBatchState(rangeRefs);
            }
            else
            {
                PersistCopyBatchState();
            }

            return RedirectToPage("/Batches/CopyBatch", new { sourceBatchId = batchId });
        }

        var submissionId = await GetOrCreateSubmissionIdAsync(batchId.Value);
        if (submissionId is null or <= 0)
        {
            ModelError = "Could not add the sample. Please try again.";
            return Page();
        }

        Session.BatchSubmissionID = submissionId;
        var batch = await _batches.GetByIdAsync(batchId.Value);

        if (ShouldUseMouseRange())
            return await OnPostMouseRangeAsync(batchId.Value, submissionId.Value, batch);

        var (newAnimalId, createError) = await CreateAnimalForSenderAsync(batchId.Value, submissionId.Value, batch, SenderRef);
        if (createError is not null)
        {
            ModelError = createError;
            return Page();
        }

        return await CompleteSampleCreationAsync(batchId.Value, newAnimalId, submissionId.Value);
    }

    private async Task<IActionResult> HandleAssignTissueModeAsync(int batchId)
    {
        var available = await GetAssignableAnimalsAsync(batchId);
        AvailableAnimals = available;

        var chosen = available.FirstOrDefault(a => string.Equals(a.SenderRef, SenderRef, StringComparison.OrdinalIgnoreCase));
        if (chosen is null)
        {
            ModelError = "Select a sample from the list.";
            return Page();
        }

        Session.SampleDetailReturnPage = BackLinkPage;
        return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId, animalId = chosen.ID });
    }

    private IActionResult? ValidateSubmissionInput()
    {
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
            ModelError = ShowMouseRange ? "Enter either the Sender ref or the mouse number ranges." : "Enter the sender reference.";
            return Page();
        }

        return null;
    }

    private bool ShouldUseMouseRange() => ShowMouseRange && !string.IsNullOrWhiteSpace(MouseNumberFrom) && !string.IsNullOrWhiteSpace(MouseNumberTo);

    /// <summary>
    /// True for the Copy Submission "Change" flow. Matched on path only via
    /// <see cref="IsCopyBatchReturn"/> — the return page carries a <c>?sourceBatchId=</c> query string.
    /// </summary>
    private bool ShouldRedirectBackToCopyBatch() => IsCopyBatchReturn && SourceAnimalId is > 0;

    private async Task<int?> GetOrCreateSubmissionIdAsync(int batchId)
    {
        var submissionId = BatchSubmissionId ?? Session.BatchSubmissionID;
        if (submissionId is not null and > 0)
            return submissionId;

        var existing = await _submissions.GetSubmissionsByBatchAsync(batchId);
        if (existing.Count > 0)
            return existing[0].ID;

        return await _submissions.AddSubmissionAsync(
            new BatchSubmission { BatchID = batchId, SubmissionName = "Default", Order = 1 },
            Session.UserID);
    }

    private async Task<IActionResult> CompleteSampleCreationAsync(int batchId, int newAnimalId, int submissionId)
    {
        var submittedAsCode = await _batches.GetSubmittedAsCodeAsync(batchId);
        var isWetTissue = await IsWetTissueCodeAsync(submittedAsCode);

        var siblingSubmissions = await _submissions.GetSubmissionsByBatchAsync(batchId);
        var nextOrder = siblingSubmissions.Count > 0 ? siblingSubmissions.Max(s => s.Order) + 1 : 1;
        var ownSubmissionId = await _submissions.AddSubmissionAsync(
            new BatchSubmission { BatchID = batchId, AnimalID = newAnimalId, SubmissionName = "Default", Order = nextOrder },
            Session.UserID);
        if (ownSubmissionId > 0) Session.BatchSubmissionID = ownSubmissionId;

        if (isWetTissue)
            return await HandleWetTissueCreationAsync(batchId, newAnimalId, siblingSubmissions, ownSubmissionId);

        if (SourceAnimalId is > 0)
        {
            // Block-type "Copy sample": reproduce the source sample's blocks and give the copy its
            // own histology ref, so it lands complete rather than needing Copy blocks afterwards.
            try
            {
                await CopySourceSampleToAsync(batchId, newAnimalId);
            }
            catch (InvalidOperationException ex)
            {
                // The sample itself exists by now, so say so rather than implying nothing happened.
                ModelError = $"Sample '{SenderRef}' was created, but the copy did not finish. {ex.Message}";
                return Page();
            }

            if (IsCopyBatchReturn)
            {
                PersistCopyBatchState();
                return RedirectToPage("/Batches/CopyBatch", new { sourceBatchId = batchId });
            }
            return RedirectToPage("/Submissions/SampleSummary", new { batchId });
        }

        Session.SampleDetailReturnPage = BackLinkPage;
        return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId, animalId = newAnimalId });
    }

    /// <summary>Gives one newly created sample the source sample's blocks and its own histology ref.</summary>
    private async Task CopySourceSampleToAsync(int batchId, int newAnimalId)
    {
        var animals = await _submissions.GetAnimalsByBatchAsync(batchId);
        var source = animals.FirstOrDefault(a => a.ID == SourceAnimalId);
        var target = animals.FirstOrDefault(a => a.ID == newAnimalId);
        if (source is null || target is null) return;

        var allBlocks = await _blocks.GetByBatchAsync(batchId);
        await CopySourceSampleToTargetAsync(batchId, allBlocks, source, target);
    }

    /// <summary>
    /// Shared per-sample copy step: blocks (with tissues and test ticks), then the histology ref,
    /// saved in one update so the advanced NextBlockRef goes with it.
    /// </summary>
    private async Task CopySourceSampleToTargetAsync(int batchId, IReadOnlyList<Block> allBlocks, Animal source, Animal target)
    {
        var sourceBlocks = allBlocks.Where(b => b.AnimalID == source.ID).ToList();
        if (sourceBlocks.Count > 0)
        {
            var allTests = await _blockTests.GetAllSelectionsByBatchAsync(batchId);
            await SampleCopyHelper.CopyBlocksToAnimalAsync(
                _blocks, _submissions, _blockTests, sourceBlocks, allBlocks, allTests, batchId, target, Session.UserID);
        }

        if (!target.HistoRefSet)
        {
            var nextRef = await SampleCopyHelper.DrawRefMatchingAsync(_histologyRefs, source.HistologyRef);
            if (nextRef is not null)
            {
                target.HistologyRef = nextRef;
                target.HistoRefSet = true;
            }
        }

        await _submissions.UpdateAnimalAsync(target, Session.UserID);
    }

    /// <summary>
    /// "Copy sample" for Wet Tissue — duplicates the source sample's tissues onto the new one.
    /// Block-owned tissues on other submission types are copied via the separate Copy Blocks flow.
    /// </summary>
    private async Task<IActionResult> HandleWetTissueCreationAsync(int batchId, int newAnimalId, IReadOnlyList<BatchSubmission> siblingSubmissions, int ownSubmissionId)
    {
        if (SourceAnimalId is > 0 && ownSubmissionId > 0)
        {
            var sourceSubmission = siblingSubmissions.FirstOrDefault(s => s.AnimalID == SourceAnimalId);
            if (sourceSubmission is not null)
            {
                var sourceTissues = await _submissions.GetTissuesBySubmissionAsync(batchId, sourceSubmission.ID);
                foreach (var tissue in sourceTissues)
                    await _submissions.CopyTissueAsync(tissue, ownSubmissionId, Session.UserID);
            }

            // The "Change" flow must return to the batch copy list so the user can still press
            // Finish — otherwise they are stranded on Sample Summary with no final action.
            if (IsCopyBatchReturn)
            {
                PersistCopyBatchState();
                return RedirectToPage("/Batches/CopyBatch", new { sourceBatchId = batchId });
            }

            // Copy sample started from Sample Summary — return there so it's clear the new sample
            // was added, rather than continuing straight into its (empty) detail page.
            return RedirectToPage("/Submissions/SampleSummary", new { batchId });
        }

        Session.SampleDetailReturnPage = BackLinkPage;
        return RedirectToPage("/Submissions/SubmissionDetails", new { batchId, animalId = newAnimalId });
    }

    /// <summary>
    /// Writes the chosen sender ref(s) back into the staged Copy Submission list.
    /// <paramref name="rangeRefs"/> stages one row per mouse number in a range copy, cloning the
    /// edited row so each new sample still copies the same source sample's tissues.
    /// </summary>
    private void PersistCopyBatchState(IReadOnlyList<string>? rangeRefs = null)
    {
        if (TempData is null || !IsCopyBatchReturn)
            return;

        if (TempData["CopyBatch_Animals"] is not string savedJson)
            return;

        var animals = JsonSerializer.Deserialize<List<CopyBatchModel.AnimalRow>>(savedJson) ?? [];
        if (RowIndex is >= 0 && RowIndex < animals.Count)
        {
            if (rangeRefs is { Count: > 0 })
            {
                var edited = animals[RowIndex.Value];
                edited.NewSenderRef = rangeRefs[0];
                for (var i = 1; i < rangeRefs.Count; i++)
                {
                    animals.Insert(RowIndex.Value + i, new CopyBatchModel.AnimalRow
                    {
                        AnimalId = edited.AnimalId,
                        SubmissionId = edited.SubmissionId,
                        SenderRef = edited.SenderRef,
                        NewSenderRef = rangeRefs[i],
                        TissueDetails = edited.TissueDetails,
                    });
                }
            }
            else
            {
                animals[RowIndex.Value].NewSenderRef = string.IsNullOrWhiteSpace(SenderRef)
                    ? string.IsNullOrWhiteSpace(MouseNumberFrom) ? string.Empty : MouseNumberFrom.Trim()
                    : SenderRef.Trim();
            }
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
    /// Validates the mouse-number range and expands it to its individual sender refs.
    /// Returns the error message instead of the refs when the range is invalid or collides with
    /// an existing sample.
    /// </summary>
    private async Task<(List<string> Refs, string? Error)> ResolveMouseRangeRefsAsync()
    {
        const int maxMouseRangeEntries = 1000;

        var from = MouseNumberFrom.Trim().ToUpperInvariant();
        var to = MouseNumberTo.Trim().ToUpperInvariant();

        if (from.Length != 8 || to.Length != 8
            || !ValidationHelpers.ValidateMouseNumber(from) || !ValidationHelpers.ValidateMouseNumber(to)
            || !SenderRefHelpers.TryParseMouseNumber(from, out var fromId) || !SenderRefHelpers.TryParseMouseNumber(to, out var toId))
        {
            return ([], "The mouse number format is MC followed by 6 digits, i.e. MC000105.");
        }

        if (fromId >= toId)
            return ([], "The from number must be less than the to number.");

        var rangeSize = toId - fromId + 1;
        if (rangeSize > maxMouseRangeEntries)
            return ([], $"The mouse number range cannot exceed {maxMouseRangeEntries} entries. Use a smaller range or create a bulk job.");

        // Validate the whole range against EVERY animal in the database (not just this batch) —
        // SenderRef collisions from a different batch (e.g. leftover test/fixture data) still
        // cause the tissue-copy step inside CreateMouseRangeAsync to fail deep in a DB transaction.
        var candidates = Enumerable.Range(fromId, rangeSize).Select(SenderRefHelpers.FormatMouseNumber).ToList();
        var duplicate = (await _submissions.GetExistingSenderRefsAsync(candidates)).FirstOrDefault();

        return duplicate is not null
            ? ([], $"Mouse number {duplicate} already exists. Alter the range and try again.")
            : (candidates, null);
    }

    /// <summary>
    /// Legacy "Alternatively you can assign mouse ranges..." — creates one Animal per MC number in
    /// [<see cref="MouseNumberFrom"/>, <see cref="MouseNumberTo"/>], each with its own BatchSubmission
    /// row, then returns to Sample Summary rather than a single sample's detail page.
    /// </summary>
    private async Task<IActionResult> OnPostMouseRangeAsync(int batchId, int submissionId, Batch? batch)
    {
        var (rangeRefs, rangeError) = await ResolveMouseRangeRefsAsync();
        if (rangeError is not null)
        {
            ModelError = rangeError;
            MouseRangeHasError = true;
            return Page();
        }

        var from = MouseNumberFrom.Trim().ToUpperInvariant();
        var to = MouseNumberTo.Trim().ToUpperInvariant();

        var created = await _submissions.CreateMouseRangeAsync(batchId, SourceAnimalId, from, to, Session.UserID);
        if (!created)
        {
            ModelError = "Could not add the sample range. Please try again.";
            MouseRangeHasError = true;
            return Page();
        }

        // CreateMouseRangeAsync only copies submission tissues, so the blocks and histology refs
        // the single-sample copy performs have to be applied to each new sample here too.
        try
        {
            await CopyToRangeSamplesAsync(batchId, rangeRefs);
        }
        catch (InvalidOperationException ex)
        {
            // The range's samples exist by now, so report the incomplete copy rather than succeed.
            ModelError = ex.Message;
            MouseRangeHasError = true;
            return Page();
        }

        return RedirectToPage("/Submissions/SampleSummary", new { batchId });
    }

    /// <summary>Gives every sample just created by a range copy the same blocks and histology ref a single-sample copy would get.</summary>
    private async Task CopyToRangeSamplesAsync(int batchId, IReadOnlyList<string> rangeRefs)
    {
        if (SourceAnimalId is not > 0 || rangeRefs.Count == 0) return;

        var animals = await _submissions.GetAnimalsByBatchAsync(batchId);
        var source = animals.FirstOrDefault(a => a.ID == SourceAnimalId);
        if (source is null) return;

        var allBlocks = await _blocks.GetByBatchAsync(batchId);
        var wanted = rangeRefs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var target in animals.Where(a => wanted.Contains(a.SenderRef)))
            await CopySourceSampleToTargetAsync(batchId, allBlocks, source, target);
    }
}
