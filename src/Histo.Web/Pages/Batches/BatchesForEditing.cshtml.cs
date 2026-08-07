using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists all batches available for editing — replaces <c>BatchesForEditing.aspx</c>.
/// Legacy source: <c>clsBatch.GetBatchesWithStatus(0)</c> where status 0 returns all batches.
/// </summary>
public class BatchesForEditingModel : HistoPageModel
{
    private readonly BatchService _batches;

    public BatchesForEditingModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

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
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/EditBatch");
    }

    public async Task<IActionResult> OnPostGoAsync()
    {
        ViewData["Title"] = "Submissions available for editing";
        ViewData["PageTitle"] = "Submissions available for editing";
        Batches = await _batches.GetAllBatchesAsync();

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
