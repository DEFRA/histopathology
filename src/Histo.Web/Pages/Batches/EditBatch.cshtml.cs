using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>Replaces <c>EditBatch.aspx</c>.</summary>
public class EditBatchModel : HistoPageModel
{
    private readonly BatchService _batches;

    public EditBatchModel(ISessionService session, BatchService batches)
        : base(session) => _batches = batches;

    [BindProperty] public string CustomerRef { get; set; } = string.Empty;
    [BindProperty] public string? Comments    { get; set; }
    [BindProperty] public bool   IsPreCassetted { get; set; }

    public Batch? Batch { get; private set; }
    public string? SaveError { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Edit Batch";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");
        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch is null) return RedirectToPage("/Index");
        CustomerRef    = Batch.CustomerRef;
        Comments       = Batch.Comments;
        IsPreCassetted = Batch.IsPreCassetted;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Edit Batch";
        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch?.RowStamp is null) return RedirectToPage("/Index");

        // Full batch update wired in Phase 2 when UpdateBatchAsync is added to BatchService.
        // For now redirect back to details to preserve existing batch data.
        return RedirectToPage("/Batches/BatchDetails");
    }
}
