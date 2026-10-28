using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists batches not yet received — replaces <c>BatchesNotReceived.aspx</c>.
/// Histopathology User and Maintenance groups only.
/// </summary>
public class BatchesNotReceivedModel : BatchListPageModelBase
{
    private const string BatchesNotReceivedTitle = "Batches not received";

    public BatchesNotReceivedModel(ISessionService session, IBatchService batches)
        : base(session, batches)
    {
        // Legacy default view was newest submission first when no sort had been chosen.
        SortDesc = true;
    }

    protected override string ListTitle => BatchesNotReceivedTitle;

    protected override Task<IReadOnlyList<BatchListResult>> FetchBatchesAsync() => BatchService.GetNotReceivedAsync();

    public IReadOnlyList<BatchListResult> GetPagedEntries() =>
        TakePage(SortColumn switch
        {
            "ProjectDescription" => OrderBatches(b => b.ProjectDescription),
            "ContactDescription" => OrderBatches(b => b.ContactDescription),
            "Species"            => OrderBatches(b => b.Species),
            "SubmissionDate"     => OrderBatches(b => b.SubmissionDate),
            "OtherSubmittedBy"   => OrderBatches(b => b.OtherSubmittedBy),
            _                    => OrderBatches(b => b.ID),
        }, PageNumber);

    public async Task OnGetAsync() => await LoadListAsync();

    public async Task<IActionResult> OnPostReceiveAsync(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/ReceiveBatch");
    }

    public async Task<IActionResult> OnPostGoAsync()
    {
        await LoadListAsync();

        if (IsQuickGoIdMissing()) return Page();

        var batch = await BatchService.GetByIdAsync(QuickGoId!.Value);
        if (batch is null || batch.Status != BatchStatus.Submitted)
        {
            GoError = $"Submission {QuickGoId.Value} could not be found or is not awaiting receipt.";
            return Page();
        }

        Session.BatchID = QuickGoId.Value;
        return RedirectToPage("/Batches/ReceiveBatch");
    }
}
