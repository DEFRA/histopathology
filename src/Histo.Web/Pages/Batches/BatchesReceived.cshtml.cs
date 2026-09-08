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
public class BatchesReceivedModel : GridPageModel
{
    private readonly IBatchService _batches;

    public BatchesReceivedModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    public int TotalCount => Batches.Count;

    public IReadOnlyList<BatchListResult> PagedEntries =>
        (SortColumn switch
        {
            "ID"                  => SortDesc ? Batches.OrderByDescending(b => b.ID)                  : Batches.OrderBy(b => b.ID),
            "ProjectDescription"  => SortDesc ? Batches.OrderByDescending(b => b.ProjectDescription)  : Batches.OrderBy(b => b.ProjectDescription),
            "ContactDescription"  => SortDesc ? Batches.OrderByDescending(b => b.ContactDescription)  : Batches.OrderBy(b => b.ContactDescription),
            "Species"             => SortDesc ? Batches.OrderByDescending(b => b.Species)             : Batches.OrderBy(b => b.Species),
            "BatchDate"           => SortDesc ? Batches.OrderByDescending(b => b.BatchDate)           : Batches.OrderBy(b => b.BatchDate),
            "AllTissuesAssigned"  => SortDesc ? Batches.OrderByDescending(b => b.AllTissuesAssigned)  : Batches.OrderBy(b => b.AllTissuesAssigned),
            // Legacy default (no column clicked yet): dtBatchesView.Sort = "ID DESC".
            _                     => Batches.OrderByDescending(b => b.ID),
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
        ViewData["Title"] = "Batches received";
        ViewData["PageTitle"] = "Batches received";
        Batches = await _batches.GetReceivedAsync();
        PopulateGridViewData(TotalCount);
    }

    /// <summary>Row select — legacy source: <c>grdBatches_SelectedIndexChanged</c>, redirects to <c>BatchBlocks.aspx</c>.</summary>
    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/Batches/BatchBlocks");
    }

    /// <summary>
    /// Quick-Go — legacy source: <c>btnGo_Click</c>, which checks the entered submission number
    /// exists with status InProgress OR Received (<c>GetBatchWithStatus</c> SP) before redirecting
    /// to <c>BatchBlocks.aspx</c>; shows an inline error otherwise.
    /// </summary>
    public async Task<IActionResult> OnPostGoAsync()
    {
        ViewData["Title"] = "Batches received";
        ViewData["PageTitle"] = "Batches received";
        Batches = await _batches.GetReceivedAsync();
        PopulateGridViewData(TotalCount);

        if (!QuickGoId.HasValue || QuickGoId.Value <= 0)
        {
            GoError = "Enter a submission number.";
            return Page();
        }

        var batch = await _batches.GetByIdAsync(QuickGoId.Value);
        if (batch is null || batch.Status is not (BatchStatus.Received or BatchStatus.InProgress))
        {
            GoError = $"Submission {QuickGoId.Value} could not be found or does not have the required status.";
            return Page();
        }

        Session.BatchID = QuickGoId.Value;
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/Batches/BatchBlocks");
    }
}

