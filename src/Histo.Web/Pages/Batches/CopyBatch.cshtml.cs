using Histo.Administration.Interfaces;
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

        var userId = Session.UserID;
        var batchToCopy = new Batch
        {
            Status              = SourceBatch.Status,
            Comments            = SourceBatch.Comments,
            SubmittedByUserID   = SourceBatch.SubmittedByUserID,
            UserAreaCode        = SourceBatch.UserAreaCode,
            IsPreCassetted      = SourceBatch.IsPreCassetted,
            BatchType           = SourceBatch.BatchType,
            ProjectContractCode = SourceBatch.ProjectContractCode,
            ContactName         = SourceBatch.ContactName,
            Species             = SourceBatch.Species,
            BatchDate           = SourceBatch.BatchDate,
            Fixation            = SourceBatch.Fixation,
            SafeToHandle        = SourceBatch.SafeToHandle,
            OtherSubmittedBy    = SourceBatch.OtherSubmittedBy,
            OtherSubmittedArea  = SourceBatch.OtherSubmittedArea ?? "",
        };
        var newBatchId = await _batches.CopyBatchHeaderAsync(batchToCopy, userId);
        if (newBatchId <= 0)
        {
            Error = "Failed to create the new submission.";
            return Page();
        }

        var submissions = await _submissions.GetSubmissionsByBatchAsync(SourceBatchId);
        var blockAnimals = await _submissions.GetBlockAnimalsByBatchAsync(SourceBatchId);
        var animals = blockAnimals.Count > 0 ? blockAnimals : await _submissions.GetAnimalsByBatchAsync(SourceBatchId);
        var newSenderRefs = Animals.ToDictionary(a => a.AnimalId, a => a.NewSenderRef);
        // When BatchSubmissionID is absent on the animal, assign to the first submission.
        var firstSubmId = submissions.Count > 0 ? submissions[0].ID : 0;

        foreach (var submission in submissions)
        {
            var newSubmissionId = await _submissions.CopySubmissionAsync(submission, newBatchId, userId);
            if (newSubmissionId <= 0) continue;

            var tissues = await _submissions.GetTissuesBySubmissionAsync(SourceBatchId, submission.ID);
            foreach (var tissue in tissues)
                await _submissions.CopyTissueAsync(tissue, newSubmissionId, userId);

            foreach (var animal in animals.Where(a =>
                a.BatchSubmissionID == submission.ID ||
                (a.BatchSubmissionID == 0 && submission.ID == firstSubmId)))
            {
                var newSenderRef = newSenderRefs.GetValueOrDefault(animal.ID, animal.SenderRef);
                if (string.IsNullOrWhiteSpace(newSenderRef)) newSenderRef = animal.SenderRef;
                await _submissions.CopyAnimalAsync(animal, newSubmissionId, newSenderRef, userId);
            }
        }

        return RedirectToPage("/Batches/CopyBatchSummary", new { newBatchId });
    }

    /// <summary>
    /// Saves current Animals to TempData and navigates to SearchSender in picker mode.
    /// Called when the user clicks Change on a row.
    /// </summary>
    public IActionResult OnPostPick(int rowIndex)
    {
        TempData["CopyBatch_Animals"]     = JsonSerializer.Serialize(Animals);
        TempData["CopyBatch_IsCassetted"] = IsCassetted.ToString();
        TempData["CopyBatch_RowIndex"]    = rowIndex.ToString();
        return RedirectToPage("/Search/SearchSender", new
        {
            returnPage = "/Batches/CopyBatch",
            returnId   = SourceBatchId,
            rowIndex,
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
}
