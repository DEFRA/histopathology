using Histo.Submissions.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Admin;

/// <summary>
/// Admin data-correction utility — replaces <c>FixCompletedDates.aspx</c>.
///
/// Recomputes the <c>CompletedDate</c> of every cassetted batch whose tests have
/// all been dispatched. See <see cref="BatchService.FixCompletedDatesAsync"/>.
/// </summary>
public class FixCompletedDatesModel : HistoPageModel
{
    private const string FixCompletedDatesTitle = "Fix Completed Dates";

    private readonly IBatchService _batches;

    public FixCompletedDatesModel(ISessionService session, IBatchService batches)
        : base(session) => _batches = batches;

    public int? BatchesUpdated { get; private set; }

    public void OnGet()
    {
        ViewData["Title"] = FixCompletedDatesTitle;
        ViewData["PageTitle"] = FixCompletedDatesTitle;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = FixCompletedDatesTitle;
        ViewData["PageTitle"] = FixCompletedDatesTitle;
        BatchesUpdated = await _batches.FixCompletedDatesAsync();
        return Page();
    }
}
