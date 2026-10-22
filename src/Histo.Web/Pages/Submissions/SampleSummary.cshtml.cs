using Histo.Administration.Interfaces;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
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
///
/// Sortable/paginated via <see cref="GridPageModel"/> — legacy's <c>AllowPaging</c> grid, dropped
/// during the original flat-list consolidation (D-2), restored on the Sender ref/Histology ref columns.
/// </summary>
public class SampleSummaryModel : GridPageModel
{
    private readonly ISubmissionService _submissions;
    private readonly IBatchService _batches;
    private readonly IBlockService _blocks;
    private readonly ILookupService _lookups;

    public SampleSummaryModel(ISessionService session, ISubmissionService submissions, IBatchService batches, IBlockService blocks, ILookupService lookups)
        : base(session)
    {
        _submissions = submissions;
        _batches = batches;
        _blocks = blocks;
        _lookups = lookups;
    }

    public IReadOnlyList<Animal> Animals { get; private set; } = [];

    /// <summary>Current page of <see cref="Animals"/>, sorted by the selected column or — when no
    /// column is selected — the existing default/bypass-sort order already applied to <see cref="Animals"/>.</summary>
    public IReadOnlyList<Animal> PagedEntries => GetPagedEntries();

    private const string IndexPage = "/Index";

    private List<Animal> GetPagedEntries()
    {
        IEnumerable<Animal> sorted = Animals;
        if (SortColumn == "HistologyRef")
            sorted = SortDesc ? Animals.OrderByDescending(a => a.HistologyRef) : Animals.OrderBy(a => a.HistologyRef);
        else if (!string.IsNullOrEmpty(SortColumn))
            sorted = SortDesc ? Animals.OrderByDescending(a => a.SenderRef) : Animals.OrderBy(a => a.SenderRef);

        return sorted
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    public Batch? Batch { get; private set; }

    /// <summary>
    /// Batch ID from the URL (route/query). Falls back to <see cref="ISessionService.BatchID"/> for
    /// links not yet migrated to pass it explicitly — Phase 1 of the route-based-state rollout.
    /// </summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>Animal awaiting delete confirmation — drives the inline GOV.UK confirmation panel (replaces browser confirm()).</summary>
    [BindProperty(SupportsGet = true)] public int? ConfirmDeleteAnimalId { get; set; }
    /// <summary>Set when a POST action (e.g. delete sample) failed server-side; survives the redirect via TempData.</summary>
    public string? SaveError { get; private set; }
    /// <summary>Tissue detail strings keyed by AnimalID for the Tissue Details column.</summary>
    public IReadOnlyDictionary<int, IReadOnlyList<string>> TissuesByAnimalId { get; private set; } =
        new Dictionary<int, IReadOnlyList<string>>();

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
    /// <summary>Exposes the session-resolved submission ID so the Copy sample link can pass it explicitly to AddSample.</summary>
    public int? BatchSubmissionId => Session.BatchSubmissionID;

    public bool CanModifySamples => Batch?.Status is BatchStatus.Submitted or BatchStatus.Rejected;

    /// <summary>Add/Copy/Delete sample and Finish are also offered once a submission is Received or In progress.</summary>
    public bool CanAddSamples => CanModifySamples || Batch?.Status is BatchStatus.Received or BatchStatus.InProgress;

    /// <summary>
    /// Wet Tissue routes Edit sample to SubmissionDetails (tissue-only view); all other types
    /// (Wax Block, Pre-Cassetted, Stained/Unstained Section) route to SubmissionDetailsBlock.
    /// Determined by resolving the batch's Submitted As code to its lookup description and
    /// comparing to "Wet Tissue" — see <see cref="IsWetTissueCodeAsync"/>.
    /// Legacy source: <c>BatchSummary.aspx</c> vs <c>BatchBlockSummary.aspx</c> — separate pages per type;
    /// both consolidated here but the Edit navigation target differs by type.
    /// </summary>
    public bool IsWetTissue { get; private set; }

    /// <summary>Resolved LOOKUP_SUBMITTEDAS description for this batch (e.g. "Pre Cassetted Tissue", "Wax Block", "Wet Tissue") — drives the page's type caption.</summary>
    public string? SubmittedAsDescription { get; private set; }

    /// <summary>
    /// Mirrors legacy <c>SV_ViewSubmission</c>: true when reached via the View Submission journey
    /// (ViewSubmissions or SearchSubmissions → BatchDetails → Samples). All sample actions are
    /// read-only in this mode.
    /// </summary>
    public bool IsViewMode => Session.IsViewSubmissionMode;

    /// <summary>Back-link target — whichever of BatchDetails/EditBatch's "Samples" button was
    /// actually used to get here (see <see cref="ISessionService.SampleSummaryReturnPage"/>).</summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(Session.SampleSummaryReturnPage)
        ? "/Batches/BatchDetails"
        : Session.SampleSummaryReturnPage;

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Sample summary";
        ViewData["PageTitle"] = "Sample summary";
        if (TempData["SampleSummary_Error"] is string err) SaveError = err;
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage(IndexPage);

        var forbidden = await CheckBatchAccessAsync(_batches, batchId.Value);
        if (forbidden is not null) return forbidden;

        Session.BatchID = batchId; // keep session in sync as a fallback for links not yet migrated
        BatchId = batchId;
        var batchIdValue = batchId.Value;
        Batch = await _batches.GetByIdAsync(batchIdValue);
        var submittedAsCode = await _batches.GetSubmittedAsCodeAsync(batchIdValue);
        SubmittedAsDescription = await ResolveSubmittedAsDescriptionAsync(submittedAsCode);
        IsWetTissue = ValidationHelpers.IsWetTissueDescription(SubmittedAsDescription);
        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(batchIdValue);
        var allAnimals = await _submissions.GetAnimalsByBatchAsync(batchIdValue);
        // Merge rather than either/or: GetBlockAnimalsByBatchAsync only returns animals that
        // already have at least one block assigned, so a newly added sample (no blocks yet)
        // would silently disappear from the list whenever any OTHER animal in the same batch
        // already had a block. Union by ID instead — keep the richer block-animal rows (correct
        // SenderRef/HistologyRef per legacy CreateSenderHistoRefData) and append anything not
        // yet block-assigned from the plain animal list.
        // Once blocks are assigned (Done) legacy BatchBlockSummary lists only the block animals; the
        // submission animal of a migrated batch is a placeholder whose "sender ref" is a histology ref.
        var animals = blockAnimals.Count == 0 ? allAnimals
            : Batch?.IsBlocked == true ? blockAnimals
            : MergeAnimals(blockAnimals, allAnimals);

        // Default sort: SenderRef ASC then HistologyRef ASC, matching legacy ByPassSort=false behaviour.
        // When ByPassSort=true the user has explicitly requested block-insertion order — preserve SP order.
        Animals = Batch?.ByPassSort == true
            ? animals
            : [.. animals.OrderBy(a => a.SenderRef).ThenBy(a => a.HistologyRef)];

        var submissions  = await _submissions.GetSubmissionsByBatchAsync(batchIdValue);
        var firstSubmId  = submissions.Count > 0 ? submissions[0].ID : 0;

        // Legacy source: clsBatchSummary.CreateBatchSummaryData — "N x Tissue" per tissue under each sample.
        var tissueTypes = await _lookups.GetLookupDataAsync(9); // 9 = LOOKUP_TISSUE_CODE
        var tissueNames = tissueTypes
            .Where(t => t.Code != null)
            .ToDictionary(t => t.Code!, t => t.Name, StringComparer.OrdinalIgnoreCase);
        var allTissues = await _submissions.GetBatchSubmissionTissuesAsync(batchIdValue);
        var tissuesBySubmId = allTissues
            .GroupBy(t => t.OwnerID)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g
                    .Select(t => $"{t.NoPieces} x {tissueNames.GetValueOrDefault(t.TissueCode, t.TissueCode)}")
                    .ToList());

        // A sample's tissues hang off its OWN BatchSubmission row(s). The animal SPs never return
        // BatchSubmissionID (always 0), and every batch starts with an empty "Default" submission
        // (AnimalID 0, Order 1) — falling back to the batch's first submission therefore showed no
        // tissues at all, or another sample's. This page is one row per sample, so a sample linked to
        // several submissions shows all of their tissues together.
        var submissionIdsByAnimalId = submissions
            .Where(s => s.AnimalID > 0)
            .GroupBy(s => s.AnimalID)
            .ToDictionary(g => g.Key, g => g.Select(s => s.ID).ToList());

        TissuesByAnimalId = Animals.ToDictionary(
            a => a.ID,
            a => (IReadOnlyList<string>)(submissionIdsByAnimalId.TryGetValue(a.ID, out var submIds)
                ? submIds.SelectMany(id => tissuesBySubmId.GetValueOrDefault(id) ?? []).ToList()
                : []));

        // Populate session BatchSubmissionID using already-loaded submissions (no second query needed).
        var firstAnimalSubId = Animals.FirstOrDefault(a => a.BatchSubmissionID > 0)?.BatchSubmissionID;
        if (firstAnimalSubId is > 0)
        {
            Session.BatchSubmissionID = firstAnimalSubId.Value;
        }
        else if (firstSubmId > 0)
        {
            Session.BatchSubmissionID = firstSubmId;
        }
        else
        {
            // First visit for this batch: create the default batch submission record.
            var sub = new BatchSubmission { BatchID = batchIdValue, SubmissionName = "Default", Order = 1 };
            var subId = await _submissions.AddSubmissionAsync(sub, Session.UserID);
            if (subId > 0) Session.BatchSubmissionID = subId;
        }

        PopulateGridViewData(Animals.Count);
        return Page();
    }

    public async Task<IActionResult> OnPostSelect(int animalId)
    {
        Session.AnimalID = animalId;
        // Wet Tissue submissions route to SubmissionDetails (tissue list + PM date/histology ref edit);
        // all block-type submissions route to SubmissionDetailsBlock (block assignment view).
        // Legacy source: BatchSummary.aspx btnEditSubmission → SubmissionDetails.aspx;
        //                BatchBlockSummary.aspx btnEditSubmission → SubmissionDetailsBlock.aspx.
        // A Wet Tissue sample that already has blocks (via the "Assign tissue to block" journey)
        // is routed as a block-type submission instead — deliberate consistency rule, not a legacy behaviour.
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage(IndexPage);

        // Back/Save from the detail screen must return here (SampleSummary), the page the user
        // actually clicked the sample from — not further back to wherever SampleSummary itself
        // was reached from. Previously fell through to Session.SampleSummaryReturnPage, which
        // skipped SampleSummary entirely whenever it held a stale value from an earlier,
        // unrelated BatchDetails/EditBatch visit.
        Session.SampleDetailReturnPage = $"/Submissions/SampleSummary?batchId={batchId.Value}";

        // Re-resolve submission type server-side on POST.
        // Do not trust the hidden field from the view for routing decisions.
        var submittedAsCode = await _batches.GetSubmittedAsCodeAsync(batchId.Value);
        var isWetTissue = ValidationHelpers.IsWetTissueDescription(await ResolveSubmittedAsDescriptionAsync(submittedAsCode));

        if (isWetTissue)
        {
            var blocks = await _blocks.GetByBatchAsync(batchId.Value);
            var hasBlocks = blocks.Any(b => b.AnimalID == animalId);
            if (!hasBlocks)
                return RedirectToPage("/Submissions/SubmissionDetails", new { batchId, animalId });
        }

        return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId, animalId });
    }

    /// <summary>Resolves a raw "Submitted As" code to its LOOKUP_SUBMITTEDAS (table 11) description.</summary>
    private async Task<string?> ResolveSubmittedAsDescriptionAsync(string? submittedAsCode)
    {
        if (string.IsNullOrEmpty(submittedAsCode)) return null;
        // includeInactive: true — a batch may have been submitted under a type since deactivated
        // (e.g. code 6 "Fresh Frozen"), which the active-only default would silently omit.
        var items = await _lookups.GetLookupDataAsync(11, includeInactive: true); // LOOKUP_SUBMITTEDAS
        return items.FirstOrDefault(i => i.Code == submittedAsCode)?.Name;
    }

    /// <summary>
    /// Replaces the legacy "Delete submission" action (<c>BatchSummary.aspx.vb</c>::<c>btnDeleteSubmission_Click</c>
    /// / <c>BatchBlockSummary.aspx.vb</c>::<c>btnDeleteSubmission_Click</c>), both of which removed the animal
    /// record from the in-progress batch.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(int animalId)
    {
        // Cascade delete scoped to this submission; DeleteAnimal would remove the shared Animal row and orphan other submissions' blocks.
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage(IndexPage);

        var forbidden = await CheckBatchAccessAsync(_batches, batchId.Value);
        if (forbidden is not null) return forbidden;

        if (!await _submissions.DeleteSampleSubmissionFromBatchAsync(batchId.Value, animalId, Session.UserID))
        {
            TempData["SampleSummary_Error"] = "Could not delete this sample. It may still have blocks or tissues recorded against it.";
            return RedirectToPage(new { batchId });
        }

        await _batches.RefreshAllTissuesAssignedAsync(batchId.Value, Session.UserID);
        return RedirectToPage(new { batchId });
    }

    /// <summary>
    /// Finalises the in-progress submission — the equivalent of legacy's <c>btSubmit_Click</c>
    /// (<c>BatchBlocks.aspx.vb</c> / <c>BatchSummary.aspx.vb</c> / <c>BatchBlockSummary.aspx.vb</c>),
    /// which only proceeded once <c>dtBatch.Rows.Count &gt; 0</c> and then redirected to
    /// <c>FinalPrintBatch.aspx</c>. Until this step, the batch record persisted eagerly by
    /// <c>BatchDetails.OnPostCreateAsync</c> is treated as a draft: this handler is the actual
    /// "submission created" business gate, restoring the legacy rule that a submission must have
    /// at least one sample before it is considered finished.
    ///
    /// <summary>
    /// Legacy source: <c>BatchSummary.aspx.vb::btSubmit_Click</c> and its cassetted counterpart —
    /// neither changes <c>BatchStatus</c> at "Finish". Every submission type stays Submitted
    /// ("Not started") until Histopathology explicitly receives it via <c>ReceiveBatch</c>; the
    /// block-based journey only advances to In progress later, via <c>BatchBlocks.cshtml.cs</c>'s
    /// own "Done" button (<see cref="IBatchService.CompleteBlockAssignmentAsync"/>), once the
    /// user has actually started assigning blocks — not at submission-creation time.
    ///
    /// A previous version of this handler called <c>CompleteBlockAssignmentAsync</c> here for every
    /// non-Wet-Tissue type, which set the batch In progress immediately on Finish — before it had
    /// even been received. That skipped "Received" entirely and is why non-Wet-Tissue submissions
    /// (Pre-Cassetted, Stained/Unstained Section, Wax Block) appeared to jump straight past the
    /// awaiting-receipt list. Removed; status is no longer touched here for any submission type.
    ///
    /// <c>FinalPrintBatch.aspx</c> itself remains unmigrated (blocked on the Phase 2 Reporting
    /// work — see <c>docs/Parity-Audit-Report.md</c>), so on success this mirrors the same
    /// accepted redirect target already used by <c>BatchBlocks.cshtml.cs</c>::<c>OnPostDoneAsync</c>
    /// (<c>/Batches/BatchesNotReceived</c>) rather than a non-functional Print Submission stub.
    /// </summary>
    public async Task<IActionResult> OnPostFinishAsync()
    {
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage(IndexPage);

        // Same merge as OnGetAsync — GetAnimalsByBatchAsync alone misses samples that only exist
        // via a block assignment, which was wrongly reporting "no samples" for such batches.
        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(batchId.Value);
        var allAnimals = await _submissions.GetAnimalsByBatchAsync(batchId.Value);
        var animals = blockAnimals.Count > 0 ? MergeAnimals(blockAnimals, allAnimals) : allAnimals;
        if (animals.Count == 0)
        {
            TempData["SampleSummary_Error"] = "You must add at least one sample before finishing this submission.";
            return RedirectToPage(new { batchId });
        }

        // When the user reached this page from Edit Submission/Submission Details, the logical
        // completion target is the originating submission page rather than the final print screen.
        if (!string.IsNullOrWhiteSpace(Session.SampleSummaryReturnPage))
            return RedirectToPage(Session.SampleSummaryReturnPage);

        // Legacy flow continues to the printable confirmation page after finishing a submission,
        // with the follow-up return target kept as the awaiting-receipt list.
        Session.ReturnPage = "/Batches/BatchesNotReceived";

        return RedirectToPage("/Batches/PrintSubmission");
    }

    /// <summary>
    /// Toggles the ByPassSort flag on the batch and reloads.
    /// Legacy source: <c>BatchBlockSummary.aspx.vb</c>::<c>chkByPassSort_CheckedChanged</c>.
    /// </summary>
    public async Task<IActionResult> OnPostToggleByPassSortAsync()
    {
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage();
        var current = await _batches.GetByIdAsync(batchId.Value);
        if (current is not null)
            await _batches.SetByPassSortAsync(batchId.Value, !current.ByPassSort, Session.UserID);
        return RedirectToPage(new { batchId });
    }

    /// <summary>Unions two animal lists by ID, keeping the first list's entries and appending any not already present.</summary>
    private static IReadOnlyList<Animal> MergeAnimals(IReadOnlyList<Animal> primary, IReadOnlyList<Animal> supplementary)
    {
        var distinctPrimary = primary
            .GroupBy(a => a.ID)
            .Select(g => g.First())
            .ToList();
        var seenIds = distinctPrimary.Select(a => a.ID).ToHashSet();
        var missing = supplementary.Where(a => !seenIds.Contains(a.ID));
        return [.. distinctPrimary, .. missing];
    }
}
