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
///
/// GET-based (all filters/sort/page bound via <c>SupportsGet</c>): results/filters live in
/// the query string, so they're restored correctly by the browser Back button and are
/// bookmarkable/shareable — matching the established pattern already used by
/// <see cref="Histo.Web.Pages.Search.SearchSubmissionsModel"/>. Previously this page used a
/// POST form, which loses all of this on Back.
/// </summary>
public class ViewSamplesModel : GridPageModel
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

    [BindProperty(SupportsGet = true)] public string? SenderRef { get; set; }
    [BindProperty(SupportsGet = true)] public string? HistologyRef { get; set; }
    [BindProperty(SupportsGet = true)] public string? TissueCode { get; set; }
    [BindProperty(SupportsGet = true)] public string? ProjectDesc { get; set; }

    /// <summary>"Tissue" = legacy "Tissue Information" mode (default); "Block" = "Block Information" mode.</summary>
    [BindProperty(SupportsGet = true)] public string Mode { get; set; } = "Tissue";

    // Distinguishes "user clicked Search with nothing filled in" from a first, bare page
    // visit — both would otherwise look identical (no query string at all).
    [BindProperty(SupportsGet = true)] public bool Submitted { get; set; }

    public Dictionary<string, string> Errors { get; } = [];
    public bool Searched { get; private set; }

    /// <summary>
    /// Legacy source: <c>ViewSamples.aspx.vb::FillviewGrid</c> — shows whichever ref was NOT
    /// entered by the user, resolved from the matched sample. Sender ref takes precedence when
    /// both are given (matching this page's own "either or both" relaxation).
    /// </summary>
    public string? OtherFieldLabel { get; private set; }

    public IReadOnlyList<LookupItem> Tissues { get; private set; } = [];
    public IReadOnlyList<LookupItem> Projects { get; private set; } = [];
    public IReadOnlyList<AnimalTissueSearchResult> Results { get; private set; } = [];

    [BindProperty(SupportsGet = true)] public string? ReturnPage { get; set; }


    /// <summary>Only ever redirect to a path inside this application — blocks open-redirect abuse.</summary>
    public string BackLinkPage =>
        !string.IsNullOrWhiteSpace(ReturnPage) && Url.IsLocalUrl(ReturnPage) ? ReturnPage : "/Search/SearchMenu";

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

    public async Task OnGetAsync()
    {
        SetTitle();
        await LoadLookupsAsync();

        // A bare first visit (no query string) shows the empty form only.
        if (!Submitted)
        {
            PopulateGridViewData(0);
            return;
        }

        if (!Validate())
        {
            PopulateGridViewData(0);
            return;
        }

        Searched = true;
        Results = await SearchAsync();
        OtherFieldLabel = !string.IsNullOrWhiteSpace(SenderRef)
            ? $"Histology Ref: {Results.FirstOrDefault()?.HistologyRef}"
            : !string.IsNullOrWhiteSpace(HistologyRef)
                ? $"Sender Ref: {Results.FirstOrDefault()?.SenderRef}"
                : null;
        PopulateGridViewData(Results.Count);
    }

    /// <summary>Replaces the legacy ExcelExport.aspx links (hlTissuesExcelExport / hlExcelExport).</summary>
    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        if (!Validate())
            return RedirectToPage("/Search/ViewSamples");

        var results = await SearchAsync();
        var isBlockMode = Mode == "Block";

        var headers = isBlockMode
            ? (IReadOnlyList<string>)new[] { "Sub. number", "Date submitted", "Date received", "Time received", "Date completed", "Customer received date", "Block ref", "Tissue description", "No pieces", "Histology ref", "Sender ref", "Submitted as" }
            : (IReadOnlyList<string>)new[] { "Sub. number", "Date submitted", "Date received", "Time received", "Date completed", "Customer received date", "Tissue description", "No pieces", "Histology ref", "Sender ref", "Submitted as" };

        var rows = results.Select(r => isBlockMode
            ? (IReadOnlyList<object?>)new object?[]
              {
                  r.ID, r.DateSubmitted, r.DateReceived, r.TimeReceived,
                  r.DateCompleted, r.CustomerReceivedDate, r.BlockRef, r.TissueDescription, r.NoPieces, r.HistologyRef, r.SenderRef, r.SubmittedAs,
              }
            : (IReadOnlyList<object?>)new object?[]
              {
                  r.ID, r.DateSubmitted, r.DateReceived, r.TimeReceived,
                  r.DateCompleted, r.CustomerReceivedDate, r.TissueDescription, r.NoPieces, r.HistologyRef, r.SenderRef, r.SubmittedAs,
              });

        return ExcelExportHelper.BuildXlsx(isBlockMode ? "BlockInformation.xlsx" : "TissueInformation.xlsx", headers, rows);
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
        // Both Sender ref and Histology ref are shown as plain, always-visible fields — the
        // underlying stored procedures tolerate either or both being supplied (each branches
        // internally on one and ignores the other); only reject when neither is given.
        var hasSenderRef = !string.IsNullOrWhiteSpace(SenderRef);
        var hasHistologyRef = !string.IsNullOrWhiteSpace(HistologyRef);

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
