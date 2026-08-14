using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Read-only batch summary page — replaces <c>BatchDetails.aspx</c> in view mode
/// (<c>SessionVars.SV_ViewSubmission = True</c>).
///
/// The legacy <c>BatchDetails.aspx</c> served four distinct modes in one page:
/// New, Edit, View, and Date-Returned. In the new application these are separated
/// into distinct GDS pages:
/// <list type="bullet">
/// <item><description><c>Cassetted.cshtml</c> + <c>AddSubmission.cshtml</c> — new submission</description></item>
/// <item><description><c>EditBatch.cshtml</c> — edit submission status / comments</description></item>
/// <item><description><c>BatchDetails.cshtml</c> — read-only view (this page)</description></item>
/// <item><description><c>DateReturned.cshtml</c> — set customer received date on completed batches</description></item>
/// </list>
///
/// Action buttons are gated on batch status, matching the legacy <c>EnableDisableControls()</c> logic.
/// The back link is context-aware: it reads <see cref="ISessionService.ReturnPage"/> which is set
/// by the list page (<c>ViewSubmissions</c>, <c>SearchSubmissions</c>) before navigating here,
/// replacing the legacy <c>SessionVars.SV_RedirectCancelPage</c> pattern.
/// </summary>
public class BatchDetailsModel : HistoPageModel
{
    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;

    public BatchDetailsModel(ISessionService session, IBatchService batches, ILookupService lookups)
        : base(session)
    {
        _batches = batches;
        _lookups = lookups;
    }

    public Batch? Batch { get; private set; }

    /// <summary>Batch-level test type selections (histology, antibodies, special stains).</summary>
    public BatchTestSelections TestSelections { get; private set; } = new();

    /// <summary>
    /// Descriptions keyed by Code for each test type — used to translate stored codes to
    /// human-readable names in the summary list.
    /// </summary>
    public IReadOnlyDictionary<string, string> HistologyNames  { get; private set; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> AntibodyNames   { get; private set; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> StainNames      { get; private set; } = new Dictionary<string, string>();

    // ── Status-gated action availability (mirrors legacy EnableDisableControls) ──

    /// <summary>True when the batch can be edited — Submitted or Rejected only.</summary>
    public bool CanEdit => Batch?.Status is BatchStatus.Submitted or BatchStatus.Rejected;

    /// <summary>True when blocks can be assigned — Received or InProgress (lab is working on it).</summary>
    public bool CanAssignBlocks => Batch?.Status is BatchStatus.Received or BatchStatus.InProgress;

    /// <summary>True when the customer received date (date returned) can be set — Completed only.</summary>
    public bool CanDateReturned => Batch?.Status == BatchStatus.Completed;

    /// <summary>
    /// True when batch-level test types can be edited — Submitted, Received, or InProgress.
    /// Legacy: the histology checkboxes on BatchDetails.aspx were editable in these states.
    /// Completed/Rejected/Archived batches should not have test types changed.
    /// </summary>
    public bool CanEditTestTypes =>
        Batch?.Status is BatchStatus.Submitted or BatchStatus.Received or BatchStatus.InProgress;

    /// <summary>
    /// Page path for the back link, populated from <see cref="ISessionService.ReturnPage"/>.
    /// Falls back to <c>/Index</c> if the session value is absent (e.g. direct URL access).
    /// </summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Index"
        : Session.ReturnPage;

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Batch details";
        ViewData["PageTitle"] = "Batch details";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");
        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch is not null)
        {
            Session.BatchType = Batch.BatchType;  // ISS-023: restore from DB for downstream lookup selection

            // Load batch-level test selections and translate codes to descriptions.
            var batchId = Session.BatchID ?? 0;
            var antibodyTableId = Batch.BatchType == BatchTypeConstants.NonTse ? 5 : 4;

            var selectionsTask  = _batches.GetBatchTestSelectionsAsync(batchId);
            var histologyTask   = _lookups.GetHistologyTypesAsync();
            var antibodyTask    = _lookups.GetLookupDataAsync(antibodyTableId);
            var stainTask       = _lookups.GetLookupDataAsync(6);   // LOOKUP_SPECIAL_STAIN = 6

            await Task.WhenAll(selectionsTask, histologyTask, antibodyTask, stainTask);

            TestSelections  = selectionsTask.Result;
            HistologyNames  = ToDictionary(histologyTask.Result);
            AntibodyNames   = ToDictionary(antibodyTask.Result);
            StainNames      = ToDictionary(stainTask.Result);
        }
        return Page();
    }

    /// <summary>
    /// Builds a Code→Name dictionary from a lookup item list.
    /// Prefers <see cref="LookupItem.Code"/> as key; falls back to
    /// <see cref="LookupItem.ID"/> as string when Code is absent.
    /// </summary>
    private static IReadOnlyDictionary<string, string> ToDictionary(IReadOnlyList<LookupItem> items)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var key = !string.IsNullOrWhiteSpace(item.Code) ? item.Code : item.ID.ToString();
            dict.TryAdd(key, item.Name);
        }
        return dict;
    }
}
