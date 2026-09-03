using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists batches awaiting Quality Data entry — replaces <c>BatchesForDispatch.aspx</c>.
/// Legacy source: <c>clsBatch.GetBatchesForDispatch</c> → <c>GetBatchesForDispatch</c> SP.
/// </summary>
public class BatchesForDispatchModel : GridPageModel
{
    private readonly IBatchService _batches;

    public BatchesForDispatchModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    public int TotalCount => Batches.Count;

    public IReadOnlyList<BatchListResult> PagedEntries =>
        (SortColumn switch
        {
            "ProjectDescription" => SortDesc ? Batches.OrderByDescending(b => b.ProjectDescription) : Batches.OrderBy(b => b.ProjectDescription),
            "ContactDescription" => SortDesc ? Batches.OrderByDescending(b => b.ContactDescription) : Batches.OrderBy(b => b.ContactDescription),
            "Species"            => SortDesc ? Batches.OrderByDescending(b => b.Species)            : Batches.OrderBy(b => b.Species),
            "BatchDate"          => SortDesc ? Batches.OrderByDescending(b => b.BatchDate)           : Batches.OrderBy(b => b.BatchDate),
            _                    => SortDesc ? Batches.OrderByDescending(b => b.ID)                  : Batches.OrderBy(b => b.ID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    /// <summary>Quick-Go: direct navigation by submission number.</summary>
    [BindProperty]
    public int? QuickGoId { get; set; }

    /// <summary>Inline error message for Quick-Go validation failures.</summary>
    public string? GoError { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Submissions available for quality data entry";
        ViewData["PageTitle"] = "Submissions available for quality data entry";
        Batches = await _batches.GetForDispatchAsync();
        PopulateGridViewData(TotalCount);
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/QC/QualityData");
    }

    public async Task<IActionResult> OnPostGoAsync()
    {
        ViewData["Title"] = "Submissions available for quality data entry";
        ViewData["PageTitle"] = "Submissions available for quality data entry";
        Batches = await _batches.GetForDispatchAsync();
        PopulateGridViewData(TotalCount);

        if (!QuickGoId.HasValue || QuickGoId.Value <= 0)
        {
            GoError = "Enter a submission number.";
            return Page();
        }

        // Use the dispatch list itself as the validation source — if the SP returned it, it's ready.
        var batchInList = Batches.FirstOrDefault(b => b.ID == QuickGoId.Value);
        if (batchInList is null)
        {
            GoError = $"Submission {QuickGoId.Value} could not be found or is not ready for quality data entry.";
            return Page();
        }

        Session.BatchID = QuickGoId.Value;
        return RedirectToPage("/QC/QualityData");
    }
}
