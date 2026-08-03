using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists completed batches for dispatch — replaces <c>BatchesForDispatch.aspx</c>.
/// </summary>
public class BatchesForDispatchModel : HistoPageModel
{
    private readonly BatchService _batches;

    public BatchesForDispatchModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<Batch> Batches { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches for Dispatch";
        ViewData["PageTitle"] = "Batches for Dispatch";
        // Completed batches awaiting dispatch
        Batches = await _batches.GetReceivedAsync();
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/QC/QualityData");
    }
}
