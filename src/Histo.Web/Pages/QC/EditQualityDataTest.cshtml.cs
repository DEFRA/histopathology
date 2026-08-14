using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.QualityControl.Interfaces;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.QC;

/// <summary>
/// Edits a single test's result, QC, dispatch and archive data — replaces the
/// per-row editing portion of <c>QualityData.aspx</c> (<c>btnEdit_Click</c> /
/// <c>btnSave_Click</c>, single-selection case).
///
/// SIMPLIFIED: validation is reduced to the fields that matter for data integrity
/// (number of slides, QC code required on failure, dispatch/archive date pairing).
/// The legacy page's full mandatory-field matrix (always-required Dispatched By /
/// Dispatched To / Remedial Action, submission-received-date bounds checking on
/// dispatch/archive dates) is not reproduced. Premium-charge ("TC code") checkboxes
/// are not ported. See <see cref="Histo.Histology.Models.BlockTest"/> for further notes.
/// </summary>
public class EditQualityDataTestModel : HistoPageModel
{
    private readonly IBlockTestService _tests;
    private readonly IQCNoteService _qc;
    private readonly ILookupService _lookups;
    private readonly IUserService _users;

    private const int LookupQcCode = 14;
    private const int LookupRemedialAction = 15;
    private const int LookupArchiveLocation = 16;

    public EditQualityDataTestModel(
        ISessionService session,
        IBlockTestService tests,
        IQCNoteService qc,
        ILookupService lookups,
        IUserService users)
        : base(session)
    {
        _tests = tests;
        _qc = qc;
        _lookups = lookups;
        _users = users;
    }

    [BindProperty(SupportsGet = true)] public int TestId { get; set; }

    [BindProperty] public string? Result { get; set; }
    [BindProperty] public string? StainRef { get; set; }
    [BindProperty] public bool Dispatched { get; set; }
    [BindProperty] public DateTime? DispatchedDate { get; set; }
    [BindProperty] public string? DispatchedBy { get; set; }
    [BindProperty] public string? DispatchedTo { get; set; }
    [BindProperty] public string? QCCode { get; set; }
    [BindProperty] public bool QCNote { get; set; }
    [BindProperty] public string? RemedialAction { get; set; }
    [BindProperty] public string? ArchiveLocation { get; set; }
    [BindProperty] public DateTime? ArchivedDate { get; set; }
    [BindProperty] public string? ArchiveComment { get; set; }
    [BindProperty] public int? NumberOfSlides { get; set; }
    [BindProperty] public string? Comment { get; set; }

    public BlockTest? Test { get; private set; }
    public IReadOnlyList<LookupItem> QCCodes { get; private set; } = [];
    public IReadOnlyList<LookupItem> RemedialActions { get; private set; } = [];
    public IReadOnlyList<LookupItem> ArchiveLocations { get; private set; } = [];
    public IReadOnlyList<User> Users { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SetTitle();
        if (!Session.BatchID.HasValue) return RedirectToPage("/Index");

        Test = await _tests.GetByIdAsync(Session.BatchID.Value, TestId);
        if (Test is null) return RedirectToPage("/QC/QualityData");

        Result = Test.Result;
        StainRef = Test.StainRef;
        Dispatched = Test.Dispatched;
        DispatchedDate = Test.DispatchedDate;
        DispatchedBy = Test.DispatchedBy;
        DispatchedTo = Test.DispatchedTo;
        QCCode = Test.QCCode;
        QCNote = Test.QCNote;
        RemedialAction = Test.RemedialAction;
        ArchiveLocation = Test.ArchiveLocation;
        ArchivedDate = Test.ArchivedDate;
        ArchiveComment = Test.ArchiveComment;
        NumberOfSlides = Test.NumberOfSlides;
        Comment = Test.Comment;

        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetTitle();
        if (!Session.BatchID.HasValue) return RedirectToPage("/Index");

        Test = await _tests.GetByIdAsync(Session.BatchID.Value, TestId);
        if (Test?.RowStamp is null) return RedirectToPage("/QC/QualityData");

        if (Result == Histo.Histology.Models.BlockTestResult.Failed && string.IsNullOrWhiteSpace(QCCode))
        {
            Error = "Enter a QC code when the test result is Failed.";
            await LoadLookupsAsync();
            return Page();
        }

        if (Dispatched && DispatchedDate is null)
        {
            Error = "Enter a dispatched date.";
            await LoadLookupsAsync();
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(ArchiveLocation) && ArchivedDate is null)
        {
            Error = "Enter an archive date.";
            await LoadLookupsAsync();
            return Page();
        }

        // Create a QC note the first time the box is ticked for this test.
        var qcNoteRef = Test.QCNoteRef;
        if (QCNote && qcNoteRef is null or 0)
        {
            var newId = await _qc.AddAsync(Session.BatchID.Value, Session.UserID);
            if (newId > 0) qcNoteRef = newId;
        }
        else if (!QCNote)
        {
            qcNoteRef = null;
        }

        var updated = new BlockTest
        {
            ID = Test.ID,
            BlockID = Test.BlockID,
            BlockRef = Test.BlockRef,
            HistologyRef = Test.HistologyRef,
            TestType = Test.TestType,
            Code = Test.Code,
            TestDetails = Test.TestDetails,
            Result = Result,
            QCCode = QCCode,
            QCNote = QCNote,
            QCNoteRef = qcNoteRef,
            StainRef = StainRef,
            Dispatched = Dispatched,
            DispatchedDate = DispatchedDate,
            DispatchedBy = DispatchedBy,
            DispatchedTo = DispatchedTo,
            Comment = Comment,
            RemedialAction = RemedialAction,
            ArchiveLocation = ArchiveLocation,
            ArchivedDate = ArchivedDate,
            ArchiveComment = ArchiveComment,
            NumberOfSlides = NumberOfSlides,
            OnHold = Test.OnHold,
            Archived = !string.IsNullOrWhiteSpace(ArchiveLocation) && ArchivedDate is not null,
            RowStamp = Test.RowStamp,
        };

        try
        {
            await _tests.UpdateAsync(updated, Session.UserID);
            return RedirectToPage("/QC/QualityData");
        }
        catch (BlockTestConcurrencyException)
        {
            Error = "Another user has modified this test. Please reload and try again.";
            await LoadLookupsAsync();
            return Page();
        }
    }

    private void SetTitle()
    {
        ViewData["Title"] = "Edit Quality Data";
        ViewData["PageTitle"] = "Edit Quality Data";
    }

    private async Task LoadLookupsAsync()
    {
        QCCodes = await _lookups.GetLookupDataAsync(LookupQcCode);
        RemedialActions = await _lookups.GetLookupDataAsync(LookupRemedialAction);
        ArchiveLocations = await _lookups.GetLookupDataAsync(LookupArchiveLocation);
        Users = await _users.GetAllUsersAsync();
    }
}
