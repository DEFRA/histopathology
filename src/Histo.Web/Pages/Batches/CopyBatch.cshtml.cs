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

        if (!Confirm)
        {
            ShowConfirmPanel = true;
            return Page();
        }

        // Nothing is written to the database here — the samples are staged and the user is sent
        // to the normal Create Submission form, pre-filled from this source batch, so the new
        // batch (and these samples) are only created once that form is actually submitted.
        TempData["CopyBatch_PendingCopy"] = JsonSerializer.Serialize(new PendingCopy(SourceBatchId, Animals));

        return RedirectToPage("/Batches/BatchDetails", new { mode = "create" });
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

    /// <summary>Staged copy request read by <see cref="Histo.Web.Pages.Batches.BatchDetailsModel"/> once the Create Submission form is submitted.</summary>
    public sealed record PendingCopy(int SourceBatchId, List<AnimalRow> Samples);
}
