using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Archive;

/// <summary>One archive-grid row — a Block joined with its owning Animal's Sender/Histology ref.</summary>
public sealed class ArchiveBlockRow
{
    public int ID { get; init; }
    public string SenderRef { get; init; } = string.Empty;
    public string? HistologyRef { get; init; }
    public string BlockRef { get; init; } = string.Empty;
    public DateTime? ArchivedDate { get; init; }
    public string? ArchiveLocation { get; init; }
    public string? ArchiveLocationName { get; init; }
    public string? ArchiveComment { get; init; }
}

/// <summary>
/// Replaces <c>ArchiveBlocks.aspx</c> — archives physical blocks (cassetted/blocked batches) by
/// recording their Archive Location/Archived Date/Comment. Selecting exactly one row lets its
/// existing values be edited; selecting two or more applies only the fields actually entered
/// (blank fields left unchanged) to every selected row — mirrors legacy's <c>btnUpdate_Click</c>.
///
/// Deviation from legacy (consistent with this app's established immediate-write architecture):
/// "Update selected" persists immediately rather than staging in a session DataSet until "Done".
/// </summary>
public class ArchiveBlocksModel : GridPageModel
{
    private const int LookupArchiveLocation = 16;

    private readonly IBlockService _blocks;
    private readonly IBatchService _batches;
    private readonly ISubmissionService _submissions;
    private readonly ILookupService _lookups;
    private readonly IUserService _users;

    public ArchiveBlocksModel(ISessionService session, IBlockService blocks, IBatchService batches, ISubmissionService submissions, ILookupService lookups, IUserService users)
        : base(session) { _blocks = blocks; _batches = batches; _submissions = submissions; _lookups = lookups; _users = users; }

    [BindProperty(SupportsGet = true)]
    public int? BatchId { get; set; }

    /// <summary>
    /// Origin page to return to once archiving is done. Set explicitly by <c>ArchiveMenu</c>, which
    /// itself forwards whatever navigated to it (BatchesForArchiving or SearchSubmissions) — without
    /// this, <see cref="OnGetAsync"/> could only guess "BatchesForArchiving" for every entry route,
    /// even when reached via SearchSubmissions' "View archive" link.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnPage { get; set; }

    /// <summary>
    /// Back/Done target — honours <see cref="ISessionService.ReturnPage"/> (set on entry to this
    /// page) so a direct grid link from BatchesForArchiving skips the redundant ArchiveMenu detour.
    /// Falls back to ArchiveMenu for the menu/SearchSubmissions "View archive" entry points.
    /// </summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Archive/ArchiveMenu"
        : Session.ReturnPage;

    public Batch? Batch { get; private set; }
    public IReadOnlyList<ArchiveBlockRow> Rows { get; private set; } = [];
    public IReadOnlyList<LookupItem> ArchiveLocations { get; private set; } = [];
    public bool IsViewMode => Session.IsViewSubmissionMode;

    /// <summary>
    /// Legacy <c>ddlHistologyRefList</c> / <c>ddlBlockRefList</c> — populated from the batch's own
    /// rows (legacy built them from <c>CreateArchiveBlockSummaryData</c>'s two ref DataTables) and
    /// applied in memory, matching legacy's <c>DataView.RowFilter</c> rather than re-querying.
    /// </summary>
    [BindProperty(SupportsGet = true)] public string? FilterHistologyRef { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterBlockRef { get; set; }

    public IReadOnlyList<string> HistologyRefOptions =>
        [.. Rows.Select(r => r.HistologyRef).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).Distinct().Order()];

    public IReadOnlyList<string> BlockRefOptions =>
        [.. Rows.Select(r => r.BlockRef).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().Order()];

    public bool FilterApplied => !string.IsNullOrWhiteSpace(FilterHistologyRef) || !string.IsNullOrWhiteSpace(FilterBlockRef);

    public IReadOnlyList<ArchiveBlockRow> FilteredRows =>
        [.. Rows.Where(r =>
            (string.IsNullOrWhiteSpace(FilterHistologyRef) || string.Equals(r.HistologyRef, FilterHistologyRef, StringComparison.OrdinalIgnoreCase))
         && (string.IsNullOrWhiteSpace(FilterBlockRef) || string.Equals(r.BlockRef, FilterBlockRef, StringComparison.OrdinalIgnoreCase)))];

    // Resolved display names for the batch summary header (shared with QualityData).
    public string? ProjectName { get; private set; }
    public string? PathologistName { get; private set; }
    public string? SpeciesName { get; private set; }
    public string? EnteredByName { get; private set; }
    public string? EnteredAreaName { get; private set; }
    public string? SubmittedByName { get; private set; }
    public string? SubmittedAreaName { get; private set; }

    public int TotalCount => FilteredRows.Count;

    public IReadOnlyList<ArchiveBlockRow> PagedEntries =>
        (SortColumn switch
        {
            "SenderRef"       => SortDesc ? FilteredRows.OrderByDescending(r => r.SenderRef)       : FilteredRows.OrderBy(r => r.SenderRef),
            "HistologyRef"    => SortDesc ? FilteredRows.OrderByDescending(r => r.HistologyRef)    : FilteredRows.OrderBy(r => r.HistologyRef),
            "BlockRef"        => SortDesc ? FilteredRows.OrderByDescending(r => r.BlockRef)        : FilteredRows.OrderBy(r => r.BlockRef),
            "ArchivedDate"    => SortDesc ? FilteredRows.OrderByDescending(r => r.ArchivedDate)    : FilteredRows.OrderBy(r => r.ArchivedDate),
            "ArchiveLocation" => SortDesc ? FilteredRows.OrderByDescending(r => r.ArchiveLocationName) : FilteredRows.OrderBy(r => r.ArchiveLocationName),
            _                 => SortDesc ? FilteredRows.OrderByDescending(r => r.BlockRef)        : FilteredRows.OrderBy(r => r.BlockRef),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    [BindProperty] public List<int> SelectedIds { get; set; } = [];
    [BindProperty] public string? ArchiveLocationCode { get; set; }
    [BindProperty] public DateTime? ArchivedDate { get; set; }
    [BindProperty] public string? Comment { get; set; }

    public string? Error { get; private set; }
    public string? SuccessMessage { get; private set; }

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Archive blocks";
        ViewData["PageTitle"] = "Archive blocks";
        if (BatchId is > 0)
        {
            Session.BatchID = BatchId;
            // Only the BatchesForArchiving grid link reaches here with no ReturnPage — ArchiveMenu
            // always supplies its own resolved back link, so this fallback never overrides it.
            Session.ReturnPage = string.IsNullOrWhiteSpace(ReturnPage) ? "/Batches/BatchesForArchiving" : ReturnPage;
        }
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        ViewData["Title"] = "Archive blocks";
        ViewData["PageTitle"] = "Archive blocks";
        await LoadAsync();

        if (IsViewMode) return RedirectToPage(new { batchId = Session.BatchID });
        if (SelectedIds.Count == 0) { Error = "Select at least one block to update."; return Page(); }
        if (string.IsNullOrWhiteSpace(ArchiveLocationCode) && ArchivedDate is null && string.IsNullOrWhiteSpace(Comment))
        {
            Error = "Enter an Archive Location, Archived Date, or Comment to apply.";
            return Page();
        }

        if (ArchivedDate is not null)
        {
            var earliest = Batch?.ReceivedDate?.Date;
            if (earliest is not null && ArchivedDate.Value.Date < earliest)
            {
                Error = $"Archive date must be the same or later than the Submission received date of {earliest:d}.";
                return Page();
            }
            if (ArchivedDate.Value.Date > DateTime.Today)
            {
                Error = "Archive date must be today or earlier.";
                return Page();
            }
        }

        var all = await _blocks.GetByBatchAsync(Session.BatchID ?? 0);
        var selected = all.Where(b => SelectedIds.Contains(b.ID)).ToList();
        var bulkMode = selected.Count >= 2;

        foreach (var block in selected)
        {
            var updated = new Block
            {
                ID = block.ID,
                BatchID = block.BatchID,
                AnimalID = block.AnimalID,
                BlockRef = block.BlockRef,
                CustomerRef = block.CustomerRef,
                RepeatBlock = block.RepeatBlock,
                Comment = block.Comment,
                Status = block.Status,
                Order = block.Order,
                ArchiveLocation = !bulkMode || !string.IsNullOrWhiteSpace(ArchiveLocationCode) ? ArchiveLocationCode : block.ArchiveLocation,
                ArchivedDate = !bulkMode || ArchivedDate is not null ? ArchivedDate : block.ArchivedDate,
                ArchiveComment = !bulkMode || !string.IsNullOrWhiteSpace(Comment) ? Comment : block.ArchiveComment,
            };
            await _blocks.UpdateBlockAsync(updated, Session.UserID);
        }

        SuccessMessage = $"Archive information updated for {selected.Count} block(s).";
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        ArchiveLocations = await _lookups.GetLookupDataAsync(LookupArchiveLocation);
        var archiveLocationNameByCode = ArchiveLocations.ToDictionary(l => l.Code ?? string.Empty, l => l.Name, StringComparer.OrdinalIgnoreCase);
        if (Session.BatchID is not > 0) { Rows = []; return; }

        var batchId = Session.BatchID.Value;
        Batch = await _batches.GetByIdAsync(batchId);
        if (Batch is not null)
        {
            var summary = await BatchSummaryDisplayResolver.ResolveAsync(Batch, _lookups, _users);
            ProjectName       = summary.ProjectName;
            PathologistName   = summary.PathologistName;
            SpeciesName       = summary.SpeciesName;
            EnteredByName     = summary.EnteredByName;
            EnteredAreaName   = summary.EnteredAreaName;
            SubmittedByName   = summary.SubmittedByName;
            SubmittedAreaName = summary.SubmittedAreaName;
        }

        var blocks = await _blocks.GetByBatchAsync(batchId);
        var animals = await _submissions.GetBlockAnimalsByBatchAsync(batchId);
        var animalsById = animals.ToDictionary(a => a.ID);

        Rows = blocks.Select(b =>
        {
            animalsById.TryGetValue(b.AnimalID, out var animal);
            archiveLocationNameByCode.TryGetValue(b.ArchiveLocation ?? string.Empty, out var locationName);
            return new ArchiveBlockRow
            {
                ID = b.ID,
                SenderRef = animal?.SenderRef ?? string.Empty,
                HistologyRef = animal?.HistologyRef,
                BlockRef = b.BlockRef,
                ArchivedDate = b.ArchivedDate,
                ArchiveLocation = b.ArchiveLocation,
                ArchiveLocationName = locationName ?? b.ArchiveLocation,
                ArchiveComment = b.ArchiveComment,
            };
        }).ToList();

        PopulateGridViewData(TotalCount);
    }
}
