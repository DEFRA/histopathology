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

    [BindProperty] public string ArchiveType { get; set; } = "Tissue";
    [BindProperty] public string? HistologyRef { get; set; }
    [BindProperty] public string? SenderRef { get; set; }
    [BindProperty] public string? ArchiveLocation { get; set; }
    [BindProperty] public string? TissueCode { get; set; }
    [BindProperty] public string? BlockRef { get; set; }

    public Dictionary<string, string> Errors { get; } = [];
    public bool Searched { get; private set; }

    /// <summary>Tissue pick list (table 9) — populates the Tissue code dropdown for Tissue archive mode.</summary>
    public IReadOnlyList<LookupItem> Tissues { get; private set; } = [];

    /// <summary>Archive location pick list (table 16) — only Tissue/Slide archive use a dropdown; Block archive is free text.</summary>
    public IReadOnlyList<LookupItem> ArchiveLocations { get; private set; } = [];

    public IReadOnlyList<TissueArchiveInfo> TissueResults { get; private set; } = [];
    public IReadOnlyList<BlockArchiveInfo> BlockResults { get; private set; } = [];
    public IReadOnlyList<SlideArchiveInfo> SlideResults { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Search Archive Location";
        ViewData["PageTitle"] = "Search Archive Location";
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Search Archive Location";
        ViewData["PageTitle"] = "Search Archive Location";
        await LoadLookupsAsync();

        var hasSenderRef = !string.IsNullOrWhiteSpace(SenderRef);
        var hasHistologyRef = !string.IsNullOrWhiteSpace(HistologyRef);

        // The underlying SPs tolerate both being supplied (SenderRef takes precedence, HistologyRef
        // is then ignored) — only reject when NEITHER is given, there's nothing to filter on.
        if (!hasSenderRef && !hasHistologyRef)
        {
            Errors[nameof(HistologyRef)] = "Enter the Sender Ref or the Histology Ref.";
            return Page();
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

        return Page();
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

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as CSV.</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var senderRef = NullIfEmpty(SenderRef);
        var histologyRef = NullIfEmpty(HistologyRef);
        var archiveLocation = NullIfEmpty(ArchiveLocation);

        switch (ArchiveType)
        {
            case "Block":
                var blockResults = await _blocks.GetBlockArchiveAsync(senderRef, histologyRef, NullIfEmpty(BlockRef), archiveLocation);
                return CsvExportHelper.BuildCsv(
                    "BlockArchive.csv",
                    ["Submission number", "Block ref", "Archive location", "Archived date", "Tissue", "No pieces"],
                    blockResults.Select(r => (IReadOnlyList<string?>)new string?[]
                    {
                        r.ID.ToString(), r.BlockRef, r.ArchiveLocation, r.ArchivedDate?.ToShortDateString(), r.TissueDescription, r.NoPieces?.ToString()
                    }));

            case "Slide":
                var slideResults = await _blocks.GetSlideArchiveAsync(senderRef, histologyRef, archiveLocation);
                return CsvExportHelper.BuildCsv(
                    "SlideArchive.csv",
                    ["Submission number", "Block ref", "Archive location", "Archived date", "Slide", "Tissue"],
                    slideResults.Select(r => (IReadOnlyList<string?>)new string?[]
                    {
                        r.BatchID.ToString(), r.BlockRef, r.ArchiveLocation, r.ArchivedDate?.ToShortDateString(), r.Description, r.TissueDescription
                    }));

            default:
                var tissueResults = await _submissions.GetTissueArchiveAsync(senderRef, histologyRef, archiveLocation, NullIfEmpty(TissueCode));
                return CsvExportHelper.BuildCsv(
                    "TissueArchive.csv",
                    ["Submission number", "Tissue", "Archive location", "Archived date", "No pieces"],
                    tissueResults.Select(r => (IReadOnlyList<string?>)new string?[]
                    {
                        r.BatchID.ToString(), r.TissueDescription, r.ArchiveLocation, r.ArchivedDate?.ToShortDateString(), r.NoPieces?.ToString()
                    }));
        }
    }
}
