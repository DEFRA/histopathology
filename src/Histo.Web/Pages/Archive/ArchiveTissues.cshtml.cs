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
    public string? ArchiveLocationName { get; init; }
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
public class ArchiveTissuesModel : ArchivePageModelBase
{
    private const string ArchiveTissuesTitle = "Archive tissues";

    private const int LookupTissueCode = 9;

    private readonly ISubmissionService _submissions;
    private readonly ILookupService _lookups;

    public ArchiveTissuesModel(ISessionService session, ISubmissionService submissions, IBatchService batches, ILookupService lookups, IUserService users)
        : base(session, batches, lookups, users) { _submissions = submissions; _lookups = lookups; }

    public IReadOnlyList<ArchiveTissueRow> Rows { get; private set; } = [];

    /// <summary>
    /// Legacy <c>ddlHistologyRefList</c> / <c>ddlTissueList</c> — populated from the batch's own
    /// rows and applied in memory, matching legacy's <c>DataView.RowFilter</c> rather than
    /// re-querying. Mirrors <see cref="ArchiveBlocksModel"/>'s HistologyRef/BlockRef filter.
    /// </summary>
    [BindProperty(SupportsGet = true)] public string? FilterHistologyRef { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterTissue { get; set; }

    public IReadOnlyList<string> HistologyRefOptions =>
        [.. Rows.Select(r => r.HistologyRef).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).Distinct().Order()];

    public IReadOnlyList<string> TissueOptions =>
        [.. Rows.Select(r => r.TissueDescription).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().Order()];

    public bool FilterApplied => !string.IsNullOrWhiteSpace(FilterHistologyRef) || !string.IsNullOrWhiteSpace(FilterTissue);

    public IReadOnlyList<ArchiveTissueRow> FilteredRows =>
        [.. Rows.Where(r =>
            (string.IsNullOrWhiteSpace(FilterHistologyRef) || string.Equals(r.HistologyRef, FilterHistologyRef, StringComparison.OrdinalIgnoreCase))
         && (string.IsNullOrWhiteSpace(FilterTissue) || string.Equals(r.TissueDescription, FilterTissue, StringComparison.OrdinalIgnoreCase)))];

    public int TotalCount => FilteredRows.Count;

    public IReadOnlyList<ArchiveTissueRow> GetPagedEntries() =>
        (SortColumn switch
        {
            "HistologyRef"    => SortDesc ? FilteredRows.OrderByDescending(r => r.HistologyRef)    : FilteredRows.OrderBy(r => r.HistologyRef),
            "Tissue"          => SortDesc ? FilteredRows.OrderByDescending(r => r.TissueDescription) : FilteredRows.OrderBy(r => r.TissueDescription),
            "ArchivedDate"    => SortDesc ? FilteredRows.OrderByDescending(r => r.ArchivedDate)    : FilteredRows.OrderBy(r => r.ArchivedDate),
            "ArchiveLocation" => SortDesc ? FilteredRows.OrderByDescending(r => r.ArchiveLocationName) : FilteredRows.OrderBy(r => r.ArchiveLocationName),
            _                 => SortDesc ? FilteredRows.OrderByDescending(r => r.SenderRef)       : FilteredRows.OrderBy(r => r.SenderRef),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public async Task OnGetAsync()
    {
        SetTitle(ArchiveTissuesTitle);
        RememberBatchContext();
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        SetTitle(ArchiveTissuesTitle);
        await LoadAsync();

        if (IsViewMode) return RedirectToPage(new { batchId = Session.BatchID });

        // "All" only ever reaches the server as a flag — off-page rows' checkboxes never render,
        // so SelectedIds alone can't carry them across a page boundary.
        var selectedIds = SelectAllAcrossPages ? FilteredRows.Select(r => r.ID).ToList() : SelectedIds;
        if (selectedIds.Count == 0) { Error = "Select at least one tissue to update."; return Page(); }
        var validationError = ValidateArchiveUpdate();
        if (validationError is not null)
        {
            Error = validationError;
            return Page();
        }

        var all = await _submissions.GetBatchSubmissionTissuesAsync(Session.BatchID ?? 0);
        var selected = all.Where(t => selectedIds.Contains(t.ID)).ToList();
        var bulkMode = selected.Count >= 2;

        foreach (var tissue in selected)
            await _submissions.UpdateTissueAsync(BuildUpdatedTissue(tissue, bulkMode), Session.UserID);

        SuccessMessage = $"Archive information updated for {selected.Count} tissue(s).";
        await LoadAsync();
        return Page();
    }

    private Tissue BuildUpdatedTissue(Tissue tissue, bool bulkMode) => new()
    {
        ID = tissue.ID,
        OwnerID = tissue.OwnerID,
        Owner = tissue.Owner,
        TissueCode = tissue.TissueCode,
        NoPieces = tissue.NoPieces,
        Comment = tissue.Comment,
        RowStamp = tissue.RowStamp,
        ArchiveLocation = MergedArchiveLocation(bulkMode, tissue.ArchiveLocation),
        ArchivedDate = MergedArchivedDate(bulkMode, tissue.ArchivedDate),
        ArchiveComment = MergedArchiveComment(bulkMode, tissue.ArchiveComment),
    };

    private async Task LoadAsync()
    {
        var archiveLocationNameByCode = await LoadBatchContextAsync();
        if (archiveLocationNameByCode is null) { Rows = []; return; }

        var batchId = Session.BatchID!.Value;
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
            archiveLocationNameByCode.TryGetValue(t.ArchiveLocation ?? string.Empty, out var locationName);
            return new ArchiveTissueRow
            {
                ID = t.ID,
                SenderRef = animal?.SenderRef ?? string.Empty,
                HistologyRef = animal?.HistologyRef,
                TissueDescription = tissueDesc ?? t.TissueCode,
                ArchivedDate = NormalizeArchivedDate(t.ArchivedDate),
                ArchiveLocation = t.ArchiveLocation,
                ArchiveLocationName = locationName ?? t.ArchiveLocation,
                ArchiveComment = t.ArchiveComment,
            };
        }).ToList();

        PopulateGridViewData(TotalCount);
    }
}
