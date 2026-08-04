using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists in-progress batches available for editing — replaces <c>BatchesForEditing.aspx</c>.
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

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches for editing";
        ViewData["PageTitle"] = "Batches for editing";
        Batches = await _batches.GetInProgressAsync();
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/EditBatch");
    }

    public IActionResult OnPostGoAsync()
    {
        if (QuickGoId.HasValue)
            Session.BatchID = QuickGoId.Value;
        return RedirectToPage("/Batches/EditBatch");
    }
}
