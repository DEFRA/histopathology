using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Archive;

/// <summary>State and logic shared by <see cref="ArchiveBlocksModel"/> and <see cref="ArchiveTissuesModel"/>.</summary>
public abstract class ArchivePageModelBase : GridPageModel, IArchivePage
{
    protected const int LookupArchiveLocation = 16;

    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;
    private readonly IUserService _users;

    protected ArchivePageModelBase(ISessionService session, IBatchService batches, ILookupService lookups, IUserService users)
        : base(session) { _batches = batches; _lookups = lookups; _users = users; }

    [BindProperty(SupportsGet = true)]
    public int? BatchId { get; set; }

    protected override int? RequestedBatchId => BatchId is > 0 ? BatchId : Session.BatchID;

    /// <summary>
    /// Origin page to return to once archiving is done. Set explicitly by <c>ArchiveMenu</c>, which
    /// itself forwards whatever navigated to it (BatchesForArchiving or SearchSubmissions) — without
    /// this, <c>OnGetAsync</c> could only guess "BatchesForArchiving" for every entry route,
    /// even when reached via SearchSubmissions' "View archive" link.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnPage { get; set; }

    /// <summary>
    /// Back target — honours <see cref="ISessionService.ReturnPage"/> (set on entry to this page)
    /// so a direct grid link from BatchesForArchiving skips the redundant ArchiveMenu detour.
    /// Falls back to ArchiveMenu for the menu/SearchSubmissions "View archive" entry points.
    /// </summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Archive/ArchiveMenu"
        : Session.ReturnPage;

    public Batch? Batch { get; private set; }
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

    [BindProperty] public List<int> SelectedIds { get; set; } = [];
    /// <summary>"All" checkbox — selects every filtered row, not just the current page's.</summary>
    [BindProperty] public bool SelectAllAcrossPages { get; set; }
    [BindProperty] public string? ArchiveLocationCode { get; set; }
    [BindProperty] public DateTime? ArchivedDate { get; set; }
    [BindProperty] public string? Comment { get; set; }

    public string? Error { get; protected set; }
    public string? SuccessMessage { get; protected set; }

    protected void SetTitle(string title)
    {
        ViewData["Title"] = title;
        ViewData["PageTitle"] = title;
    }

    protected void RememberBatchContext()
    {
        if (BatchId is not > 0) return;
        Session.BatchID = BatchId;
        // Only the BatchesForArchiving grid link reaches here with no ReturnPage — ArchiveMenu
        // always supplies its own resolved back link, so this fallback never overrides it.
        Session.ReturnPage = string.IsNullOrWhiteSpace(ReturnPage) ? "/Batches/BatchesForArchiving" : ReturnPage;
    }

    protected string? ValidateArchiveUpdate()
    {
        // Legacy requires both: rfvArchiveLocation, and a blank date shows the red star and skips the update.
        if (string.IsNullOrWhiteSpace(ArchiveLocationCode) || ArchivedDate is null)
            return "Select an archive location and enter an archived date.";

        var earliest = Batch?.ReceivedDate?.Date;
        if (earliest is not null && ArchivedDate.Value.Date < earliest)
            return $"Archive date must be the same or later than the Submission received date of {earliest:d}.";

        return ArchivedDate.Value.Date > DateTime.Today ? "Archive date must be today or earlier." : null;
    }

    protected string? MergedArchiveLocation(bool bulkMode, string? existing) =>
        !bulkMode || !string.IsNullOrWhiteSpace(ArchiveLocationCode) ? ArchiveLocationCode : existing;

    protected DateTime? MergedArchivedDate(bool bulkMode, DateTime? existing) =>
        !bulkMode || ArchivedDate is not null ? ArchivedDate : existing;

    protected string? MergedArchiveComment(bool bulkMode, string? existing) =>
        !bulkMode || !string.IsNullOrWhiteSpace(Comment) ? Comment : existing;

    /// <summary>A stray DateTime.MinValue (0001-01-01) means "no date" — never a real archive date.</summary>
    protected static DateTime? NormalizeArchivedDate(DateTime? value) => value is null or { Year: 1 } ? null : value;

    /// <summary>
    /// Loads the archive locations and, when a batch is selected, the batch with its resolved summary names.
    /// Returns the location-name lookup by code, or <c>null</c> when no batch is selected.
    /// </summary>
    protected async Task<Dictionary<string, string>?> LoadBatchContextAsync()
    {
        ArchiveLocations = await _lookups.GetLookupDataAsync(LookupArchiveLocation);
        var archiveLocationNameByCode = ArchiveLocations.ToDictionary(l => l.Code ?? string.Empty, l => l.Name, StringComparer.OrdinalIgnoreCase);
        if (Session.BatchID is not > 0) return null;

        Batch = await _batches.GetByIdAsync(Session.BatchID.Value);
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

        return archiveLocationNameByCode;
    }
}
