using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>Replaces <c>BatchesForArchiving.aspx</c>.</summary>
public class BatchesForArchivingModel : GridPageModel
{
    private readonly IBatchService _batches;

    public BatchesForArchivingModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    public int TotalCount => Batches.Count;

    public IReadOnlyList<BatchListResult> PagedEntries =>
        (SortColumn switch
        {
            "ProjectDescription" => SortDesc ? Batches.OrderByDescending(b => b.ProjectDescription) : Batches.OrderBy(b => b.ProjectDescription),
            "ContactDescription" => SortDesc ? Batches.OrderByDescending(b => b.ContactDescription) : Batches.OrderBy(b => b.ContactDescription),
            "Species"            => SortDesc ? Batches.OrderByDescending(b => b.Species)            : Batches.OrderBy(b => b.Species),
            "OtherSubmittedBy"   => SortDesc ? Batches.OrderByDescending(b => b.OtherSubmittedBy)   : Batches.OrderBy(b => b.OtherSubmittedBy),
            "CompletedDate"      => SortDesc ? Batches.OrderByDescending(b => b.CompletedDate)      : Batches.OrderBy(b => b.CompletedDate),
            "ID"                 => SortDesc ? Batches.OrderByDescending(b => b.ID)                  : Batches.OrderBy(b => b.ID),
            // No column clicked yet — legacy default: dvBatchesView.Sort = "ID DESC".
            _                    => Batches.OrderByDescending(b => b.ID),
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
        ViewData["Title"] = "Batches for Archiving";
        ViewData["PageTitle"] = "Batches for Archiving";
        Batches = await _batches.GetCompletedAsync();
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostGoAsync()
    {
        ViewData["Title"] = "Batches for Archiving";
        ViewData["PageTitle"] = "Batches for Archiving";
        Batches = await _batches.GetCompletedAsync();
        PopulateGridViewData(TotalCount);

        if (!QuickGoId.HasValue || QuickGoId.Value <= 0)
        {
            GoError = "Enter a submission number.";
            return Page();
        }

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
