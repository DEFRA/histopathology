using Histo.Administration.Interfaces;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces <c>CopyBatch.aspx</c> / <c>CopyBatchBlocks.aspx</c> — duplicates an
/// existing submission as the starting point for a new submission.
///
/// Scenario 1 (<c>CopyBatchBlocks.aspx</c>): cassetted batch — shows SenderRef + New Sender Ref.
/// Scenario 2 (<c>CopyBatch.aspx</c>): non-cassetted batch — shows SenderRef + expandable
/// Tissue Details + New Sender Ref.
///
/// Legacy branching: <c>ViewSubmissions.aspx.vb</c> redirects to <c>CopyBatch.aspx</c> when
/// <c>SV_Cassetted = False</c>, otherwise to <c>CopyBatchBlocks.aspx</c>.
/// </summary>
public class CopyBatchModel : HistoPageModel
{
    private readonly IBatchService _batches;
    private readonly ISubmissionService _submissions;
    private readonly ILookupService _lookups;

    public CopyBatchModel(ISessionService session, IBatchService batches, ISubmissionService submissions, ILookupService lookups)
        : base(session)
    {
        _batches = batches;
        _submissions = submissions;
        _lookups = lookups;
    }

    [BindProperty] public int SourceBatchId { get; set; }
    [BindProperty] public List<AnimalRow> Animals { get; set; } = [];

    public IReadOnlyList<CopyBatchDisplayRow> DisplayRows => BuildDisplayRows(Animals);

    public Batch? SourceBatch { get; private set; }
    // Persisted as hidden field so the view branches correctly on POST re-render.
    [BindProperty] public bool IsCassetted { get; set; }
    public string? Error { get; private set; }

    /// <summary>First "Finish" click posts with this false, showing an inline confirmation
    /// panel instead of creating the copy immediately — the Help page documents a confirm
    /// step here, but none previously existed.</summary>
    [BindProperty] public bool Confirm { get; set; }
    public bool ShowConfirmPanel { get; private set; }

    public async Task<IActionResult> OnGetAsync(int sourceBatchId)
    {
        ViewData["Title"] = "Copy Submission";
        ViewData["PageTitle"] = "Copy Submission";

        SourceBatchId = sourceBatchId;

        // ── Picker return: Animals were serialised to TempData before navigating to SearchSender. ──
        if (TempData["CopyBatch_Animals"] is string savedJson)
        {
            Animals     = JsonSerializer.Deserialize<List<AnimalRow>>(savedJson) ?? [];
            IsCassetted = bool.TryParse(TempData["CopyBatch_IsCassetted"] as string, out var ic) && ic;
            if (TempData["SenderRefPicker_Selected"] is string chosen &&
                int.TryParse(TempData["CopyBatch_RowIndex"] as string, out var ri) &&
                ri >= 0 && ri < Animals.Count)
            {
                Animals[ri].NewSenderRef = chosen;
            }
            SourceBatch = await _batches.GetByIdAsync(sourceBatchId);
            return Page();
        }
        SourceBatch = await _batches.GetByIdAsync(sourceBatchId);
        if (SourceBatch is null)
        {
            Error = "The submission to copy could not be found.";
            return Page();
        }

        var submissions = await _submissions.GetSubmissionsByBatchAsync(sourceBatchId);
        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(sourceBatchId);
        IsCassetted = blockAnimals.Count > 0;

        // Load submission-level tissues for ALL scenarios — the column is always shown.
        var tissueTypes = await _lookups.GetLookupDataAsync(9); // 9 = LOOKUP_TISSUE_CODE
        var tissueNames = tissueTypes
            .Where(t => t.Code != null)
            .ToDictionary(t => t.Code!, t => t.Name, StringComparer.OrdinalIgnoreCase);
        var allTissues = await _submissions.GetBatchSubmissionTissuesAsync(sourceBatchId);
        var tissuesBySubmId = allTissues
            .GroupBy(t => t.OwnerID)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => $"{t.NoPieces} x {tissueNames.GetValueOrDefault(t.TissueCode, t.TissueCode)}").ToList());
        // Fallback: if all OwnerID = 0 (column name mismatch), all tissues land under key 0.
        var allTissueStrings = tissuesBySubmId.TryGetValue(0, out var zeroTd) ? zeroTd : [];
        var firstSubmId = submissions.Count > 0 ? submissions[0].ID : 0;
        var submissionIds = submissions.Select(s => s.ID).ToHashSet();

        List<string> ResolveTissues(int batchSubmissionId)
        {
            var submId = batchSubmissionId > 0 && submissionIds.Contains(batchSubmissionId)
                ? batchSubmissionId : firstSubmId;
            if (submId > 0 && tissuesBySubmId.TryGetValue(submId, out var td)) return td;
            // All tissues keyed under 0 when BatchSubmissionID column wasn't mapped.
            return allTissueStrings.Count > 0 ? allTissueStrings : [];
        }

        if (IsCassetted)
        {
            Animals = blockAnimals.OrderBy(a => a.SenderRef).Select(a => new AnimalRow
            {
                AnimalId = a.ID,
                SubmissionId = a.BatchSubmissionID > 0 ? a.BatchSubmissionID : firstSubmId,
                SenderRef = a.SenderRef,
                NewSenderRef = string.Empty,
                TissueDetails = ResolveTissues(a.BatchSubmissionID),
            }).ToList();
        }
        else
        {
            var animals = await _submissions.GetAnimalsByBatchAsync(sourceBatchId);
            Animals = animals.OrderBy(a => a.SenderRef).Select(a => new AnimalRow
            {
                AnimalId = a.ID,
                SubmissionId = a.BatchSubmissionID > 0 && submissionIds.Contains(a.BatchSubmissionID)
                    ? a.BatchSubmissionID : firstSubmId,
                SenderRef = a.SenderRef,
                NewSenderRef = string.Empty,
                TissueDetails = ResolveTissues(a.BatchSubmissionID),
            }).ToList();
        }

        _ = submissions;
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

        // Only rows with an actual replacement sender ref are part of the copied submission.
        // Blank rows represent "not selected" or a cancelled range edit and must never be carried
        // into the new batch.
        var selected = Animals
            .Where(a => !string.IsNullOrWhiteSpace(a.NewSenderRef))
            .ToList();

        // Every row starts blank, so without this the copy would continue with an empty plan and
        // create a batch header with no samples at all.
        if (selected.Count == 0)
        {
            Error = "Select at least one sample to copy. Use Change to give a sample a new sender reference.";
            return Page();
        }

        if (!Confirm)
        {
            ShowConfirmPanel = true;
            return Page();
        }

        // Nothing is written to the database here — the samples are staged and the user is sent
        // to the normal Create Submission form, pre-filled from this source batch, so the new
        // batch (and these samples) are only created once that form is actually submitted.
        var token = Guid.NewGuid().ToString("N");
        TempData["CopyBatch_PendingCopy"] = JsonSerializer.Serialize(new PendingCopy(SourceBatchId, selected, token));

        // BatchDetailsModel's create-mode form reads Session.BatchType (not the source batch
        // directly) to pick the TSE/Non-TSE antibody lookup table and to stamp the new batch's own
        // BatchType — View/Search Submissions' entry points only ever set Session.BatchID, so
        // without this a Non-TSE source could render TSE options and create the copy as TSE.
        Session.BatchType = SourceBatch.BatchType;

        return RedirectToPage("/Batches/BatchDetails", new { mode = "create", copyToken = token });
    }

    /// <summary>
    /// Saves current Animals to TempData and navigates to SearchSender in picker mode.
    /// Called when the user clicks Change on a row.
    /// </summary>
    public IActionResult OnPostPick(int rowIndex)
    {
        if (TempData is not null)
        {
            TempData["CopyBatch_Animals"]     = JsonSerializer.Serialize(Animals);
            TempData["CopyBatch_IsCassetted"] = IsCassetted.ToString();
            TempData["CopyBatch_RowIndex"]    = rowIndex.ToString();
        }

        var sourceAnimalId = rowIndex >= 0 && rowIndex < Animals.Count ? Animals[rowIndex].AnimalId : 0;
        return RedirectToPage("/Submissions/AddSubmission", new
        {
            returnPage = $"/Batches/CopyBatch?sourceBatchId={SourceBatchId}",
            sourceBatchId = SourceBatchId,
            rowIndex,
            sourceAnimalId,
        });
    }

    /// <summary>
    /// Rows the Copy Submission table shows as a single line: a range copy stages one row per new
    /// sender ref against the same source sample.
    /// </summary>
    public static bool IsSameDisplayGroup(AnimalRow a, AnimalRow b) =>
        a.AnimalId == b.AnimalId
        && a.SubmissionId == b.SubmissionId
        && string.Equals(a.SenderRef, b.SenderRef, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<CopyBatchDisplayRow> BuildDisplayRows(IReadOnlyList<AnimalRow> animals)
    {
        var result = new List<CopyBatchDisplayRow>();
        var consumed = new HashSet<int>();

        for (var i = 0; i < animals.Count; i++)
        {
            if (!consumed.Add(i))
                continue;

            var anchor = animals[i];
            var rowIndexes = new List<int> { i };
            var newSenderRefs = new List<string>();

            if (!string.IsNullOrWhiteSpace(anchor.NewSenderRef))
                newSenderRefs.Add(anchor.NewSenderRef);

            for (var j = i + 1; j < animals.Count; j++)
            {
                if (!consumed.Add(j))
                    continue;

                var candidate = animals[j];
                if (IsSameDisplayGroup(candidate, anchor))
                {
                    rowIndexes.Add(j);
                    if (!string.IsNullOrWhiteSpace(candidate.NewSenderRef))
                        newSenderRefs.Add(candidate.NewSenderRef);
                }
                else
                {
                    consumed.Remove(j);
                }
            }

            result.Add(new CopyBatchDisplayRow
            {
                AnimalId = anchor.AnimalId,
                SubmissionId = anchor.SubmissionId,
                SenderRef = anchor.SenderRef,
                TissueDetails = anchor.TissueDetails,
                RowIndexes = rowIndexes,
                NewSenderRefs = newSenderRefs,
            });
        }

        return result;
    }

    /// <summary>One editable row of the source submission's samples.</summary>
    public class AnimalRow
    {
        public int AnimalId { get; set; }
        public int SubmissionId { get; set; }
        public string SenderRef { get; set; } = string.Empty;
        public string NewSenderRef { get; set; } = string.Empty;
        /// <summary>Tissue detail strings for Scenario 2 (non-cassetted). Empty for Scenario 1.</summary>
        public List<string> TissueDetails { get; set; } = [];
    }

    public sealed class CopyBatchDisplayRow
    {
        public int AnimalId { get; init; }
        public int SubmissionId { get; init; }
        public string SenderRef { get; init; } = string.Empty;
        public List<string> TissueDetails { get; init; } = [];
        public List<int> RowIndexes { get; init; } = [];
        public List<string> NewSenderRefs { get; init; } = [];

        public string DisplayNewSenderRef
        {
            get
            {
                var refs = NewSenderRefs
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return refs.Count switch
                {
                    0 => string.Empty,
                    1 => refs[0],
                    _ => $"{refs[0]} - {refs[^1]}",
                };
            }
        }
    }

    /// <summary>
    /// Staged copy request read by <see cref="Histo.Web.Pages.Batches.BatchDetailsModel"/> once
    /// the Create Submission form is submitted. <paramref name="Token"/> must match the
    /// <c>copyToken</c> query/form value on that page for the entry to be treated as current —
    /// see <c>BatchDetailsModel.TryGetValidPendingCopyAsync</c>.
    /// </summary>
    public sealed record PendingCopy(int SourceBatchId, List<AnimalRow> Samples, string Token);
}
