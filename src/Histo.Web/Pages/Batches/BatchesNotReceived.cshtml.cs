using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists batches not yet received — replaces <c>BatchesNotReceived.aspx</c>.
/// Histopathology User and Maintenance groups only.
/// </summary>
public class BatchesNotReceivedModel : HistoPageModel
{
    private readonly BatchService _batches;

    public BatchesNotReceivedModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<Batch> Batches { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches Not Received";
        ViewData["PageTitle"] = "Batches Not Received";
        Batches = await _batches.GetNotReceivedAsync();
    }

    public async Task<IActionResult> OnPostReceiveAsync(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/ReceiveBatch");
    }
}
