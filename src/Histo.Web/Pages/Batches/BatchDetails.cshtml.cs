using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>Replaces <c>BatchDetails.aspx</c>.</summary>
public class BatchDetailsModel : HistoPageModel
{
    private readonly BatchService _batches;

    public BatchDetailsModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    public Batch? Batch { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Batch Details";
        ViewData["PageTitle"] = "Batch Details";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");
        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        return Page();
    }
}
