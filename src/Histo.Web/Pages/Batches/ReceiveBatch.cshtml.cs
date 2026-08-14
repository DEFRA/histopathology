using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using static Histo.Core.Domain.BatchStatus;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>Replaces <c>ReceiveBatch.aspx</c> — marks a batch as Received.</summary>
public class ReceiveBatchModel : HistoPageModel
{
    private readonly IBatchService _batches;

    public ReceiveBatchModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public Batch? Batch { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Receive Batch";
        if (Session.BatchID <= 0) return RedirectToPage("/Batches/BatchesNotReceived");
        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch?.RowStamp is null)
        {
            Error = "Batch not found.";
            return Page();
        }

        var ok = await _batches.UpdateStatusAsync(
            Session.BatchID ?? 0, Received, Batch.RowStamp, Session.UserID);

        if (!ok)
        {
            Error = "Could not update batch status. It may have been modified by another user.";
            return Page();
        }

        return RedirectToPage("/Batches/BatchesReceived");
    }
}
