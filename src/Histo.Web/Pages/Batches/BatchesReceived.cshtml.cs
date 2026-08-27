using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists received batches — replaces <c>BatchesReceived.aspx</c>.
/// </summary>
public class BatchesReceivedModel : HistoPageModel
{
    private readonly IBatchService _batches;

    public BatchesReceivedModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    /// <summary>Quick-Go: direct navigation by submission number.</summary>
    [BindProperty]
    public int? QuickGoId { get; set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches received";
        ViewData["PageTitle"] = "Batches received";
        Batches = await _batches.GetReceivedAsync();
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/Batches/BatchDetails");
    }

    public IActionResult OnPostGoAsync()
    {
        if (QuickGoId.HasValue)
            Session.BatchID = QuickGoId.Value;
        return RedirectToPage("/Batches/BatchDetails");
    }
}
