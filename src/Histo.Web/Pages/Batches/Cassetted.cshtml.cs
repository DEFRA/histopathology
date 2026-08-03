using Histo.Administration.Models;
using Histo.Administration.Services;
using Histo.Core.Domain;
using Histo.Submissions.Models;
using Histo.Submissions.Services;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces <c>Cassetted.aspx</c> — the "Submission Type" step shown when the
/// user starts a new submission from Home. Despite the legacy page name, this
/// is not a blocks status transition; it lets the user pick how the samples
/// are being submitted (Wet Tissue, Blocks, etc.) from the SubmittedAs pick
/// list, creates the new batch header, and routes to Batch Details.
///
/// Legacy source: Cassetted.aspx.vb. The multi-select checkbox list in the
/// legacy page enforces single-selection in code (<c>chkblSubmittedAs_SelectedIndexChanged</c>);
/// this is reproduced here as a single-select dropdown.
///
/// ISS-023 fix: the legacy Home.aspx exposed two separate links ("New TSE Submission"
/// and "New Non-TSE Submission") that set <c>SV_SubmissionType</c> in session before
/// routing here. This page now captures that choice directly via the BatchType
/// radio group, consistent with GOV.UK single-page-per-question design.
/// </summary>
public class CassettedModel : HistoPageModel
{
    private const int LookupSubmittedAs = 11; // Legacy source: HistopathologySystem/Common.vb — LOOKUP_SUBMITTEDAS

    private readonly LookupService _lookups;
    private readonly BatchService _batches;

    public CassettedModel(ISessionService session, LookupService lookups, BatchService batches)
        : base(session)
    {
        _lookups = lookups;
        _batches = batches;
    }

    [BindProperty] public int? SubmittedAs { get; set; }

    /// <summary>0 = TSE (default), 1 = Non-TSE. Mirrors legacy SUBMISSION_TSE/SUBMISSION_NONTSE.</summary>
    [BindProperty] public int BatchType { get; set; } = BatchTypeConstants.Tse;

    public IReadOnlyList<LookupItem> SubmittedAsOptions { get; private set; } = [];
    public string? SaveError { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Submission Type";
        ViewData["PageTitle"] = "Submission Type";
        SubmittedAsOptions = await _lookups.GetLookupDataAsync(LookupSubmittedAs);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Submission Type";
        ViewData["PageTitle"] = "Submission Type";
        SubmittedAsOptions = await _lookups.GetLookupDataAsync(LookupSubmittedAs);

        var selected = SubmittedAsOptions.FirstOrDefault(o => o.ID == SubmittedAs);
        if (selected is null)
        {
            SaveError = "Select a submission type.";
            return Page();
        }

        var batch = new Batch
        {
            SubmittedByUserID = Session.UserID,
            UserAreaCode      = Session.UserAreaID,
            IsPreCassetted    = ValidationHelpers.IsBatchPreCassetted(selected.ID.ToString()),
            BatchType         = BatchType,
        };

        var batchId = await _batches.AddAsync(batch, Session.UserID);
        if (batchId <= 0)
        {
            SaveError = "Failed to create the new submission.";
            return Page();
        }

        Session.BatchID   = batchId;
        Session.BatchType = BatchType;  // ISS-023: persist for downstream lookup selection
        return RedirectToPage("/Batches/BatchDetails");
    }
}
