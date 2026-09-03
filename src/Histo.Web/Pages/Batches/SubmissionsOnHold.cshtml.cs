using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists batches currently on hold — replaces <c>SubmissionsOnHold.aspx</c>.
/// Shows the batch-level on-hold list with project, pathologist, species, and date columns.
/// </summary>
public class SubmissionsOnHoldModel : GridPageModel
{
    private readonly IBatchService _batches;

    public SubmissionsOnHoldModel(ISessionService session, IBatchService batches)
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

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Submissions on hold";
        ViewData["PageTitle"] = "Submissions on hold";
        Batches = await _batches.GetOnHoldAsync();
        PopulateGridViewData(TotalCount);
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/Batches/BatchDetails");
    }
}
