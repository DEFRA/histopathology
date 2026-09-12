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
/// Validation: at least one of Sender Ref / Histology Ref must be supplied — both
/// stored procedures tolerate both being given (each branches internally on one
/// ref, ignoring the other; confirmed via <c>sp_helptext</c> — <c>GetAnimalBatchTissues</c>
/// (Tissue mode) prefers Sender Ref, <c>GetAnimalBlockTissues</c> (Block mode) prefers
/// Histology Ref), so an exactly-one-only rule was an unnecessarily strict UI
/// invention. Two mutually exclusive search modes (legacy <c>rbWetTissue</c> /
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

    // Sort/page state bound the same way as the filter criteria — [BindProperty] carries it
    // through the POST-based sort/page buttons (see _SortableHeaderPost/_PaginationPost), which
    // resubmit this same form rather than navigating via a GET link.
    private const int PageSize = 10;
    [BindProperty] public string? SortColumn { get; set; }
    [BindProperty] public bool SortDesc { get; set; }
    [BindProperty] public int PageNumber { get; set; } = 1;

    public Dictionary<string, string> Errors { get; } = [];
    public bool Searched { get; private set; }

    public IReadOnlyList<LookupItem> Tissues { get; private set; } = [];
    public IReadOnlyList<LookupItem> Projects { get; private set; } = [];
    public IReadOnlyList<AnimalTissueSearchResult> Results { get; private set; } = [];

    public IReadOnlyList<AnimalTissueSearchResult> PagedResults =>
        (SortColumn switch
        {
            "DateSubmitted"  => SortDesc ? Results.OrderByDescending(r => r.DateSubmitted)  : Results.OrderBy(r => r.DateSubmitted),
            "DateReceived"   => SortDesc ? Results.OrderByDescending(r => r.DateReceived)   : Results.OrderBy(r => r.DateReceived),
            "TimeReceived" => SortDesc ? Results.OrderByDescending(r => r.TimeReceived) : Results.OrderBy(r => r.TimeReceived),
            "DateCompleted"  => SortDesc ? Results.OrderByDescending(r => r.DateCompleted)  : Results.OrderBy(r => r.DateCompleted),
            "SubmittedAs"    => SortDesc ? Results.OrderByDescending(r => r.SubmittedAs)    : Results.OrderBy(r => r.SubmittedAs),
            "NoPieces" => SortDesc ? Results.OrderByDescending(r => r.NoPieces) : Results.OrderBy(r => r.NoPieces),
            "CustomerReceivedDate" => SortDesc ? Results.OrderByDescending(r => r.CustomerReceivedDate) : Results.OrderBy(r => r.CustomerReceivedDate),
            "TissueDescription" => SortDesc ? Results.OrderByDescending(r => r.TissueDescription) : Results.OrderBy(r => r.TissueDescription),
            _                => SortDesc ? Results.OrderByDescending(r => r.ID) : Results.OrderBy(r => r.ID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    private void PopulateGridViewData()
    {
        var totalPages = Results.Count == 0 ? 1 : (int)Math.Ceiling(Results.Count / (double)PageSize);
        if (PageNumber < 1) PageNumber = 1;
        else if (PageNumber > totalPages) PageNumber = totalPages;
        ViewData["SortColumn"] = SortColumn;
        ViewData["SortDesc"] = SortDesc;
        ViewData["CurrentPage"] = PageNumber;
        ViewData["TotalPages"] = totalPages;
        ViewData["FormId"] = "view-samples-form";
        ViewData["Handler"] = "Search";
    }

    public async Task OnGetAsync()
    {
        SetTitle();
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        SetTitle();
        await LoadLookupsAsync();

        if (!Validate())
            return Page();

        Searched = true;
        Results = await SearchAsync();
        PopulateGridViewData();

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

    private Task<IReadOnlyList<AnimalTissueSearchResult>> SearchAsync()
    {
        var senderRef = NullIfEmpty(SenderRef);
        var histologyRef = NullIfEmpty(HistologyRef);
        var tissueCode = NullIfEmpty(TissueCode);
        var projectDesc = NullIfEmpty(ProjectDesc);

        return Mode == "Block"
            ? _submissions.GetAnimalBlockTissuesAsync(senderRef, histologyRef, tissueCode, projectDesc)
            : _submissions.GetAnimalTissuesAsync(senderRef, histologyRef, tissueCode, projectDesc);
    }

    // The stored procedures treat an unapplied filter as "@Param IS NULL" — an empty string
    // (what a blank text input actually posts) never satisfies that check, so it silently
    // filters out every row instead of being ignored.
    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private bool Validate()
    {
        var hasSenderRef = !string.IsNullOrWhiteSpace(SenderRef);
        var hasHistologyRef = !string.IsNullOrWhiteSpace(HistologyRef);

        // Both stored procedures tolerate both being supplied (each ignores the other, with its
        // own internal precedence) — only reject when NEITHER is given.
        if (!hasSenderRef && !hasHistologyRef)
        {
            Errors[nameof(SenderRef)] = "Enter the Sender Ref or the Histology Ref.";
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
