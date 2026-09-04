using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists received batches — replaces <c>BatchesReceived.aspx</c>.
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
            "ProjectDescription"  => SortDesc ? Batches.OrderByDescending(b => b.ProjectDescription)  : Batches.OrderBy(b => b.ProjectDescription),
            "ContactDescription"  => SortDesc ? Batches.OrderByDescending(b => b.ContactDescription)  : Batches.OrderBy(b => b.ContactDescription),
            "Species"             => SortDesc ? Batches.OrderByDescending(b => b.Species)             : Batches.OrderBy(b => b.Species),
            "BatchDate"           => SortDesc ? Batches.OrderByDescending(b => b.BatchDate)           : Batches.OrderBy(b => b.BatchDate),
            "ReceivedDate"        => SortDesc ? Batches.OrderByDescending(b => b.ReceivedDate)        : Batches.OrderBy(b => b.ReceivedDate),
            "OtherSubmittedBy"    => SortDesc ? Batches.OrderByDescending(b => b.OtherSubmittedBy)    : Batches.OrderBy(b => b.OtherSubmittedBy),
            "AllTissuesAssigned"  => SortDesc ? Batches.OrderByDescending(b => b.AllTissuesAssigned)  : Batches.OrderBy(b => b.AllTissuesAssigned),
            _                     => SortDesc ? Batches.OrderByDescending(b => b.ID)                  : Batches.OrderBy(b => b.ID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    /// <summary>Quick-Go: direct navigation by submission number.</summary>
    [BindProperty]
    public int? QuickGoId { get; set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Batches received";
        ViewData["PageTitle"] = "Batches received";
        Batches = await _batches.GetReceivedAsync();
        PopulateGridViewData(TotalCount);
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/Batches/BatchDetails");
    }

    public IActionResult OnPostGoAsync()
    {
        if (QuickGoId.HasValue)
            Session.BatchID = QuickGoId.Value;
        return RedirectToPage("/Batches/BatchDetails");
    }
}
