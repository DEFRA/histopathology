using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>Replaces <c>BatchesForArchiving.aspx</c>.</summary>
public class BatchesForArchivingModel : BatchListPageModelBase
{
    private const string BatchesForArchivingTitle = "Batches for Archiving";

    public BatchesForArchivingModel(ISessionService session, IBatchService batches)
        : base(session, batches) { }

    protected override string ListTitle => BatchesForArchivingTitle;

    protected override Task<IReadOnlyList<BatchListResult>> FetchBatchesAsync() => BatchService.GetCompletedAsync();

    public IReadOnlyList<BatchListResult> GetPagedEntries() =>
        TakePage(SortColumn switch
        {
            "ProjectDescription" => OrderBatches(b => b.ProjectDescription),
            "ContactDescription" => OrderBatches(b => b.ContactDescription),
            "Species"            => OrderBatches(b => b.Species),
            "CompletedDate"      => OrderBatches(b => b.CompletedDate),
            "ID"                 => OrderBatches(b => b.ID),
            // No column clicked yet — legacy default: dvBatchesView.Sort = "ID DESC".
            _                    => Batches.OrderByDescending(b => b.ID),
        }, PageNumber);

    public async Task OnGetAsync() => await LoadListAsync();

    public async Task<IActionResult> OnPostGoAsync()
    {
        await LoadListAsync();

        if (IsQuickGoIdMissing()) return Page();

        var batchInList = Batches.FirstOrDefault(b => b.ID == QuickGoId.Value);
        if (batchInList is null)
        {
            GoError = $"Submission {QuickGoId.Value} could not be found or is not ready for archiving.";
            return Page();
        }

        // Legacy btnGo_Click sets Session.BatchID then redirects to ArchiveMenu.aspx.
        Session.ReturnPage = "/Batches/BatchesForArchiving";
        return RedirectToPage("/Archive/ArchiveMenu", new { batchId = QuickGoId.Value });
    }
}
