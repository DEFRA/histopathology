using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists all batches available for editing — replaces <c>BatchesForEditing.aspx</c>.
/// Legacy source: <c>clsBatch.GetBatchesWithStatus(0)</c> where status 0 returns all batches.
/// </summary>
public class BatchesForEditingModel : BatchListPageModelBase
{
    private const string BatchesForEditingTitle = "Submissions available for editing";

    public BatchesForEditingModel(ISessionService session, IBatchService batches)
        : base(session, batches) { }

    protected override string ListTitle => BatchesForEditingTitle;

    // ISS-044: use GetAllBatchesAsync (maps to GetAllBatches SP) — legacy showed all statuses
    protected override Task<IReadOnlyList<BatchListResult>> FetchBatchesAsync() => BatchService.GetAllBatchesAsync();

    public IReadOnlyList<BatchListResult> GetPagedEntries() =>
        TakePage(SortColumn switch
        {
            "ProjectDescription" => OrderBatches(b => b.ProjectDescription),
            "ContactDescription" => OrderBatches(b => b.ContactDescription),
            "Species"            => OrderBatches(b => b.Species),
            "BatchDate"          => OrderBatches(b => b.BatchDate),
            "Status"             => OrderBatches(b => b.Status),
            // No column clicked yet — legacy default: dvBatchesView.Sort = "ID DESC".
            _                    => Batches.OrderByDescending(b => b.ID),
        }, PageNumber);

    public async Task OnGetAsync() => await LoadListAsync();

    public IActionResult OnPostSelect(int batchId)
    {
        // This list's purpose is status management — routes to the status page, not header-field editing.
        Session.BatchID    = batchId;
        Session.ReturnPage = "/Batches/BatchesForEditing";
        Session.ReturnPageQuery = Request.QueryString.Value;
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/Batches/EditSubmissionStatus");
    }

    public async Task<IActionResult> OnPostGoAsync()
    {
        await LoadListAsync();

        if (IsQuickGoIdMissing()) return Page();

        var batch = await BatchService.GetByIdAsync(QuickGoId!.Value);
        if (batch is null)
        {
            GoError = $"Submission {QuickGoId.Value} could not be found.";
            return Page();
        }

        Session.BatchID = QuickGoId.Value;
        Session.ReturnPage = "/Batches/BatchesForEditing";
        Session.ReturnPageQuery = Request.QueryString.Value;
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/Batches/EditSubmissionStatus");
    }
}
