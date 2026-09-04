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
public class BatchesNotReceivedModel : GridPageModel
{
    private readonly IBatchService _batches;

    public BatchesNotReceivedModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    public int TotalCount => Batches.Count;

    public IReadOnlyList<BatchListResult> PagedEntries =>
        (SortColumn switch
        {
            "ProjectDescription" => SortDesc ? Batches.OrderByDescending(b => b.ProjectDescription) : Batches.OrderBy(b => b.ProjectDescription),
            "ContactDescription" => SortDesc ? Batches.OrderByDescending(b => b.ContactDescription) : Batches.OrderBy(b => b.ContactDescription),
            "Species"            => SortDesc ? Batches.OrderByDescending(b => b.Species)            : Batches.OrderBy(b => b.Species),
            "SubmissionDate"     => SortDesc ? Batches.OrderByDescending(b => b.SubmissionDate)     : Batches.OrderBy(b => b.SubmissionDate),
            "OtherSubmittedBy"   => SortDesc ? Batches.OrderByDescending(b => b.OtherSubmittedBy)   : Batches.OrderBy(b => b.OtherSubmittedBy),
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
        ViewData["Title"] = "Batches not received";
        ViewData["PageTitle"] = "Batches not received";
        Batches = await _batches.GetNotReceivedAsync();
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostReceiveAsync(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/ReceiveBatch");
    }

    public async Task<IActionResult> OnPostGoAsync()
    {
        ViewData["Title"] = "Batches not received";
        ViewData["PageTitle"] = "Batches not received";
        Batches = await _batches.GetNotReceivedAsync();
        PopulateGridViewData(TotalCount);

        if (!QuickGoId.HasValue || QuickGoId.Value <= 0)
        {
            GoError = "Enter a submission number.";
            return Page();
        }

        var batch = await _batches.GetByIdAsync(QuickGoId.Value);
        if (batch is null || batch.Status != BatchStatus.Submitted)
        {
            GoError = $"Submission {QuickGoId.Value} could not be found or is not awaiting receipt.";
            return Page();
        }

        Session.BatchID = QuickGoId.Value;
        return RedirectToPage("/Batches/ReceiveBatch");
    }
}
