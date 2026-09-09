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

    public Batch? Batch { get; private set; }
    public IReadOnlyList<ArchiveBlockRow> Rows { get; private set; } = [];
    public IReadOnlyList<LookupItem> ArchiveLocations { get; private set; } = [];
    public bool IsViewMode => Session.IsViewSubmissionMode;

    // Resolved display names for the batch summary header (shared with QualityData).
    public string? ProjectName { get; private set; }
    public string? PathologistName { get; private set; }
    public string? SpeciesName { get; private set; }
    public string? EnteredByName { get; private set; }
    public string? EnteredAreaName { get; private set; }
    public string? SubmittedByName { get; private set; }
    public string? SubmittedAreaName { get; private set; }

    public int TotalCount => Rows.Count;

    public IReadOnlyList<ArchiveBlockRow> PagedEntries =>
        (SortColumn switch
        {
            "SenderRef"       => SortDesc ? Rows.OrderByDescending(r => r.SenderRef)       : Rows.OrderBy(r => r.SenderRef),
            "HistologyRef"    => SortDesc ? Rows.OrderByDescending(r => r.HistologyRef)    : Rows.OrderBy(r => r.HistologyRef),
            "BlockRef"        => SortDesc ? Rows.OrderByDescending(r => r.BlockRef)        : Rows.OrderBy(r => r.BlockRef),
            "ArchivedDate"    => SortDesc ? Rows.OrderByDescending(r => r.ArchivedDate)    : Rows.OrderBy(r => r.ArchivedDate),
            "ArchiveLocation" => SortDesc ? Rows.OrderByDescending(r => r.ArchiveLocationName) : Rows.OrderBy(r => r.ArchiveLocationName),
            _                 => SortDesc ? Rows.OrderByDescending(r => r.BlockRef)        : Rows.OrderBy(r => r.BlockRef),
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
            Session.ReturnPage = "/Batches/BatchesForArchiving";
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
