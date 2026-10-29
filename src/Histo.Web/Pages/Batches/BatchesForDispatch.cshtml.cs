using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists batches awaiting Quality Data entry — replaces <c>BatchesForDispatch.aspx</c>.
/// Legacy source: <c>clsBatch.GetBatchesForDispatch</c> → <c>GetBatchesForDispatch</c> SP.
/// </summary>
public class BatchesForDispatchModel : BatchListPageModelBase
{
    private const string BatchesForDispatchTitle = "Submissions available for quality data entry";

    public BatchesForDispatchModel(ISessionService session, IBatchService batches)
        : base(session, batches) { }

    protected override string ListTitle => BatchesForDispatchTitle;

    protected override Task<IReadOnlyList<BatchListResult>> FetchBatchesAsync() => BatchService.GetForDispatchAsync();

    public IReadOnlyList<BatchListResult> GetPagedEntries() =>
        TakePage(SortColumn switch
        {
            "ProjectDescription" => OrderBatches(b => b.ProjectDescription),
            "ContactDescription" => OrderBatches(b => b.ContactDescription),
            "Species"            => OrderBatches(b => b.Species),
            "BatchDate"          => OrderBatches(b => b.BatchDate),
            "ID"                 => OrderBatches(b => b.ID),
            // No column clicked yet — legacy default: dvBatchesView.Sort = "ID DESC".
            _                    => Batches.OrderByDescending(b => b.ID),
        }, PageNumber);

    public async Task OnGetAsync() => await LoadListAsync();

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        Session.IsViewSubmissionMode = false;
        Session.ReturnPage = "/Batches/BatchesForDispatch";
        return RedirectToPage("/QC/QualityData");
    }

    public async Task<IActionResult> OnPostGoAsync()
    {
        await LoadListAsync();

        if (IsQuickGoIdMissing()) return Page();

        // Use the dispatch list itself as the validation source — if the SP returned it, it's ready.
        var batchInList = Batches.FirstOrDefault(b => b.ID == QuickGoId.Value);
        if (batchInList is null)
        {
            GoError = $"Submission {QuickGoId.Value} could not be found or is not ready for quality data entry.";
            return Page();
        }

        Session.BatchID = QuickGoId.Value;
        Session.ReturnPage = "/Batches/BatchesForDispatch";
        return RedirectToPage("/QC/QualityData");
    }
}
