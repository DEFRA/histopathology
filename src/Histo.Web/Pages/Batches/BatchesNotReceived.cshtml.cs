using Histo.Core.Domain;
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

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    /// <summary>Quick-Go: direct navigation by submission number.</summary>
    [BindProperty]
    public int? QuickGoId { get; set; }

    /// <summary>Inline error message for Quick-Go validation failures.</summary>
    public string? GoError { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches not received";
        ViewData["PageTitle"] = "Batches not received";
        Batches = await _batches.GetNotReceivedAsync();
    }

    public async Task<IActionResult> OnPostReceiveAsync(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/ReceiveBatch");
    }

    public async Task<IActionResult> OnPostGoAsync()
    {
        ViewData["Title"] = "Batches not received";
        ViewData["PageTitle"] = "Batches not received";
        Batches = await _batches.GetNotReceivedAsync();

        if (!QuickGoId.HasValue || QuickGoId.Value <= 0)
        {
            GoError = "Enter a submission number.";
            return Page();
        }

        var batch = await _batches.GetByIdAsync(QuickGoId.Value);
        if (batch is null || batch.Status != BatchStatus.Submitted)
        {
            GoError = $"Submission {QuickGoId.Value} could not be found or is not awaiting receipt.";
            return Page();
        }

        Session.BatchID = QuickGoId.Value;
        return RedirectToPage("/Batches/ReceiveBatch");
    }
}
