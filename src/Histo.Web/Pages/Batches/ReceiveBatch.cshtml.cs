using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Histology.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.Batches;

/// <summary>
/// Replaces <c>ReceiveBatch.aspx</c> — records receipt (or rejection) of a submitted batch.
///
/// Legacy behaviour preserved:
/// <list type="bullet">
/// <item>Submission status can be set to Received or Rejected (the only two transitions
///   available from this page — other statuses are set via <c>EditBatch</c>).</item>
/// <item>Received/Rejected requires Date received, Time received and Received/Rejected by.</item>
/// <item>Rejected additionally requires a Reason.</item>
/// <item>Date received must be on or after the submission date and on or before today.</item>
/// <item>Post fixation methods are recorded as a checkbox list, with a free-text "Other" detail.</item>
/// <item>Whether the batch contains repeat blocks is shown read-only (computed from block data).</item>
/// <item>Once a batch has any status other than Submitted, the page is read-only — this replaces
///   the legacy <c>SessionVars.SV_ViewSubmission</c> flag, reached here via
///   <c>SearchSubmissions</c>' "View receipt" action.</item>
/// </list>
/// </summary>
public class ReceiveBatchModel : HistoPageModel
{
    private const int LookupTimeReceived = 3;  // Legacy source: HistopathologySystem/Common.vb — LOOKUP_TIME_RECEIVED
    private const int LookupPostFixation = 12; // Legacy source: HistopathologySystem/Common.vb — LOOKUP_POSTFIXATION
    private const int LookupContacts     = 18; // Legacy source: HistopathologySystem/Common.vb — LOOKUP_CONTACTS
    private const int LookupProjects     = 19; // Legacy source: HistopathologySystem/Common.vb — LOOKUP_PROJECTS

    /// <summary>Synthetic post-fixation code for the free-text "Other" option (not a real lookup row).</summary>
    private const string PostFixationOtherCode = "Other";

    private readonly IBatchService  _batches;
    private readonly IBlockService  _blocks;
    private readonly ILookupService _lookups;
    private readonly IUserService   _users;

    public ReceiveBatchModel(ISessionService session, IBatchService batches, IBlockService blocks,
        ILookupService lookups, IUserService users)
        : base(session)
    {
        _batches = batches;
        _blocks  = blocks;
        _lookups = lookups;
        _users   = users;
    }

    public Batch? Batch { get; private set; }
    public string? Error { get; private set; }

    /// <summary>True once the batch has moved past Submitted — the page becomes read-only.</summary>
    public bool IsReadOnly => Batch?.Status is not null && Batch.Status != BatchStatus.Submitted;

    public string? ProjectName { get; private set; }
    public string? PathologistName { get; private set; }
    public string? EnteredByName { get; private set; }
    public string? EnteredAreaName { get; private set; }
    public string? SubmittedByName { get; private set; }
    public string? SubmittedAreaName { get; private set; }
    public string? SpeciesName { get; private set; }

    /// <summary> True when any block on this batch is flagged as a repeat block (read-only indicator).</summary>
    public bool HasRepeatBlocks { get; private set; }

    public IReadOnlyList<User> Users { get; private set; } = [];
    public IReadOnlyList<LookupItem> TimeReceivedOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> PostFixationOptions { get; private set; } = [];

    /// <summary>Page path for the back link / cancel target — set by the list page before navigating here.</summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Batches/BatchesNotReceived"
        : Session.ReturnPage;

    // ── Bind properties (the receive/reject form) ──────────────────────────

    [BindProperty] public string Status { get; set; } = BatchStatus.Submitted;
    [BindProperty] public DateTime? DateReceived { get; set; }
    [BindProperty] public string? TimeReceived { get; set; }
    [BindProperty] public int? ReceivedByUserId { get; set; }
    [BindProperty] public List<string> SelectedPostFixationCodes { get; set; } = [];
    [BindProperty] public string? PostFixationOther { get; set; }
    [BindProperty] public string? Reason { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SetTitle();
        if (Session.BatchID is null or <= 0) return RedirectToPage("/Batches/BatchesNotReceived");

        Batch = await _batches.GetByIdAsync(Session.BatchID.Value);
        if (Batch is null) return RedirectToPage("/Batches/BatchesNotReceived");

        Session.BatchType = Batch.BatchType;
        await LoadLookupsAsync();

        Status            = Batch.Status;
        DateReceived      = Batch.ReceivedDate;
        TimeReceived      = Batch.TimeReceived;
        ReceivedByUserId  = Batch.ReceivedBy;
        PostFixationOther = Batch.PostFixationOther;
        Reason            = Batch.StatusComments;

        SelectedPostFixationCodes = (await _batches.GetPostFixationCodesAsync(Batch.ID)).ToList();
        if (!string.IsNullOrWhiteSpace(PostFixationOther) && !SelectedPostFixationCodes.Contains(PostFixationOtherCode))
            SelectedPostFixationCodes.Add(PostFixationOtherCode);

        HasRepeatBlocks = (await _blocks.GetByBatchAsync(Batch.ID)).Any(b => b.RepeatBlock);

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        SetTitle();
        Batch = await _batches.GetByIdAsync(Session.BatchID ?? 0);
        if (Batch?.RowStamp is null)
        {
            Error = "Submission not found.";
            return Page();
        }

        if (Batch.Status != BatchStatus.Submitted)
            return RedirectToPage();

        await LoadLookupsAsync();
        HasRepeatBlocks = (await _blocks.GetByBatchAsync(Batch.ID)).Any(b => b.RepeatBlock);

        if (!ValidateData())
            return Page();

        // Post-fixation "Other" free text only persists when "Other" is ticked.
        var otherText = SelectedPostFixationCodes.Contains(PostFixationOtherCode) ? PostFixationOther : null;

        // Full field carry-forward (mirrors legacy ReceiveBatch.aspx → UpdateBatchDetails → EditBatch SP)
        // so EditBatch's exhaustive parameter set doesn't null out fields this page doesn't edit.
        var updated = new Batch
        {
            ID                  = Batch.ID,
            Status              = Status,
            CustomerRef         = Batch.CustomerRef,
            Comments            = Batch.Comments,
            StatusComments      = Reason,
            BatchDate           = Batch.BatchDate,
            ReceivedDate        = DateReceived,
            TimeReceived        = TimeReceived,
            ReceivedBy          = ReceivedByUserId,
            PostFixationOther   = otherText,
            CompletedDate       = Batch.CompletedDate,
            SubmittedByUserID   = Batch.SubmittedByUserID,
            UserAreaCode        = Batch.UserAreaCode,
            IsPreCassetted      = Batch.IsPreCassetted,
            ByPassSort          = Batch.ByPassSort,
            RowStamp            = Batch.RowStamp,
            BatchType           = Batch.BatchType,
            ProjectContractCode = Batch.ProjectContractCode,
            ContactName         = Batch.ContactName,
            Species             = Batch.Species,
            Fixation            = Batch.Fixation,
            CustomerReceivedDate = Batch.CustomerReceivedDate,
            SubmittedBy         = Batch.SubmittedBy,
            SubmittedArea       = Batch.SubmittedArea,
            OtherSubmittedBy    = Batch.OtherSubmittedBy,
            OtherSubmittedArea  = Batch.OtherSubmittedArea ?? "",
            SafeToHandle        = Batch.SafeToHandle,
            IsBlocked           = Batch.IsBlocked,
            SampleSameProjects  = Batch.SampleSameProjects,
            AllTissuesAssigned  = Batch.AllTissuesAssigned,
        };

        if (!await _batches.UpdateAsync(updated, Session.UserID))
        {
            Error = "Could not save the receipt details. It may have been modified by another user.";
            return Page();
        }

        if (!await _batches.SavePostFixationCodesAsync(Batch.ID, SelectedPostFixationCodes, Session.UserID))
        {
            Error = "Could not save the post-fixation selections. Please try again.";
            return Page();
        }

        return RedirectToPage("/Batches/BatchesNotReceived");
    }

    public IActionResult OnPostCancel() => RedirectToPage(BackLinkPage);

    // ── Private helpers ─────────────────────────────────────────────────────

    private void SetTitle()
    {
        ViewData["Title"] = "Receive submission";
        ViewData["PageTitle"] = "Receive submission";
    }

    /// <summary>
    /// Validation mirrors <c>ReceiveBatch.aspx.vb::ValidateData</c>:
    /// Received/Rejected require Date received, Time received and Received/Rejected by.
    /// Rejected additionally requires a Reason. Date received must fall between the
    /// submission date and today (inclusive).
    /// </summary>
    private bool ValidateData()
    {
        if (Status is BatchStatus.Received or BatchStatus.Rejected)
        {
            if (ReceivedByUserId is null or <= 0)
            {
                Error = "Select who received or rejected the submission.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(TimeReceived))
            {
                Error = "Select the time the submission was received or rejected.";
                return false;
            }

            if (DateReceived is null)
            {
                Error = "Enter the date the submission was received or rejected.";
                return false;
            }

            if (Batch?.BatchDate is { } submitted && DateReceived < submitted.Date)
            {
                Error = $"The date received must be the same as or later than the submission date of {submitted:d}.";
                return false;
            }

            if (DateReceived > DateTime.Today)
            {
                Error = "The date received must be today or earlier.";
                return false;
            }

            if (Status == BatchStatus.Rejected && string.IsNullOrWhiteSpace(Reason))
            {
                Error = "Enter a reason for rejecting the submission.";
                return false;
            }
        }

        return true;
    }

    private async Task LoadLookupsAsync()
    {
        if (Batch is null) return;

        var usersTask        = _users.GetAllUsersAsync();
        var timeReceivedTask = _lookups.GetLookupDataAsync(LookupTimeReceived);
        var postFixationTask = _lookups.GetLookupDataAsync(LookupPostFixation);
        var projectsTask     = _lookups.GetLookupDataAsync(LookupProjects);
        var contactsTask     = _lookups.GetLookupDataAsync(LookupContacts);
        var speciesTask      = _lookups.GetSpeciesLookupAsync();
        var userAreasTask    = _lookups.GetUserAreasAsync();

        await Task.WhenAll(usersTask, timeReceivedTask, postFixationTask,
            projectsTask, contactsTask, speciesTask, userAreasTask);

        Users               = usersTask.Result;
        TimeReceivedOptions = timeReceivedTask.Result;
        PostFixationOptions = postFixationTask.Result;

        var projectsById = projectsTask.Result.ToDictionary(i => i.ID.ToString(), i => i.Name);
        var contactsById = contactsTask.Result.ToDictionary(i => i.ID.ToString(), i => i.Name);
        var speciesById  = speciesTask.Result.ToDictionary(i => i.ID.ToString(), i => i.Name, StringComparer.OrdinalIgnoreCase);
        var userById     = usersTask.Result.ToDictionary(u => u.UserID, u => u.Name);
        var areaByCode   = userAreasTask.Result.ToDictionary(a => a.ID.ToString(), a => a.Name, StringComparer.OrdinalIgnoreCase);

        ProjectName       = !string.IsNullOrWhiteSpace(Batch.ProjectContractCode) && projectsById.TryGetValue(Batch.ProjectContractCode, out var pn) ? pn : Batch.ProjectContractCode;
        PathologistName   = !string.IsNullOrWhiteSpace(Batch.ContactName) && contactsById.TryGetValue(Batch.ContactName, out var cn) ? cn : Batch.ContactName;
        SpeciesName       = !string.IsNullOrWhiteSpace(Batch.Species) && speciesById.TryGetValue(Batch.Species, out var sn) ? sn : Batch.Species;
        EnteredByName     = Batch.SubmittedBy.HasValue && userById.TryGetValue(Batch.SubmittedBy.Value, out var eb) ? eb : null;
        SubmittedByName   = Batch.OtherSubmittedBy.HasValue && userById.TryGetValue(Batch.OtherSubmittedBy.Value, out var sb) ? sb : null;
        EnteredAreaName   = !string.IsNullOrEmpty(Batch.SubmittedArea) && areaByCode.TryGetValue(Batch.SubmittedArea, out var ea) ? ea : null;
        SubmittedAreaName = !string.IsNullOrEmpty(Batch.OtherSubmittedArea) && areaByCode.TryGetValue(Batch.OtherSubmittedArea, out var sa) ? sa : null;
    }
}
