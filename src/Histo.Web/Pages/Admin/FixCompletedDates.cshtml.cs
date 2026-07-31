using Histo.Submissions.Services;
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
    private readonly BatchService _batches;

    public FixCompletedDatesModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    public int? BatchesUpdated { get; private set; }

    public void OnGet()
    {
        ViewData["Title"] = "Fix Completed Dates";
        ViewData["PageTitle"] = "Fix Completed Dates";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Fix Completed Dates";
        ViewData["PageTitle"] = "Fix Completed Dates";
        BatchesUpdated = await _batches.FixCompletedDatesAsync();
        return Page();
    }
}
