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
/// </summary>
public class SearchArchiveLocationModel : GridPageModel
{
    private readonly ISubmissionService _submissions;
    private readonly IBlockService _blocks;

    public SearchArchiveLocationModel(ISessionService session, ISubmissionService submissions, IBlockService blocks)
        : base(session)
    {
        _submissions = submissions;
        _blocks = blocks;
    }

    [BindProperty] public string ArchiveType { get; set; } = "Tissue";
    [BindProperty] public string? HistologyRef { get; set; }
    [BindProperty] public string? SenderRef { get; set; }
    [BindProperty] public string? ArchiveLocation { get; set; }
    [BindProperty] public string? TissueCode { get; set; }
    [BindProperty] public string? BlockRef { get; set; }

    public string? ErrorMessage { get; private set; }
    public bool Searched { get; private set; }

    public IReadOnlyList<TissueArchiveInfo> TissueResults { get; private set; } = [];
    public IReadOnlyList<BlockArchiveInfo> BlockResults { get; private set; } = [];
    public IReadOnlyList<SlideArchiveInfo> SlideResults { get; private set; } = [];

    public int TotalCount => ArchiveType switch
    {
        "Block" => BlockResults.Count,
        "Slide" => SlideResults.Count,
        _       => TissueResults.Count,
    };

    public IReadOnlyList<BlockArchiveInfo> PagedBlockResults =>
        (SortColumn switch
        {
            "BlockRef"         => SortDesc ? BlockResults.OrderByDescending(r => r.BlockRef)         : BlockResults.OrderBy(r => r.BlockRef),
            "ArchiveLocation"  => SortDesc ? BlockResults.OrderByDescending(r => r.ArchiveLocation)  : BlockResults.OrderBy(r => r.ArchiveLocation),
            "ArchivedDate"     => SortDesc ? BlockResults.OrderByDescending(r => r.ArchivedDate)     : BlockResults.OrderBy(r => r.ArchivedDate),
            "TissueDescription" => SortDesc ? BlockResults.OrderByDescending(r => r.TissueDescription) : BlockResults.OrderBy(r => r.TissueDescription),
            "NoPieces"         => SortDesc ? BlockResults.OrderByDescending(r => r.NoPieces)         : BlockResults.OrderBy(r => r.NoPieces),
            _                  => SortDesc ? BlockResults.OrderByDescending(r => r.ID)               : BlockResults.OrderBy(r => r.ID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public IReadOnlyList<SlideArchiveInfo> PagedSlideResults =>
        (SortColumn switch
        {
            "BlockRef"         => SortDesc ? SlideResults.OrderByDescending(r => r.BlockRef)         : SlideResults.OrderBy(r => r.BlockRef),
            "ArchiveLocation"  => SortDesc ? SlideResults.OrderByDescending(r => r.ArchiveLocation)  : SlideResults.OrderBy(r => r.ArchiveLocation),
            "ArchivedDate"     => SortDesc ? SlideResults.OrderByDescending(r => r.ArchivedDate)     : SlideResults.OrderBy(r => r.ArchivedDate),
            "Description"      => SortDesc ? SlideResults.OrderByDescending(r => r.Description)      : SlideResults.OrderBy(r => r.Description),
            "TissueDescription" => SortDesc ? SlideResults.OrderByDescending(r => r.TissueDescription) : SlideResults.OrderBy(r => r.TissueDescription),
            _                  => SortDesc ? SlideResults.OrderByDescending(r => r.BatchID)          : SlideResults.OrderBy(r => r.BatchID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public IReadOnlyList<TissueArchiveInfo> PagedTissueResults =>
        (SortColumn switch
        {
            "TissueDescription" => SortDesc ? TissueResults.OrderByDescending(r => r.TissueDescription) : TissueResults.OrderBy(r => r.TissueDescription),
            "ArchiveLocation"   => SortDesc ? TissueResults.OrderByDescending(r => r.ArchiveLocation)    : TissueResults.OrderBy(r => r.ArchiveLocation),
            "ArchivedDate"      => SortDesc ? TissueResults.OrderByDescending(r => r.ArchivedDate)       : TissueResults.OrderBy(r => r.ArchivedDate),
            "NoPieces"          => SortDesc ? TissueResults.OrderByDescending(r => r.NoPieces)           : TissueResults.OrderBy(r => r.NoPieces),
            _                   => SortDesc ? TissueResults.OrderByDescending(r => r.BatchID)            : TissueResults.OrderBy(r => r.BatchID),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public void OnGet()
    {
        ViewData["Title"] = "Search Archive Location";
        ViewData["PageTitle"] = "Search Archive Location";
        PopulateGridViewData(TotalCount);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["Title"] = "Search Archive Location";
        ViewData["PageTitle"] = "Search Archive Location";

        var hasSenderRef = !string.IsNullOrWhiteSpace(SenderRef);
        var hasHistologyRef = !string.IsNullOrWhiteSpace(HistologyRef);

        if (hasSenderRef == hasHistologyRef)
        {
            ErrorMessage = "Enter either the Sender Ref or the Histology Ref, not both.";
            PopulateGridViewData(TotalCount);
            return Page();
        }

        Searched = true;

        switch (ArchiveType)
        {
            case "Block":
                BlockResults = await _blocks.GetBlockArchiveAsync(SenderRef, HistologyRef, BlockRef, ArchiveLocation);
                break;
            case "Slide":
                SlideResults = await _blocks.GetSlideArchiveAsync(SenderRef, HistologyRef, ArchiveLocation);
                break;
            default:
                TissueResults = await _submissions.GetTissueArchiveAsync(SenderRef, HistologyRef, ArchiveLocation, TissueCode);
                break;
        }

        PopulateGridViewData(TotalCount);
        return Page();
    }

    /// <summary>Replaces the legacy ExcelExport.aspx link — exports the current results as CSV.</summary>
    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        switch (ArchiveType)
        {
            case "Block":
                var blockResults = await _blocks.GetBlockArchiveAsync(SenderRef, HistologyRef, BlockRef, ArchiveLocation);
                return CsvExportHelper.BuildCsv(
                    "BlockArchive.csv",
                    ["Submission number", "Block ref", "Archive location", "Archived date", "Tissue", "No pieces"],
                    blockResults.Select(r => (IReadOnlyList<string?>)new string?[]
                    {
                        r.ID.ToString(), r.BlockRef, r.ArchiveLocation, r.ArchivedDate?.ToShortDateString(), r.TissueDescription, r.NoPieces?.ToString()
                    }));

            case "Slide":
                var slideResults = await _blocks.GetSlideArchiveAsync(SenderRef, HistologyRef, ArchiveLocation);
                return CsvExportHelper.BuildCsv(
                    "SlideArchive.csv",
                    ["Submission number", "Block ref", "Archive location", "Archived date", "Slide", "Tissue"],
                    slideResults.Select(r => (IReadOnlyList<string?>)new string?[]
                    {
                        r.BatchID.ToString(), r.BlockRef, r.ArchiveLocation, r.ArchivedDate?.ToShortDateString(), r.Description, r.TissueDescription
                    }));

            default:
                var tissueResults = await _submissions.GetTissueArchiveAsync(SenderRef, HistologyRef, ArchiveLocation, TissueCode);
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
