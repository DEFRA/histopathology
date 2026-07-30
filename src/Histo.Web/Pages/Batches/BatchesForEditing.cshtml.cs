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

    public IReadOnlyList<Batch> Batches { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches for Editing";
        ViewData["PageTitle"] = "Batches for Editing";
        Batches = await _batches.GetInProgressAsync();
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/EditBatch");
    }
}
