using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.QualityControl.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Histo.Web.Pages.QC;

/// <summary>
/// Quality-control / dispatch worklist for the current batch — replaces
/// <c>QualityData.aspx</c>. Lists every histology, antibodies and special-stain
/// test on the batch's blocks so results, QC data, dispatch and archive
/// information can be recorded per test.
///
    /// <see cref="OnPostUpdateAsync"/> reproduces legacy <c>btnEdit_Click</c>'s two save modes:
/// selecting exactly one row writes every field as shown (the row's existing values are
/// pre-filled client-side, so clearing a field clears it); selecting two or more rows applies only
/// the fields that were filled in, leaving each row's other values untouched. Editing one test at a
/// time is still available via <see cref="EditQualityDataTestModel"/> for deep-linking, but is no
/// longer required for routine updates.
/// </summary>
public class QualityDataModel : GridPageModel
{
    private readonly IBlockTestService _tests;
    private readonly IBatchService _batches;
    private readonly ILookupService _lookups;
    private readonly IUserService _users;
    private readonly IQCNoteService _qc;

    private const int LookupAntibodiesTse    = 4;
    private const int LookupAntibodiesNonTse = 5;
    private const int LookupSpecialStain     = 6;
    private const int LookupHistologyTse     = 7;
    private const int LookupHistologyNonTse  = 8;
    private const int LookupQcCode           = 14;
    private const int LookupRemedialAction   = 15;
    private const int LookupArchiveLocation  = 16;
    private const int LookupPremiumCharges   = 17;

    public QualityDataModel(
        ISessionService session,
        IBlockTestService tests,
        IBatchService batches,
        ILookupService lookups,
        IUserService users,
        IQCNoteService qc)
        : base(session)
    {
        _tests   = tests;
        _batches = batches;
        _lookups = lookups;
        _users   = users;
        _qc      = qc;
    }

    public IReadOnlyList<BlockTest> Tests { get; private set; } = [];
    public int BatchID => Session.BatchID ?? 0;
    public Batch? BatchSummary { get; private set; }

    /// <summary>
    /// Back-link target — honours <see cref="ISessionService.ReturnPage"/> so users arriving via
    /// Search submissions / View submissions ("View quality data") return there, not always to
    /// BatchesForDispatch (the only entry point the legacy hardcoded link assumed).
    /// </summary>
    public string BackLinkPage => string.IsNullOrWhiteSpace(Session.ReturnPage)
        ? "/Batches/BatchesForDispatch"
        : Session.ReturnPage;

    // Resolved display names for batch summary header
    public string? ProjectName { get; private set; }
    public string? PathologistName { get; private set; }
    public string? SpeciesName { get; private set; }
    public string? EnteredByName { get; private set; }
    public string? EnteredAreaName { get; private set; }
    public string? SubmittedByName { get; private set; }
    public string? SubmittedAreaName { get; private set; }

    [BindProperty(SupportsGet = true)] public string? FilterHistologyRef { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterTest { get; set; }

    /// <summary>
    /// Submission to open, for links arriving from a list/search page rather than from a handler
    /// that has already put the batch in session. Falls back to <see cref="ISessionService.BatchID"/>
    /// when absent, so every existing entry point is unaffected.
    /// </summary>
    [BindProperty(SupportsGet = true)] public int? BatchId { get; set; }

    public IReadOnlyList<string> HistologyRefs { get; private set; } = [];
    public IReadOnlyList<string> TestNames { get; private set; } = [];

    // ── Inline bulk edit — legacy QualityData.aspx multi-select batch-save ──────────────────

    [BindProperty] public List<int> SelectedIds { get; set; } = [];

    /// <summary>"Select all" only ever reaches the server as a flag for rows on other pages,
    /// whose checkboxes never render — mirrors <c>ArchiveBlocksModel.SelectAllAcrossPages</c>.</summary>
    [BindProperty] public bool SelectAllAcrossPages { get; set; }

    /// <summary>"" = Not tested (single row) / leave unchanged (multi-row), "1"/"2" = Passed/Failed.</summary>
    [BindProperty] public string? Result { get; set; }
    [BindProperty] public string? QCCode { get; set; }
    [BindProperty] public bool QCNote { get; set; }
    [BindProperty] public string? StainRef { get; set; }
    [BindProperty] public bool Dispatched { get; set; }
    [BindProperty] public DateTime? DispatchedDate { get; set; }
    [BindProperty] public string? DispatchedBy { get; set; }
    [BindProperty] public string? DispatchedTo { get; set; }
    [BindProperty] public string? RemedialAction { get; set; }
    [BindProperty] public string? ArchiveLocationCode { get; set; }
    [BindProperty] public DateTime? ArchivedDate { get; set; }
    [BindProperty] public string? ArchiveComment { get; set; }
    [BindProperty] public int? NumberOfSlides { get; set; }
    [BindProperty] public string? Comment { get; set; }
    [BindProperty] public List<string> SelectedCharges { get; set; } = [];

    /// <summary>Not tied to a single field (e.g. "select at least one test"), shown separately from <see cref="Errors"/>.</summary>
    public string? Error { get; private set; }
    public string? SuccessMessage { get; private set; }

    /// <summary>Field id → message, rendered via the shared clickable _ErrorSummary partial.</summary>
    public Dictionary<string, string> Errors { get; } = new();

    public IReadOnlyList<LookupItem> QCCodes { get; private set; } = [];
    public IReadOnlyList<LookupItem> RemedialActions { get; private set; } = [];
    public IReadOnlyList<LookupItem> ArchiveLocations { get; private set; } = [];
    public IReadOnlyList<LookupItem> PremiumCharges { get; private set; } = [];
    public IReadOnlyList<Administration.Models.User> Users { get; private set; } = [];

    // Resolved code→name map keyed as "TestType|Code"
    private IReadOnlyDictionary<string, string> _testNameMap = new Dictionary<string, string>();

    public string GetTestName(BlockTest t) =>
        _testNameMap.TryGetValue($"{t.TestType}|{t.Code}", out var n) ? n : t.Code;

    public IReadOnlyList<BlockTest> PagedEntries =>
        (SortColumn switch
        {
            "BlockRef"    => SortDesc ? Tests.OrderByDescending(t => t.BlockRef)         : Tests.OrderBy(t => t.BlockRef),
            "Test"        => SortDesc ? Tests.OrderByDescending(t => GetTestName(t))      : Tests.OrderBy(t => GetTestName(t)),
            "Result"      => SortDesc ? Tests.OrderByDescending(t => t.Result)            : Tests.OrderBy(t => t.Result),
            "Dispatched"  => SortDesc ? Tests.OrderByDescending(t => t.Dispatched)        : Tests.OrderBy(t => t.Dispatched),
            "Archived"    => SortDesc ? Tests.OrderByDescending(t => t.Archived)          : Tests.OrderBy(t => t.Archived),
            "OnHold"      => SortDesc ? Tests.OrderByDescending(t => t.OnHold)            : Tests.OrderBy(t => t.OnHold),
            _             => SortDesc ? Tests.OrderByDescending(t => t.HistologyRef)      : Tests.OrderBy(t => t.HistologyRef),
        })
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();

    public async Task<IActionResult> OnGetAsync()
    {
        ViewData["Title"] = "Quality data";
        ViewData["PageTitle"] = "Quality data";

        if (BatchId is > 0)
        {
            // The id came from the URL, so it has not been through an area-filtered list —
            // authorise it here rather than trusting it the way a session-set id can be trusted.
            var forbidden = await CheckBatchAccessAsync(_batches, BatchId.Value);
            if (forbidden is not null) return forbidden;

            Session.BatchID = BatchId;
        }

        if (!Session.BatchID.HasValue) return RedirectToPage("/Index");

        var allTests    = await _tests.GetByBatchAsync(Session.BatchID.Value);
        BatchSummary    = await _batches.GetByIdAsync(Session.BatchID.Value);

        if (BatchSummary is not null)
            await ResolveBatchSummaryAsync(BatchSummary);

        await ResolveTestNamesAsync(BatchSummary?.BatchType ?? Histo.Submissions.Models.BatchTypeConstants.Tse);

        HistologyRefs = allTests
            .Select(t => t.HistologyRef)
            .Where(r => !string.IsNullOrEmpty(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r)
            .ToList()!;

        TestNames = allTests
            .Select(GetTestName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList()!;

        Tests = ApplyCurrentFilter(allTests).ToList();

        await LoadEditLookupsAsync();
        PopulateGridViewData(Tests.Count);
        return Page();
    }

    /// <summary>
    /// Saves the entered quality data onto every checked row — legacy
    /// <c>QualityData.aspx.vb::btnEdit_Click</c>. One row checked writes every field as shown
    /// (blank clears); two or more rows checked applies only the fields that were filled in.
    /// </summary>
    public async Task<IActionResult> OnPostUpdateAsync()
    {
        ViewData["Title"] = "Quality data";
        ViewData["PageTitle"] = "Quality data";
        if (!Session.BatchID.HasValue) return RedirectToPage("/Index");
        var batchId = Session.BatchID.Value;

        var all = await _tests.GetByBatchAsync(batchId);

        // Legacy never lets an on-hold or unreferenced test be ticked, so "All" must skip them too.
        var selectable = all.Where(t => !t.OnHold && !string.IsNullOrWhiteSpace(t.HistologyRef)).ToList();
        var selectedIds = SelectAllAcrossPages
            ? ApplyCurrentFilter(selectable).Select(t => t.ID).ToList()
            : selectable.Where(t => SelectedIds.Contains(t.ID)).Select(t => t.ID).ToList();

        if (selectedIds.Count == 0)
        {
            Error = "Select at least one test to update.";
            await ReloadGridAsync(batchId);
            return Page();
        }

        // Legacy's two save modes: a single selection is a straight write of the form as shown
        // (the row's values were pre-filled on selection), so "blank" legitimately means "clear".
        // With two or more rows, blank means "leave each row's own value alone".
        var isSingle = selectedIds.Count == 1;

        if (!isSingle)
        {
            var anyFieldEntered = !string.IsNullOrWhiteSpace(Result) || !string.IsNullOrWhiteSpace(QCCode)
                || QCNote || !string.IsNullOrWhiteSpace(StainRef) || Dispatched
                || DispatchedDate is not null || !string.IsNullOrWhiteSpace(DispatchedBy) || !string.IsNullOrWhiteSpace(DispatchedTo)
                || !string.IsNullOrWhiteSpace(RemedialAction) || !string.IsNullOrWhiteSpace(ArchiveLocationCode) || ArchivedDate is not null
                || !string.IsNullOrWhiteSpace(ArchiveComment) || NumberOfSlides is not null || !string.IsNullOrWhiteSpace(Comment)
                || SelectedCharges.Count > 0;
            if (!anyFieldEntered)
            {
                Error = "Enter at least one field to apply to the selected tests.";
                await ReloadGridAsync(batchId);
                return Page();
            }
        }

        ValidateEntry(isSingle);
        if (Errors.Count > 0)
        {
            await ReloadGridAsync(batchId);
            return Page();
        }

        var selected = all.Where(t => selectedIds.Contains(t.ID)).ToList();
        var concurrencyCount = 0;

        foreach (var test in selected)
        {
            var applyQcNote = isSingle ? QCNote : QCNote || test.QCNote;
            var qcNoteRef = test.QCNoteRef;
            if (applyQcNote && qcNoteRef is null or 0)
            {
                var newId = await _qc.AddAsync(batchId, Session.UserID);
                if (newId > 0) qcNoteRef = newId;
            }
            else if (!applyQcNote)
            {
                qcNoteRef = null;
            }

            var newArchiveLocation = Pick(isSingle, ArchiveLocationCode, test.ArchiveLocation);
            var newArchivedDate = isSingle ? ArchivedDate : ArchivedDate ?? test.ArchivedDate;

            var updated = new BlockTest
            {
                ID = test.ID,
                BlockID = test.BlockID,
                BlockRef = test.BlockRef,
                HistologyRef = test.HistologyRef,
                TestType = test.TestType,
                Code = test.Code,
                TestDetails = test.TestDetails,
                Result = isSingle
                    ? (string.IsNullOrWhiteSpace(Result) || Result == "0" ? null : Result)
                    : Result switch { null or "" => test.Result, "0" => null, _ => Result },
                QCCode = Pick(isSingle, QCCode, test.QCCode),
                QCNote = applyQcNote,
                QCNoteRef = qcNoteRef,
                StainRef = Pick(isSingle, StainRef, test.StainRef),
                Dispatched = isSingle ? Dispatched : Dispatched || test.Dispatched,
                DispatchedDate = isSingle ? DispatchedDate : DispatchedDate ?? test.DispatchedDate,
                DispatchedBy = Pick(isSingle, DispatchedBy, test.DispatchedBy),
                PremiumCharge = test.PremiumCharge, // pass-through, not edited here
                DispatchedTo = Pick(isSingle, DispatchedTo, test.DispatchedTo),
                Comment = Pick(isSingle, Comment, test.Comment),
                RemedialAction = Pick(isSingle, RemedialAction, test.RemedialAction),
                ArchiveLocation = newArchiveLocation,
                ArchivedDate = newArchivedDate,
                ArchiveComment = Pick(isSingle, ArchiveComment, test.ArchiveComment),
                NumberOfSlides = isSingle ? NumberOfSlides : NumberOfSlides ?? test.NumberOfSlides,
                OnHold = test.OnHold,
                Archived = !string.IsNullOrWhiteSpace(newArchiveLocation) && newArchivedDate is not null,
                RowStamp = test.RowStamp,
            };

            try
            {
                await _tests.UpdateAsync(updated, Session.UserID);

                // Legacy only rewrites the charge set when the user ticked at least one box during a
                // multi-row save; a single-row save always writes it, so unticking all clears them.
                if (isSingle || SelectedCharges.Count > 0)
                    await _tests.SaveTCCodesAsync(batchId, test.ID, test.TestType, test.TCCodes, SelectedCharges, Session.UserID);
            }
            catch (BlockTestConcurrencyException)
            {
                concurrencyCount++;
            }
        }

        await CompleteBatchIfAllTestsDispatchedAsync(batchId);

        Error = concurrencyCount > 0
            ? $"{concurrencyCount} test(s) were modified by another user and were not updated. Please reload and try again."
            : null;
        SuccessMessage = concurrencyCount == 0 ? $"Updated {selected.Count} test(s)." : null;

        await ReloadGridAsync(batchId);
        return Page();
    }

    /// <summary>Single-row saves write blanks through as clears; multi-row saves treat blank as "unchanged".</summary>
    private static string? Pick(bool isSingle, string? entered, string? existing) =>
        isSingle
            ? (string.IsNullOrWhiteSpace(entered) ? null : entered)
            : (!string.IsNullOrWhiteSpace(entered) ? entered : existing);

    /// <summary>
    /// Cross-field checks mirroring legacy's conditionally-enabled validators. In multi-row mode
    /// only the fields the user actually filled in are validated, since blanks mean "unchanged".
    /// Accumulates every failing field into <see cref="Errors"/> rather than stopping at the
    /// first one, so the error summary can link to (and the user can fix) all of them at once.
    /// </summary>
    private void ValidateEntry(bool isSingle)
    {
        if (Dispatched)
        {
            if (DispatchedDate is null) Errors["DispatchedDate"] = "Enter a dispatched date.";
            if (string.IsNullOrWhiteSpace(DispatchedBy)) Errors["DispatchedBy"] = "Select who dispatched the test.";
            if (string.IsNullOrWhiteSpace(DispatchedTo)) Errors["DispatchedTo"] = "Enter who the test was dispatched to.";
        }

        if (Result == BlockTestResult.Failed)
        {
            if (string.IsNullOrWhiteSpace(QCCode)) Errors["QCCode"] = "Enter a QC code when setting the result to Failed.";
            if (string.IsNullOrWhiteSpace(RemedialAction)) Errors["RemedialAction"] = "Select a remedial action when setting the result to Failed.";
        }

        if (!string.IsNullOrWhiteSpace(ArchiveLocationCode) && ArchivedDate is null)
            Errors["ArchivedDate"] = "Enter an archive date when an archive location is selected.";
        if (ArchivedDate is not null && string.IsNullOrWhiteSpace(ArchiveLocationCode))
            Errors["ArchiveLocationCode"] = "Select an archive location when an archive date is entered.";

        if (NumberOfSlides is <= 0) Errors["NumberOfSlides"] = "Number of blocks/slides must be greater than zero.";
        if (isSingle && NumberOfSlides is null) Errors["NumberOfSlides"] = "Enter the number of blocks/slides.";
    }

    private IEnumerable<BlockTest> ApplyCurrentFilter(IEnumerable<BlockTest> tests)
    {
        if (!string.IsNullOrEmpty(FilterHistologyRef))
            tests = tests.Where(t => t.HistologyRef == FilterHistologyRef);
        if (!string.IsNullOrEmpty(FilterTest))
            tests = tests.Where(t => string.Equals(GetTestName(t), FilterTest, StringComparison.OrdinalIgnoreCase));
        return tests;
    }

    /// <summary>
    /// Reproduces legacy <c>QualityData.aspx.vb::UpdateSessionWithQualityData</c>: once every test
    /// on the batch has been dispatched, the batch is marked Completed and stamped with the latest
    /// dispatch date.
    /// </summary>
    private async Task CompleteBatchIfAllTestsDispatchedAsync(int batchId)
    {
        var tests = await _tests.GetByBatchAsync(batchId);
        if (tests.Count == 0) return;
        if (!tests.All(t => t.Dispatched && t.DispatchedDate is not null)) return;

        var latestDispatch = tests.Max(t => t.DispatchedDate!.Value);
        await _batches.SetCompletedAsync(batchId, latestDispatch, Session.UserID);
    }

    /// <summary>Re-runs the same load/filter/lookup logic as <see cref="OnGetAsync"/> after a POST, so the redisplayed grid reflects the just-applied changes.</summary>
    private async Task ReloadGridAsync(int batchId)
    {
        var allTests = await _tests.GetByBatchAsync(batchId);
        BatchSummary = await _batches.GetByIdAsync(batchId);
        if (BatchSummary is not null) await ResolveBatchSummaryAsync(BatchSummary);
        await ResolveTestNamesAsync(BatchSummary?.BatchType ?? Histo.Submissions.Models.BatchTypeConstants.Tse);

        HistologyRefs = allTests.Select(t => t.HistologyRef).Where(r => !string.IsNullOrEmpty(r))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(r => r).ToList()!;
        TestNames = allTests.Select(GetTestName).Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList()!;

        Tests = ApplyCurrentFilter(allTests).ToList();

        await LoadEditLookupsAsync();
        PopulateGridViewData(Tests.Count);
    }

    private async Task LoadEditLookupsAsync()
    {
        QCCodes          = await _lookups.GetLookupDataAsync(LookupQcCode);
        RemedialActions  = await _lookups.GetLookupDataAsync(LookupRemedialAction);
        ArchiveLocations = await _lookups.GetLookupDataAsync(LookupArchiveLocation);
        PremiumCharges   = await _lookups.GetLookupDataAsync(LookupPremiumCharges);
        Users            = await _users.GetAllUsersAsync();
    }

    private async Task ResolveTestNamesAsync(int batchType)
    {
        var antibodyId = batchType == Histo.Submissions.Models.BatchTypeConstants.Tse ? LookupAntibodiesTse : LookupAntibodiesNonTse;

        var histTask     = _lookups.GetHistologyTypesAsync();
        var antibodyTask = _lookups.GetLookupDataAsync(antibodyId);
        var stainTask    = _lookups.GetLookupDataAsync(LookupSpecialStain);
        await Task.WhenAll(histTask, antibodyTask, stainTask);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var i in histTask.Result)     map[$"Histology|{i.Code ?? i.ID.ToString()}"]  = i.Name;
        foreach (var i in antibodyTask.Result) map[$"Antibodies|{i.Code ?? i.ID.ToString()}"] = i.Name;
        foreach (var i in stainTask.Result)    map[$"Stain|{i.Code ?? i.ID.ToString()}"]      = i.Name;
        _testNameMap = map;
    }

    private async Task ResolveBatchSummaryAsync(Batch batch)
    {
        var summary = await BatchSummaryDisplayResolver.ResolveAsync(batch, _lookups, _users);
        ProjectName       = summary.ProjectName;
        PathologistName   = summary.PathologistName;
        SpeciesName       = summary.SpeciesName;
        EnteredByName     = summary.EnteredByName;
        EnteredAreaName   = summary.EnteredAreaName;
        SubmittedByName   = summary.SubmittedByName;
        SubmittedAreaName = summary.SubmittedAreaName;
    }
}


