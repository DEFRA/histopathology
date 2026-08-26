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

        // Mirrors legacy ReceiveBatch.aspx → UpdateBatchDetails → EditBatch SP.
        // Uses the full UpdateAsync path so RowStamp concurrency (WHERE ID=@ID AND RowStamp=@RowStamp)
        // is checked exactly as legacy; EditBatchStatus has no rowstamp check.
        var updated = new Batch
        {
            ID = Batch.ID, Status = Received,
            ReceivedDate      = DateTime.Now,
            ReceivedBy        = Session.UserID,
            Comments          = Batch.Comments,
            StatusComments    = Batch.StatusComments,
            BatchDate         = Batch.BatchDate,
            CompletedDate     = Batch.CompletedDate,
            SubmittedByUserID = Batch.SubmittedByUserID,
            UserAreaCode      = Batch.UserAreaCode,
            IsPreCassetted    = Batch.IsPreCassetted,
            ByPassSort        = Batch.ByPassSort,
            RowStamp          = Batch.RowStamp,
            BatchType         = Batch.BatchType,
            ProjectContractCode = Batch.ProjectContractCode,
            ContactName       = Batch.ContactName,
            Species           = Batch.Species,
            Fixation          = Batch.Fixation,
            CustomerReceivedDate = Batch.CustomerReceivedDate,
            SubmittedBy       = Batch.SubmittedBy,
            SubmittedArea     = Batch.SubmittedArea,
            OtherSubmittedBy  = Batch.OtherSubmittedBy,
            OtherSubmittedArea = Batch.OtherSubmittedArea ?? "",
            SafeToHandle      = Batch.SafeToHandle,
            IsBlocked         = Batch.IsBlocked,
            SampleSameProjects = Batch.SampleSameProjects,
            AllTissuesAssigned = Batch.AllTissuesAssigned,
            TimeReceived      = Batch.TimeReceived,
            PostFixationOther = Batch.PostFixationOther,
        };

        try
        {
            await _batches.UpdateAsync(updated, Session.UserID);
        }
        catch (BatchConcurrencyException)
        {
            Error = "Could not update batch status. It may have been modified by another user.";
            return Page();
        }
        catch (Exception)
        {
            Error = "Failed to update batch status. Please try again.";
            return Page();
        }

        return RedirectToPage("/Batches/BatchesReceived");
    }
}
