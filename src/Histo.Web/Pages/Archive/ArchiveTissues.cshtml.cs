using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Archive;

/// <summary>One archive-grid row — a submission-owned Tissue joined with its Animal's Sender/Histology ref.</summary>
public sealed class ArchiveTissueRow
{
    public int ID { get; init; }
    public string SenderRef { get; init; } = string.Empty;
    public string? HistologyRef { get; init; }
    public string TissueDescription { get; init; } = string.Empty;
    public DateTime? ArchivedDate { get; init; }
    public string? ArchiveLocation { get; init; }
    public string? ArchiveComment { get; init; }
}

/// <summary>
/// Replaces <c>ArchiveTissues.aspx</c> — archives submission-owned (Wet Tissue) tissue records by
/// recording their Archive Location/Archived Date/Comment. Block-owned tissues have no archive
/// columns on their <c>EditBlockTissue</c> stored procedure (confirmed against the database
/// directly) and are archived at the block level instead — see <see cref="ArchiveBlocksModel"/>.
///
/// Simplification vs legacy: legacy's grid shows one row per tissue PIECE (duplicating a row
/// with NoPieces=3 three times); this shows one row per tissue record — the piece count has no
/// bearing on archive location/date, so the duplication added no information.
///
/// Deviation from legacy (consistent with this app's established immediate-write architecture):
/// "Update selected" persists immediately rather than staging in a session DataSet until "Done".
/// </summary>
public class ArchiveTissuesModel : GridPageModel
{
    private const int LookupArchiveLocation = 16;
    private const int LookupTissueCode = 9;

    private readonly ISubmissionService _submissions;
    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;

    public ArchiveTissuesModel(ISessionService session, ISubmissionService submissions, IBatchService batches, ILookupService lookups)
        : base(session) { _submissions = submissions; _batches = batches; _lookups = lookups; }

    [BindProperty(SupportsGet = true)]
    public int? BatchId { get; set; }

    public Batch? Batch { get; private set; }
    public IReadOnlyList<ArchiveTissueRow> Rows { get; private set; } = [];
    public IReadOnlyList<LookupItem> ArchiveLocations { get; private set; } = [];
    public bool IsViewMode => Session.IsViewSubmissionMode;

    public int TotalCount => Rows.Count;

    public IReadOnlyList<ArchiveTissueRow> PagedEntries =>
        (SortColumn switch
        {
            "HistologyRef"    => SortDesc ? Rows.OrderByDescending(r => r.HistologyRef)    : Rows.OrderBy(r => r.HistologyRef),
            "ArchivedDate"    => SortDesc ? Rows.OrderByDescending(r => r.ArchivedDate)    : Rows.OrderBy(r => r.ArchivedDate),
            "ArchiveLocation" => SortDesc ? Rows.OrderByDescending(r => r.ArchiveLocation) : Rows.OrderBy(r => r.ArchiveLocation),
            _                 => SortDesc ? Rows.OrderByDescending(r => r.SenderRef)       : Rows.OrderBy(r => r.SenderRef),
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
        ViewData["Title"] = "Archive tissues";
        ViewData["PageTitle"] = "Archive tissues";
        if (BatchId is > 0)
        {
            Session.BatchID = BatchId;
            Session.ReturnPage = "/Batches/BatchesForArchiving";
        }
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        ViewData["Title"] = "Archive tissues";
        ViewData["PageTitle"] = "Archive tissues";
        await LoadAsync();

        if (IsViewMode) return RedirectToPage(new { batchId = Session.BatchID });
        if (SelectedIds.Count == 0) { Error = "Select at least one tissue to update."; return Page(); }
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

        var all = await _submissions.GetBatchSubmissionTissuesAsync(Session.BatchID ?? 0);
        var selected = all.Where(t => SelectedIds.Contains(t.ID)).ToList();
        var bulkMode = selected.Count >= 2;

        foreach (var tissue in selected)
        {
            var updated = new Tissue
            {
                ID = tissue.ID,
                OwnerID = tissue.OwnerID,
                Owner = tissue.Owner,
                TissueCode = tissue.TissueCode,
                NoPieces = tissue.NoPieces,
                Comment = tissue.Comment,
                RowStamp = tissue.RowStamp,
                ArchiveLocation = !bulkMode || !string.IsNullOrWhiteSpace(ArchiveLocationCode) ? ArchiveLocationCode : tissue.ArchiveLocation,
                ArchivedDate = !bulkMode || ArchivedDate is not null ? ArchivedDate : tissue.ArchivedDate,
                ArchiveComment = !bulkMode || !string.IsNullOrWhiteSpace(Comment) ? Comment : tissue.ArchiveComment,
            };
            await _submissions.UpdateTissueAsync(updated, Session.UserID);
        }

        SuccessMessage = $"Archive information updated for {selected.Count} tissue(s).";
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        ArchiveLocations = await _lookups.GetLookupDataAsync(LookupArchiveLocation);
        if (Session.BatchID is not > 0) { Rows = []; return; }

        var batchId = Session.BatchID.Value;
        Batch = await _batches.GetByIdAsync(batchId);

        var tissues = await _submissions.GetBatchSubmissionTissuesAsync(batchId);
        var submissions = await _submissions.GetSubmissionsByBatchAsync(batchId);
        var animals = await _submissions.GetAnimalsByBatchAsync(batchId);
        var tissueCodes = await _lookups.GetLookupDataAsync(LookupTissueCode);

        var animalIdBySubmission = submissions.ToDictionary(s => s.ID, s => s.AnimalID);
        var animalsById = animals.ToDictionary(a => a.ID);
        var tissueDescByCode = tissueCodes.ToDictionary(t => t.Code ?? string.Empty, t => t.Name);

        Rows = tissues.Select(t =>
        {
            animalIdBySubmission.TryGetValue(t.OwnerID, out var animalId);
            animalsById.TryGetValue(animalId, out var animal);
            tissueDescByCode.TryGetValue(t.TissueCode, out var tissueDesc);
            return new ArchiveTissueRow
            {
                ID = t.ID,
                SenderRef = animal?.SenderRef ?? string.Empty,
                HistologyRef = animal?.HistologyRef,
                TissueDescription = tissueDesc ?? t.TissueCode,
                ArchivedDate = t.ArchivedDate,
                ArchiveLocation = t.ArchiveLocation,
                ArchiveComment = t.ArchiveComment,
            };
        }).ToList();

        PopulateGridViewData(TotalCount);
    }
}
