using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists batches awaiting Quality Data entry — replaces <c>BatchesForDispatch.aspx</c>.
/// Legacy source: <c>clsBatch.GetBatchesForDispatch</c> → <c>GetBatchesForDispatch</c> SP.
/// </summary>
public class BatchesForDispatchModel : HistoPageModel
{
    private readonly BatchService _batches;

    public BatchesForDispatchModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    /// <summary>Quick-Go: direct navigation by submission number.</summary>
    [BindProperty]
    public int? QuickGoId { get; set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches for dispatch";
        ViewData["PageTitle"] = "Batches for dispatch";
        Batches = await _batches.GetForDispatchAsync();
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/QC/QualityData");
    }

    public IActionResult OnPostGoAsync()
    {
        if (QuickGoId.HasValue)
            Session.BatchID = QuickGoId.Value;
        return RedirectToPage("/QC/QualityData");
    }
}
