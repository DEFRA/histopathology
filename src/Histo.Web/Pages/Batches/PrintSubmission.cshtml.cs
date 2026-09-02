using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Confirmation page shown after a submission's receipt is saved on <c>ReceiveBatch</c>.
/// Replaces <c>FinalPrintBatch.aspx</c> — offers the printable submission form and
/// submission notes, then continues back to the awaiting-receipt list.
/// </summary>
public class PrintSubmissionModel : HistoPageModel
{
    private readonly IBatchService _batches;

    public PrintSubmissionModel(ISessionService session, IBatchService batches) : base(session)
    {
        _batches = batches;
    }

    public Batch? Batch { get; private set; }

    /// <summary>Mirrors legacy <c>EnableSubmissionNotes</c> — only offered when notes exist.</summary>
    public bool HasNotes { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Print submission";
        ViewData["PageTitle"] = "Print submission";

        if (Session.BatchID is null or <= 0) return RedirectToPage("/Batches/BatchesNotReceived");

        Batch = await _batches.GetByIdAsync(Session.BatchID.Value);
        if (Batch is null) return RedirectToPage("/Batches/BatchesNotReceived");

        HasNotes = !string.IsNullOrWhiteSpace(Batch.Comments) || !string.IsNullOrWhiteSpace(Batch.StatusComments);

        return Page();
    }
}
