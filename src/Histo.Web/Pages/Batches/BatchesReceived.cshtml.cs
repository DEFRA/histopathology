using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists received batches — replaces <c>BatchesReceived.aspx</c>.
/// Legacy source: <c>BatchesReceived.aspx.vb::InitialiseBatchesGrid</c> (SP <c>GetBatchesToBeBlocked</c>,
/// default sort <c>"ID DESC"</c> when no column has been clicked) and <c>btnGo_Click</c>/
/// <c>grdBatches_SelectedIndexChanged</c> (SP <c>GetBatchWithStatus</c>, both redirect to
/// <c>BatchBlocks.aspx</c> for a Received or InProgress submission).
/// </summary>
public class BatchesReceivedModel : BatchListPageModelBase
{
    private const string BatchesReceivedTitle = "Batches received";

    public BatchesReceivedModel(ISessionService session, IBatchService batches)
        : base(session, batches) { }

    protected override string ListTitle => BatchesReceivedTitle;

    protected override Task<IReadOnlyList<BatchListResult>> FetchBatchesAsync() => BatchService.GetReceivedAsync();

    public IReadOnlyList<BatchListResult> GetPagedEntries() =>
        TakePage(SortColumn switch
        {
            "ID"                  => OrderBatches(b => b.ID),
            "ProjectDescription"  => OrderBatches(b => b.ProjectDescription),
            "ContactDescription"  => OrderBatches(b => b.ContactDescription),
            "Species"             => OrderBatches(b => b.Species),
            "BatchDate"           => OrderBatches(b => b.BatchDate),
            "AllTissuesAssigned"  => OrderBatches(b => b.AllTissuesAssigned),
            // Legacy default (no column clicked yet): dtBatchesView.Sort = "ID DESC".
            _                     => Batches.OrderByDescending(b => b.ID),
        }, PageNumber);

    public async Task OnGetAsync() => await LoadListAsync();

    /// <summary>Row select — legacy source: <c>grdBatches_SelectedIndexChanged</c>, redirects to <c>BatchBlocks.aspx</c>.</summary>
    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        Session.IsViewSubmissionMode = false;
        Session.ReturnPage = "/Batches/BatchesReceived";
        return RedirectToPage("/Batches/BatchBlocks");
    }

    /// <summary>
    /// Quick-Go — legacy source: <c>btnGo_Click</c>, which checks the entered submission number
    /// exists with status InProgress OR Received (<c>GetBatchWithStatus</c> SP) before redirecting
    /// to <c>BatchBlocks.aspx</c>; shows an inline error otherwise.
    /// </summary>
    public async Task<IActionResult> OnPostGoAsync()
    {
        await LoadListAsync();

        if (IsQuickGoIdMissing()) return Page();

        var batch = await BatchService.GetByIdAsync(QuickGoId!.Value);
        // Legacy CheckBatchExists(..., Cassetted = 0): block-type (cassetted) submissions never go through Assign blocks.
        if (batch is null || batch.IsCassetted || batch.Status is not (BatchStatus.Received or BatchStatus.InProgress))
        {
            GoError = $"Submission {QuickGoId.Value} could not be found or does not have the required status.";
            return Page();
        }

        Session.BatchID = QuickGoId.Value;
        Session.IsViewSubmissionMode = false;
        Session.ReturnPage = "/Batches/BatchesReceived";
        return RedirectToPage("/Batches/BatchBlocks");
    }
}

