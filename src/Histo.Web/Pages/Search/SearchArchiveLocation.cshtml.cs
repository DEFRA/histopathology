using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Search;

/// <summary>
/// Replaces <c>SearchArchiveLocation.aspx</c>.
///
/// SIMPLIFIED: the legacy page renders three hierarchical, expand/collapse
/// grids (Tissue / Block / Slide archive) built row-by-row in code-behind.
/// This page shows the same three search modes and result sets as flat
/// tables — see <see cref="TissueArchiveInfo"/>, <see cref="BlockArchiveInfo"/>
/// and <see cref="SlideArchiveInfo"/> for details of what was not reproduced.
///
/// Legacy's result grids have no sorting or paging — matched here by not
/// inheriting <c>GridPageModel</c>.
///
/// GET-based (all filters bound via <c>SupportsGet</c>): results/filters live in the
/// query string, so they're restored correctly by the browser Back button and are
/// bookmarkable/shareable — matching the established pattern already used by
/// <see cref="Histo.Web.Pages.Search.SearchSubmissionsModel"/>. Previously this page
/// used a POST form, which loses all of this on Back.
/// </summary>
public class SearchArchiveLocationModel : HistoPageModel
{
    private const int LookupArchiveLocation = 16; // Code-keyed; see LookupItem.Code doc comment.
    private const int LookupTissueCode = 9; // Legacy source: HistopathologySystem/Common.vb — LOOKUP_TISSUE_CODE

    private readonly ISubmissionService _submissions;
    private readonly IBlockService _blocks;
    private readonly ILookupService _lookups;

    public SearchArchiveLocationModel(ISessionService session, ISubmissionService submissions, IBlockService blocks, ILookupService lookups)
        : base(session)
    {
        _submissions = submissions;
        _blocks = blocks;
        _lookups = lookups;
    }

    [BindProperty(SupportsGet = true)] public string ArchiveType { get; set; } = "Tissue";
    [BindProperty(SupportsGet = true)] public string? HistologyRef { get; set; }
    [BindProperty(SupportsGet = true)] public string? SenderRef { get; set; }
    [BindProperty(SupportsGet = true)] public string? ArchiveLocation { get; set; }
    [BindProperty(SupportsGet = true)] public string? TissueCode { get; set; }
    [BindProperty(SupportsGet = true)] public string? BlockRef { get; set; }

    // Distinguishes "user clicked Search with nothing filled in" from a first, bare page
    // visit — both would otherwise look identical (no query string at all).
    [BindProperty(SupportsGet = true)] public bool Submitted { get; set; }

    public Dictionary<string, string> Errors { get; } = [];
    public bool Searched { get; private set; }

    /// <summary>Tissue pick list (table 9) — populates the Tissue code dropdown for Tissue archive mode.</summary>
    public IReadOnlyList<LookupItem> Tissues { get; private set; } = [];

    /// <summary>Archive location pick list (table 16) — only Tissue/Slide archive use a dropdown; Block archive is free text.</summary>
    public IReadOnlyList<LookupItem> ArchiveLocations { get; private set; } = [];

    public IReadOnlyList<TissueArchiveInfo> TissueResults { get; private set; } = [];
    public IReadOnlyList<BlockArchiveInfo> BlockResults { get; private set; } = [];
    public IReadOnlyList<SlideArchiveInfo> SlideResults { get; private set; } = [];

    [BindProperty(SupportsGet = true)] public string? ReturnPage { get; set; }

    /// <summary>Only ever redirect to a path inside this application — blocks open-redirect abuse.</summary>
    public string BackLinkPage =>
        !string.IsNullOrWhiteSpace(ReturnPage) && Url.IsLocalUrl(ReturnPage) ? ReturnPage : "/Search/SearchMenu";
    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Search Archive Location";
        ViewData["PageTitle"] = "Search Archive Location";
        await LoadLookupsAsync();

        // A bare first visit (no query string) shows the empty form only.
        if (!Submitted) return;

        var hasSenderRef = !string.IsNullOrWhiteSpace(SenderRef);
        var hasHistologyRef = !string.IsNullOrWhiteSpace(HistologyRef);

        // The underlying SPs tolerate both being supplied (SenderRef takes precedence, HistologyRef
        // is then ignored) — only reject when NEITHER is given, there's nothing to filter on.
        if (!hasSenderRef && !hasHistologyRef)
        {
            Errors[nameof(HistologyRef)] = "Enter the Sender Ref or the Histology Ref.";
            return;
        }

        Searched = true;

        var senderRef = NullIfEmpty(SenderRef);
        var histologyRef = NullIfEmpty(HistologyRef);
        var archiveLocation = NullIfEmpty(ArchiveLocation);

        switch (ArchiveType)
        {
            case "Block":
                BlockResults = await _blocks.GetBlockArchiveAsync(senderRef, histologyRef, NullIfEmpty(BlockRef), archiveLocation);
                break;
            case "Slide":
                SlideResults = await _blocks.GetSlideArchiveAsync(senderRef, histologyRef, archiveLocation);
                break;
            default:
                TissueResults = await _submissions.GetTissueArchiveAsync(senderRef, histologyRef, archiveLocation, NullIfEmpty(TissueCode));
                break;
        }
    }

    // The stored procedures treat an unapplied filter as "@Param IS NULL" — an empty string
    // (what a blank text input/unselected dropdown actually posts) never satisfies that check,
    // so it silently filters out every row instead of being ignored.
    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private async Task LoadLookupsAsync()
    {
        ArchiveLocations = await _lookups.GetLookupDataAsync(LookupArchiveLocation);
        Tissues = await _lookups.GetLookupDataAsync(LookupTissueCode);
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as .xlsx.</summary>
    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        var senderRef = NullIfEmpty(SenderRef);
        var histologyRef = NullIfEmpty(HistologyRef);
        var archiveLocation = NullIfEmpty(ArchiveLocation);

        switch (ArchiveType)
        {
            case "Block":
                var blockResults = await _blocks.GetBlockArchiveAsync(senderRef, histologyRef, NullIfEmpty(BlockRef), archiveLocation);
                return ExcelExportHelper.BuildXlsx(
                    "BlockArchive.xlsx",
                    ["Submission number", "Block ref", "Archive location", "Archived date", "Tissue", "No pieces"],
                    blockResults.Select(r => (IReadOnlyList<object?>)new object?[]
                    {
                        r.ID, r.BlockRef, r.ArchiveLocation, r.ArchivedDate, r.TissueDescription, r.NoPieces
                    }));

            case "Slide":
                var slideResults = await _blocks.GetSlideArchiveAsync(senderRef, histologyRef, archiveLocation);
                return ExcelExportHelper.BuildXlsx(
                    "SlideArchive.xlsx",
                    ["Submission number", "Block ref", "Archive location", "Archived date", "Slide", "Tissue"],
                    slideResults.Select(r => (IReadOnlyList<object?>)new object?[]
                    {
                        r.BatchID, r.BlockRef, r.ArchiveLocation, r.ArchivedDate, r.Description, r.TissueDescription
                    }));

            default:
                var tissueResults = await _submissions.GetTissueArchiveAsync(senderRef, histologyRef, archiveLocation, NullIfEmpty(TissueCode));
                return ExcelExportHelper.BuildXlsx(
                    "TissueArchive.xlsx",
                    ["Submission number", "Tissue", "Archive location", "Archived date", "No pieces"],
                    tissueResults.Select(r => (IReadOnlyList<object?>)new object?[]
                    {
                        r.BatchID, r.TissueDescription, r.ArchiveLocation, r.ArchivedDate, r.NoPieces
                    }));
        }
    }
}
