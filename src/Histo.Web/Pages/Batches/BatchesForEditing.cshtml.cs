using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists all batches available for editing — replaces <c>BatchesForEditing.aspx</c>.
/// Legacy source: <c>clsBatch.GetBatchesWithStatus(0)</c> where status 0 returns all batches.
/// </summary>
public class BatchesForEditingModel : GridPageModel
{
    private readonly IBatchService _batches;

    public BatchesForEditingModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    public int TotalCount => Batches.Count;

    public IReadOnlyList<BatchListResult> PagedEntries =>
        (SortColumn switch
        {
            "ProjectDescription" => SortDesc ? Batches.OrderByDescending(b => b.ProjectDescription) : Batches.OrderBy(b => b.ProjectDescription),
            "ContactDescription" => SortDesc ? Batches.OrderByDescending(b => b.ContactDescription) : Batches.OrderBy(b => b.ContactDescription),
            "Species"            => SortDesc ? Batches.OrderByDescending(b => b.Species)            : Batches.OrderBy(b => b.Species),
            "BatchDate"          => SortDesc ? Batches.OrderByDescending(b => b.BatchDate)           : Batches.OrderBy(b => b.BatchDate),
            "Status"             => SortDesc ? Batches.OrderByDescending(b => b.Status)              : Batches.OrderBy(b => b.Status),
            _                    => SortDesc ? Batches.OrderByDescending(b => b.ID)                  : Batches.OrderBy(b => b.ID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    /// <summary>Quick-Go: direct navigation by submission number.</summary>
    [BindProperty]
    public int? QuickGoId { get; set; }

    /// <summary>Inline error message for Quick-Go validation failures.</summary>
    public string? GoError { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Submissions available for editing";
        ViewData["PageTitle"] = "Submissions available for editing";
        // ISS-044: use GetAllBatchesAsync (maps to GetAllBatches SP) — legacy showed all statuses
        Batches = await _batches.GetAllBatchesAsync();
        PopulateGridViewData(TotalCount);
    }

    public IActionResult OnPostSelect(int batchId)
    {
        // Matches legacy grdBatchesForEditing_SelectedIndexChanged, which always redirects to EditBatch.aspx.
        Session.BatchID    = batchId;
        Session.ReturnPage = "/Batches/BatchesForEditing";
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/Batches/EditBatch");
    }

    public async Task<IActionResult> OnPostGoAsync()
    {
        ViewData["Title"] = "Submissions available for editing";
        ViewData["PageTitle"] = "Submissions available for editing";
        Batches = await _batches.GetAllBatchesAsync();
        PopulateGridViewData(TotalCount);

        if (!QuickGoId.HasValue || QuickGoId.Value <= 0)
        {
            GoError = "Enter a submission number.";
            return Page();
        }

        var batch = await _batches.GetByIdAsync(QuickGoId.Value);
        if (batch is null)
        {
            GoError = $"Submission {QuickGoId.Value} could not be found.";
            return Page();
        }

        Session.BatchID = QuickGoId.Value;
        return RedirectToPage("/Batches/EditBatch");
    }
}
