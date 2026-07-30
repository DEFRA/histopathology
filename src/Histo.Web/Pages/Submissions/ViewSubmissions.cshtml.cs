using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using static Histo.Core.Domain.BatchStatus;

namespace Histo.Web.Pages.Submissions;

/// <summary>
/// Submission search page — replaces <c>ViewSubmissions.aspx</c>.
/// Accepts filter criteria via GET query params and returns matching batches.
/// </summary>
public class ViewSubmissionsModel : HistoPageModel
{
    private readonly BatchService _batches;

    public ViewSubmissionsModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public IReadOnlyList<Batch> Results { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "View Submissions";
        ViewData["PageTitle"] = "View Submissions";

        Results = StatusFilter switch
        {
            Received    => await _batches.GetReceivedAsync(),
            InProgress  => await _batches.GetInProgressAsync(),
            OnHold      => await _batches.GetOnHoldAsync(),
            _           => await _batches.GetNotReceivedAsync(),
        };
    }

    public IActionResult OnPostSelect(int batchId)
    {
        Session.BatchID = batchId;
        return RedirectToPage("/Batches/BatchDetails");
    }
}
