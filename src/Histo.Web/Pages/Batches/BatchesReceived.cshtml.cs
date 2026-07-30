using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists received batches — replaces <c>BatchesReceived.aspx</c>.
/// </summary>
public class BatchesReceivedModel : HistoPageModel
{
    private readonly BatchService _batches;

    public BatchesReceivedModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<Batch> Batches { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches Received";
        ViewData["PageTitle"] = "Batches Received";
        Batches = await _batches.GetReceivedAsync();
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/BatchDetails");
    }
}
