using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists batches currently on hold — replaces <c>SubmissionsOnHold.aspx</c>.
/// Shows the batch-level on-hold list with project, pathologist, species, and date columns.
/// </summary>
public class SubmissionsOnHoldModel : HistoPageModel
{
    private readonly IBatchService _batches;

    public SubmissionsOnHoldModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<BatchListResult> Batches { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Submissions on hold";
        ViewData["PageTitle"] = "Submissions on hold";
        Batches = await _batches.GetOnHoldAsync();
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        Session.IsViewSubmissionMode = false;
        return RedirectToPage("/Batches/BatchDetails");
    }
}
