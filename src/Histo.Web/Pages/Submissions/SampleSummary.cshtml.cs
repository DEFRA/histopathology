using Histo.Administration.Interfaces;
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
public class SampleSummaryModel : HistoPageModel
{
    private readonly ISubmissionService _submissions;
    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;

    public SampleSummaryModel(ISessionService session, ISubmissionService submissions, IBatchService batches, ILookupService lookups)
        : base(session)
    {
        _submissions = submissions;
        _batches = batches;
        _lookups = lookups;
    }

    public IReadOnlyList<Animal> Animals { get; private set; } = [];

    public Batch? Batch { get; private set; }

    /// <summary>
    /// Batch ID from the URL (route/query). Falls back to <see cref="ISessionService.BatchID"/> for
    /// links not yet migrated to pass it explicitly — Phase 1 of the route-based-state rollout.
    /// </summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    /// <summary>Animal awaiting delete confirmation — drives the inline GOV.UK confirmation panel (replaces browser confirm()).</summary>
    [BindProperty(SupportsGet = true)] public int? ConfirmDeleteAnimalId { get; set; }

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

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Sample summary";
        ViewData["PageTitle"] = "Sample summary";
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage("/Index");

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
        var animals = blockAnimals.Count > 0
            ? MergeAnimals(blockAnimals, allAnimals)
            : allAnimals;

        // Default sort: SenderRef ASC then HistologyRef ASC, matching legacy ByPassSort=false behaviour.
        // When ByPassSort=true the user has explicitly requested block-insertion order — preserve SP order.
        Animals = Batch?.ByPassSort == true
            ? animals
            : [.. animals.OrderBy(a => a.SenderRef).ThenBy(a => a.HistologyRef)];

        // Load submissions upfront — needed for tissue resolution fallback (mirrors CopyBatch which uses
        // firstSubmId when BatchSubmissionID is 0 or the column wasn't returned by the block-animal SP).
        var submissions  = await _submissions.GetSubmissionsByBatchAsync(batchIdValue);
        var firstSubmId  = submissions.Count > 0 ? submissions[0].ID : 0;
        var submissionIds = submissions.Select(s => s.ID).ToHashSet();

        // Load tissue details for the Tissue Details column, mirroring CopyBatch tissue resolution.
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
        var allTissueStrings = tissuesBySubmId.TryGetValue(0, out var zeroGroup) ? zeroGroup : (IReadOnlyList<string>)[];

        // Exact same fallback chain as CopyBatch.ResolveTissues: BatchSubmissionID → firstSubmId → key-0.
        IReadOnlyList<string> ResolveTissues(int batchSubmissionId)
        {
            var submId = batchSubmissionId > 0 && submissionIds.Contains(batchSubmissionId)
                ? batchSubmissionId : firstSubmId;
            if (submId > 0 && tissuesBySubmId.TryGetValue(submId, out var td)) return td;
            return allTissueStrings.Count > 0 ? allTissueStrings : [];
        }

        TissuesByAnimalId = Animals.ToDictionary(a => a.ID, a => ResolveTissues(a.BatchSubmissionID));

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

        return Page();
    }

    public async Task<IActionResult> OnPostSelect(int animalId)
    {
        Session.AnimalID = animalId;
        // Wet Tissue submissions route to SubmissionDetails (tissue list + PM date/histology ref edit);
        // all block-type submissions route to SubmissionDetailsBlock (block assignment view).
        // Legacy source: BatchSummary.aspx btnEditSubmission → SubmissionDetails.aspx;
        //                BatchBlockSummary.aspx btnEditSubmission → SubmissionDetailsBlock.aspx.
        var batchId = BatchId ?? Session.BatchID;
        if (batchId is null or <= 0) return RedirectToPage("/Index");

        // Re-resolve submission type server-side on POST.
        // Do not trust the hidden field from the view for routing decisions.
        var submittedAsCode = await _batches.GetSubmittedAsCodeAsync(batchId.Value);
        var isWetTissue = ValidationHelpers.IsWetTissueDescription(await ResolveSubmittedAsDescriptionAsync(submittedAsCode));

        if (isWetTissue)
            return RedirectToPage("/Submissions/SubmissionDetails", new { batchId, animalId });
        return RedirectToPage("/Submissions/SubmissionDetailsBlock", new { batchId, animalId });
    }

    /// <summary>Resolves a raw "Submitted As" code to its LOOKUP_SUBMITTEDAS (table 11) description.</summary>
    private async Task<string?> ResolveSubmittedAsDescriptionAsync(string? submittedAsCode)
    {
        if (string.IsNullOrEmpty(submittedAsCode)) return null;
        var items = await _lookups.GetLookupDataAsync(11); // LOOKUP_SUBMITTEDAS
        return items.FirstOrDefault(i => i.Code == submittedAsCode)?.Name;
    }

    /// <summary>
    /// Replaces the legacy "Delete submission" action (<c>BatchSummary.aspx.vb</c>::<c>btnDeleteSubmission_Click</c>
    /// / <c>BatchBlockSummary.aspx.vb</c>::<c>btnDeleteSubmission_Click</c>), both of which removed the animal
    /// record from the in-progress batch.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(int animalId)
    {
        await _submissions.DeleteAnimalAsync(animalId, Session.UserID);
        return RedirectToPage(new { batchId = BatchId ?? Session.BatchID });
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
        var seenIds = primary.Select(a => a.ID).ToHashSet();
        var missing = supplementary.Where(a => !seenIds.Contains(a.ID));
        return [.. primary, .. missing];
    }
}
