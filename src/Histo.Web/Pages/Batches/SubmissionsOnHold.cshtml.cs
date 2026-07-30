using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Lists batches on hold — replaces <c>SubmissionsOnHold.aspx</c>.
/// </summary>
public class SubmissionsOnHoldModel : HistoPageModel
{
    private readonly BatchService _batches;

    public SubmissionsOnHoldModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    public IReadOnlyList<Batch> Batches { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Submissions On Hold";
        ViewData["PageTitle"] = "Submissions On Hold";
        Batches = await _batches.GetOnHoldAsync();
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/BatchDetails");
    }
}
