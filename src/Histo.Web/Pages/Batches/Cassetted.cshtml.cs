using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Step 1 of new submission � mirrors legacy <c>Cassetted.aspx</c>.
/// Collects submission category (TSE/NonTSE) and submission type (SubmittedAs) only,
/// then stores them in session and redirects to <c>BatchDetails.cshtml?mode=create</c>
/// where the user fills in all remaining batch header fields.
/// </summary>
public class CassettedModel : HistoPageModel
{
    private const int LookupSubmittedAs = 11; // Common.vb LOOKUP_SUBMITTEDAS

    private readonly ILookupService _lookups;

    public CassettedModel(ISessionService session, ILookupService lookups)
        : base(session) => _lookups = lookups;

    [BindProperty] public int  BatchType   { get; set; } = BatchTypeConstants.Tse;
    [BindProperty] public int? SubmittedAs { get; set; }

    public IReadOnlyList<LookupItem> SubmittedAsOptions { get; private set; } = [];
    public IDictionary<string, string> Errors { get; private set; } = new Dictionary<string, string>();

    public async Task OnGetAsync()
    {
        ViewData["Title"]     = "Submission type";
        ViewData["PageTitle"] = "Submission type";
        await LoadLookupsAsync();

        // Restore previous selections when the user navigates back from BatchDetails
        BatchType = Session.BatchType;
        if (TempData.TryGetValue("CreateSubmittedAsId", out var saId) && int.TryParse(saId?.ToString(), out var id))
        {
            SubmittedAs = id;
            TempData.Keep("CreateSubmittedAsId");
            TempData.Keep("CreateSubmittedAsCode");
            TempData.Keep("CreateIsPreCassetted");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"]     = "Submission type";
        ViewData["PageTitle"] = "Submission type";
        await LoadLookupsAsync();

        var errors = new Dictionary<string, string>();

        var selected = SubmittedAsOptions.FirstOrDefault(o => o.ID == SubmittedAs);
        if (selected is null)
            errors["SubmittedAs"] = "Select a submission type.";

        if (errors.Count > 0)
        {
            Errors = errors;
            return Page();
        }

        // Store type selection in session � BatchDetails create mode reads these.
        Session.BatchType = BatchType;
        Session.BatchID   = null; // clear any previous batch

        // Pass SubmittedAs code and pre-cassetted flag via TempData so BatchDetails can read them once.
        TempData["CreateSubmittedAsId"]   = selected!.ID.ToString();
        TempData["CreateSubmittedAsCode"] = selected.Code ?? selected.ID.ToString();
        TempData["CreateIsPreCassetted"]  = Histo.Core.Domain.ValidationHelpers.IsBatchPreCassetted(selected.Code ?? selected.ID.ToString()).ToString();

        return RedirectToPage("/Batches/BatchDetails", new { mode = "create" });
    }

    private async Task LoadLookupsAsync() =>
        SubmittedAsOptions = await _lookups.GetLookupDataAsync(LookupSubmittedAs);
}
