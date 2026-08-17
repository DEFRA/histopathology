using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>
/// Replaces <c>ViewSamples.aspx</c>.
///
/// Standalone, non-batch-scoped animal/tissue search reached directly from the
/// Home page (legacy <c>hlViewSamples</c> hyperlink, "View Samples"). This is
/// distinct from the in-progress-batch sample list served by
/// <c>Submissions/BatchBlockSummary.cshtml</c> (which replaces
/// <c>BatchSummary.aspx</c>/<c>BatchBlockSummary.aspx</c>) — the two legacy
/// pages were previously conflated under a single "ViewSamples" name during
/// migration, which caused this page to be dropped from scope entirely. That
/// naming collision has been resolved by renaming the batch-scoped page to
/// <c>BatchBlockSummary</c> and creating this page to reproduce the real
/// <c>ViewSamples.aspx</c> feature.
///
/// Legacy validation (<c>btnSearch_Click</c>): exactly one of Sender Ref /
/// Histology Ref must be supplied — an error is shown if both or neither are
/// filled in. Two mutually exclusive search modes (legacy <c>rbWetTissue</c> /
/// <c>rbBlockInformation</c> radio buttons) select between
/// <c>clsAnimal.GetAnimalTissues</c> (SP <c>GetAnimalBatchTissues</c>,
/// "Tissue Information") and <c>GetAnimalBlockTissues</c> (SP
/// <c>GetAnimalBlockTissues</c>, "Block Information") — see
/// <see cref="AnimalTissueSearchResult"/> for the resulting column shape.
/// </summary>
public class ViewSamplesModel : HistoPageModel
{
    private const int LookupTissueCode = 9;  // Legacy source: HistopathologySystem/Common.vb — LOOKUP_TISSUE_CODE
    private const int LookupProjects = 19;   // Legacy source: HistopathologySystem/Common.vb — LOOKUP_PROJECTS

    private readonly ISubmissionService _submissions;
    private readonly ILookupService _lookups;

    public ViewSamplesModel(ISessionService session, ISubmissionService submissions, ILookupService lookups)
        : base(session)
    {
        _submissions = submissions;
        _lookups = lookups;
    }

    [BindProperty] public string? SenderRef { get; set; }
    [BindProperty] public string? HistologyRef { get; set; }
    [BindProperty] public string? TissueCode { get; set; }
    [BindProperty] public string? ProjectDesc { get; set; }

    /// <summary>"Tissue" = legacy "Tissue Information" mode (default); "Block" = "Block Information" mode.</summary>
    [BindProperty] public string Mode { get; set; } = "Tissue";

    public string? ErrorMessage { get; private set; }
    public bool Searched { get; private set; }

    public IReadOnlyList<LookupItem> Tissues { get; private set; } = [];
    public IReadOnlyList<LookupItem> Projects { get; private set; } = [];
    public IReadOnlyList<AnimalTissueSearchResult> Results { get; private set; } = [];

    public async Task OnGetAsync()
    {
        SetTitle();
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitle();
        await LoadLookupsAsync();

        if (!Validate())
            return Page();

        Searched = true;
        Results = await SearchAsync();

        return Page();
    }

    /// <summary>Replaces the legacy ExcelExport.aspx links (hlTissuesExcelExport / hlExcelExport).</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        SetTitle();
        await LoadLookupsAsync();

        if (!Validate())
            return Page();

        var results = await SearchAsync();
        var isBlockMode = Mode == "Block";

        var headers = isBlockMode
            ? (IReadOnlyList<string>)new[] { "Sub. number", "Date submitted", "Date received", "Time received", "Date completed", "Customer received date", "Submitted as", "Block ref", "Tissue", "No pieces" }
            : (IReadOnlyList<string>)new[] { "Sub. number", "Date submitted", "Date received", "Time received", "Date completed", "Customer received date", "Submitted as", "Tissue", "No pieces" };

        var rows = results.Select(r => isBlockMode
            ? (IReadOnlyList<string?>)new string?[]
              {
                  r.ID.ToString(), r.DateSubmitted?.ToShortDateString(), r.DateReceived?.ToShortDateString(), r.TimeReceived,
                  r.DateCompleted?.ToShortDateString(), r.CustomerReceivedDate?.ToShortDateString(), r.SubmittedAs, r.BlockRef, r.TissueDescription, r.NoPieces?.ToString(),
              }
            : (IReadOnlyList<string?>)new string?[]
              {
                  r.ID.ToString(), r.DateSubmitted?.ToShortDateString(), r.DateReceived?.ToShortDateString(), r.TimeReceived,
                  r.DateCompleted?.ToShortDateString(), r.CustomerReceivedDate?.ToShortDateString(), r.SubmittedAs, r.TissueDescription, r.NoPieces?.ToString(),
              });

        return CsvExportHelper.BuildCsv(isBlockMode ? "BlockInformation.csv" : "TissueInformation.csv", headers, rows);
    }

    private Task<IReadOnlyList<AnimalTissueSearchResult>> SearchAsync() =>
        Mode == "Block"
            ? _submissions.GetAnimalBlockTissuesAsync(SenderRef, HistologyRef, TissueCode, ProjectDesc)
            : _submissions.GetAnimalTissuesAsync(SenderRef, HistologyRef, TissueCode, ProjectDesc);

    private bool Validate()
    {
        var hasSenderRef = !string.IsNullOrWhiteSpace(SenderRef);
        var hasHistologyRef = !string.IsNullOrWhiteSpace(HistologyRef);

        if (hasSenderRef == hasHistologyRef)
        {
            ErrorMessage = "Enter either the Sender Ref or the Histology Ref, not both.";
            return false;
        }

        return true;
    }

    private async Task LoadLookupsAsync()
    {
        Tissues = await _lookups.GetLookupDataAsync(LookupTissueCode);
        Projects = await _lookups.GetLookupDataAsync(LookupProjects);
    }

    private void SetTitle()
    {
        ViewData["Title"] = "View samples";
        ViewData["PageTitle"] = "View samples";
    }
}
