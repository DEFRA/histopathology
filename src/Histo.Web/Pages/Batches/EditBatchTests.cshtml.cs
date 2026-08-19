using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces the "Select Histology and required tests" inline section of legacy
/// <c>BatchDetails.aspx</c>.  Following the GDS one-thing-per-page principle,
/// batch-level test-type selection has been extracted into this dedicated page.
///
/// Legacy source:
/// <list type="bullet">
/// <item>Three <c>CheckBoxList</c> controls: <c>chkblHistology</c>, <c>chkblAntibodies</c>,
///       <c>chkblSpecialStain</c></item>
/// <item>Lookup loading: <c>LookupData.GetHistologyLookupData()</c>,
///       <c>GetLookupData(LOOKUP_TSE_ANTIBODIES)</c>, <c>GetLookupData(LOOKUP_SPECIAL_STAIN)</c></item>
/// <item>Save: <c>clsCheckBoxData.UpdateTable</c> for BATCH_HISTOLOGY_TABLE,
///       BATCH_ANTIBODIES_TABLE, BATCH_STAIN_TABLE</item>
/// <item>Validation: <c>BatchDetails.aspx.vb::CheckHistology()</c></item>
/// </list>
///
/// Business rules preserved:
/// <list type="bullet">
/// <item>At least one histology type must be selected.</item>
/// <item>If "Special Stain" (code "3") is selected, at least one special stain must be chosen.</item>
/// <item>If "IHC-PrP" (code "4") or "IHC-Other" (code "6") is selected, at least one antibody must be chosen.</item>
/// <item>For TSE submissions, histology code "6" (IHC-Other) is not shown.</item>
/// <item>For NonTSE submissions, histology codes "4" (IHC-PrP) and "5" (H&amp;E BSE) are not shown.</item>
/// </list>
/// </summary>
public class EditBatchTestsModel : HistoPageModel
{
    // Legacy lookup table IDs (HistopathologySystem/Common.vb)
    private const int LookupTseAntibodies    = 4;   // LOOKUP_TSE_ANTIBODIES
    private const int LookupNonTseAntibodies = 5;   // LOOKUP_NONTSE_ANTIBODIES
    private const int LookupSpecialStain     = 6;   // LOOKUP_SPECIAL_STAIN

    private readonly IBatchService  _batches;
    private readonly ILookupService _lookups;

    public EditBatchTestsModel(ISessionService session, IBatchService batches, ILookupService lookups)
        : base(session)
    {
        _batches = batches;
        _lookups = lookups;
    }

    // ── Bind properties — multi-select checkbox groups ─────────────────────

    /// <summary>Selected histology type codes (at least one required).</summary>
    [BindProperty] public List<string> SelectedHistologyCodes { get; set; } = [];

    /// <summary>Selected antibody codes (required when IHC-PrP or IHC-Other is ticked).</summary>
    [BindProperty] public List<string> SelectedAntibodyCodes  { get; set; } = [];

    /// <summary>Selected special stain codes (required when Special Stain is ticked).</summary>
    [BindProperty] public List<string> SelectedStainCodes     { get; set; } = [];

    // ── Page-state properties ──────────────────────────────────────────────

    public Batch? Batch { get; private set; }

    /// <summary>All available histology type options filtered for TSE or NonTSE.</summary>
    public IReadOnlyList<LookupItem> HistologyOptions  { get; private set; } = [];

    /// <summary>Antibody options (TSE table 4 or NonTSE table 5 depending on batch type).</summary>
    public IReadOnlyList<LookupItem> AntibodyOptions   { get; private set; } = [];

    /// <summary>Special stain options (lookup table 6).</summary>
    public IReadOnlyList<LookupItem> StainOptions      { get; private set; } = [];

    public string? SaveError { get; private set; }

    /// <summary>
    /// True when the antibody section should be conditionally shown — i.e. when the current
    /// histology selection contains IHC-PrP (TSE) or IHC-Other (NonTSE).
    /// Used by the Razor view to decide whether to expand the conditional reveal section.
    /// </summary>
    public bool ShowAntibodies => SelectedHistologyCodes.Contains(HistologyCode.IhcPrp)
                                || SelectedHistologyCodes.Contains(HistologyCode.IhcOther);

    /// <summary>
    /// True when the special stain section should be conditionally shown — i.e. when the current
    /// histology selection contains Special Stain (code "3").
    /// </summary>
    public bool ShowStains => SelectedHistologyCodes.Contains(HistologyCode.SpecialStain);

    // ── Handlers ───────────────────────────────────────────────────────────

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"]     = "Edit test types";
        ViewData["PageTitle"] = "Edit test types";
        if (Session.BatchID <= 0) return RedirectToPage("/Index");

        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch is null) return RedirectToPage("/Index");

        await LoadLookupOptionsAsync(Batch.BatchType);

        // Pre-select the checkboxes from the existing batch-level test selections.
        var current = await _batches.GetBatchTestSelectionsAsync(Session.BatchID ?? 0);
        SelectedHistologyCodes = current.Histology.Select(r => r.Code).ToList();
        SelectedAntibodyCodes  = current.Antibodies.Select(r => r.Code).ToList();
        SelectedStainCodes     = current.Stains.Select(r => r.Code).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"]     = "Edit test types";
        ViewData["PageTitle"] = "Edit test types";

        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch is null) return RedirectToPage("/Index");

        await LoadLookupOptionsAsync(Batch.BatchType);

        // ── Validation (mirrors BatchDetails.aspx.vb::CheckHistology) ──────

        if (SelectedHistologyCodes.Count == 0)
        {
            SaveError = "Select at least one histology type.";
            return Page();
        }

        if (SelectedHistologyCodes.Contains(HistologyCode.SpecialStain) && SelectedStainCodes.Count == 0)
        {
            SaveError = "Special Stain is selected — you must also select at least one special stain.";
            return Page();
        }

        var ihcSelected = SelectedHistologyCodes.Contains(HistologyCode.IhcPrp)
                       || SelectedHistologyCodes.Contains(HistologyCode.IhcOther);
        if (ihcSelected && SelectedAntibodyCodes.Count == 0)
        {
            SaveError = "IHC is selected — you must also select at least one antibody.";
            return Page();
        }

        // Clear stain/antibody selections if the triggering histology codes are not present.
        var cleanedStainCodes    = SelectedHistologyCodes.Contains(HistologyCode.SpecialStain)
                                       ? SelectedStainCodes
                                       : (IReadOnlyList<string>)[];
        var cleanedAntibodyCodes = ihcSelected
                                       ? SelectedAntibodyCodes
                                       : (IReadOnlyList<string>)[];

        var ok = await _batches.SaveBatchTestSelectionsAsync(
            Session.BatchID ?? 0,
            SelectedHistologyCodes,
            cleanedAntibodyCodes,
            cleanedStainCodes,
            Session.UserID);

        if (!ok)
        {
            SaveError = "Failed to save test types. Please try again.";
            return Page();
        }

        return RedirectToPage("/Batches/BatchDetails");
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task LoadLookupOptionsAsync(int batchType)
    {
        var antibodyTableId = batchType == BatchTypeConstants.NonTse
            ? LookupNonTseAntibodies
            : LookupTseAntibodies;

        var histologyTask = _lookups.GetHistologyTypesAsync();
        var antibodyTask  = _lookups.GetLookupDataAsync(antibodyTableId);
        var stainTask     = _lookups.GetLookupDataAsync(LookupSpecialStain);

        await Task.WhenAll(histologyTask, antibodyTask, stainTask);

        // Filter histology options by submission type.
        // TSE:    hide IHC-Other (code "6")
        // NonTSE: hide IHC-PrP (code "4") and H&E(BSE) (code "5")
        // Legacy source: BatchDetails.aspx.vb::HideOptions()
        HistologyOptions = batchType == BatchTypeConstants.NonTse
            ? histologyTask.Result
                .Where(i => i.Code != HistologyCode.IhcPrp && i.Code != HistologyCode.HeBse)
                .ToList()
            : histologyTask.Result
                .Where(i => i.Code != HistologyCode.IhcOther)
                .ToList();

        AntibodyOptions = antibodyTask.Result;
        StainOptions    = stainTask.Result;
    }
}
