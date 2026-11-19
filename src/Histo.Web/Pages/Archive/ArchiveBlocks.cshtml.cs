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
/// existing values be edited; selecting two or more applies the entered Location/Date to every
/// selected row, with a blank Comment left unchanged. Location and Date are always required.
///
/// Deviation from legacy (consistent with this app's established immediate-write architecture):
/// "Update selected" persists immediately rather than staging in a session DataSet until "Done".
/// </summary>
public class ArchiveBlocksModel : ArchivePageModelBase
{
    private const string ArchiveBlocksTitle = "Archive blocks";

    private readonly IBlockService _blocks;
    private readonly ISubmissionService _submissions;

    public ArchiveBlocksModel(ISessionService session, IBlockService blocks, IBatchService batches, ISubmissionService submissions, ILookupService lookups, IUserService users)
        : base(session, batches, lookups, users) { _blocks = blocks; _submissions = submissions; }

    public IReadOnlyList<ArchiveBlockRow> Rows { get; private set; } = [];

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

    public int TotalCount => FilteredRows.Count;

    public IReadOnlyList<ArchiveBlockRow> GetPagedEntries() =>
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

    public async Task OnGetAsync()
    {
        SetTitle(ArchiveBlocksTitle);
        RememberBatchContext();
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        SetTitle(ArchiveBlocksTitle);
        await LoadAsync();

        if (IsViewMode) return RedirectToPage(new { batchId = Session.BatchID });

        // "All" only ever reaches the server as a flag — off-page rows' checkboxes never render,
        // so SelectedIds alone can't carry them across a page boundary.
        var selectedIds = SelectAllAcrossPages ? FilteredRows.Select(r => r.ID).ToList() : SelectedIds;
        if (selectedIds.Count == 0) { Error = "Select at least one block to update."; return Page(); }
        var validationError = ValidateArchiveUpdate();
        if (validationError is not null)
        {
            Error = validationError;
            return Page();
        }

        var all = await _blocks.GetByBatchAsync(Session.BatchID ?? 0);
        var selected = all.Where(b => selectedIds.Contains(b.ID)).ToList();
        var bulkMode = selected.Count >= 2;

        foreach (var block in selected)
            await _blocks.UpdateBlockAsync(BuildUpdatedBlock(block, bulkMode), Session.UserID);

        SuccessMessage = $"Archive information updated for {selected.Count} block(s).";
        await LoadAsync();
        return Page();
    }

    private Block BuildUpdatedBlock(Block block, bool bulkMode) => new()
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
        ArchiveLocation = MergedArchiveLocation(bulkMode, block.ArchiveLocation),
        ArchivedDate = MergedArchivedDate(bulkMode, block.ArchivedDate),
        ArchiveComment = MergedArchiveComment(bulkMode, block.ArchiveComment),
    };

    private async Task LoadAsync()
    {
        var archiveLocationNameByCode = await LoadBatchContextAsync();
        if (archiveLocationNameByCode is null) { Rows = []; return; }

        var batchId = Session.BatchID!.Value;
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
                ArchivedDate = NormalizeArchivedDate(b.ArchivedDate),
                ArchiveLocation = b.ArchiveLocation,
                ArchiveLocationName = locationName ?? b.ArchiveLocation,
                ArchiveComment = b.ArchiveComment,
            };
        }).ToList();

        PopulateGridViewData(TotalCount);
    }
}
