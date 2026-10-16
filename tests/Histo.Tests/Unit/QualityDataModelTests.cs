using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.QC;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="QualityDataModel"/>.</summary>
public class QualityDataModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockTestService> _tests = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<IQCNoteService> _qc = new();

    public QualityDataModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.Object.BatchID = 42;
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private QualityDataModel CreateSut() =>
        new(_session.Object, _tests.Object, _batches.Object, _lookups.Object, _users.Object, _qc.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
        };

    [Fact]
    public async Task OnGetAsync_ResolvesSpeciesNameFromLookup_NotRawId()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42, Species = "3" });
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 3, Name = "Bovine" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal("Bovine", sut.SpeciesName);
    }

    [Fact]
    public async Task OnGetAsync_ResolvesProjectAndContact_WithIncludeInactiveTrue()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 42, ProjectContractCode = "7", ContactName = "9" });
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(19, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 7, Name = "Deactivated Project" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 9, Name = "Dr Retired" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal("Deactivated Project", sut.ProjectName);
        Assert.Equal("Dr Retired", sut.PathologistName);
        _lookups.Verify(l => l.GetLookupDataAsync(19, true, It.IsAny<CancellationToken>()), Times.Once);
        _lookups.Verify(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_TestNamesFilter_UsesResolvedDisplayName_NotRawCode()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[
            new BlockTest { ID = 1, TestType = "Histology", Code = "2", HistologyRef = "24/001", BlockRef = "01" },
        ]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 2, Code = "2", Name = "H&E" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Contains("H&E", sut.TestNames);
        Assert.DoesNotContain("2", sut.TestNames);
    }

    [Fact]
    public async Task OnGetAsync_FilterTestByResolvedName_FiltersGridRows()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[
            new BlockTest { ID = 1, TestType = "Histology", Code = "2", HistologyRef = "24/001", BlockRef = "01" },
            new BlockTest { ID = 2, TestType = "Histology", Code = "3", HistologyRef = "24/002", BlockRef = "02" },
        ]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { ID = 2, Code = "2", Name = "H&E" },
                new LookupItem { ID = 3, Code = "3", Name = "Special Stain" },
            ]);
        var sut = CreateSut();
        sut.FilterTest = "H&E";

        await sut.OnGetAsync();

        Assert.Single(sut.Tests);
        Assert.Equal(1, sut.Tests[0].ID);
    }

    // ── OnPostUpdateAsync — legacy multi-select bulk-save ────────────────────────────────────

    private static BlockTest MakeTest(int id, string? result = null, bool dispatched = false) => new()
    {
        ID = id,
        BlockID = 1,
        TestType = "Histology",
        Code = "1",
        HistologyRef = "24/001",
        BlockRef = "01",
        Result = result,
        Dispatched = dispatched,
        DispatchedDate = dispatched ? new DateTime(2026, 5, 1) : null,
        RowStamp = [1, 2, 3],
    };

    [Fact]
    public async Task OnPostUpdateAsync_NoRowsSelected_SetsErrorAndDoesNotSave()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.Result = Histo.Histology.Models.BlockTestResult.Passed;

        await sut.OnPostUpdateAsync();

        Assert.Equal("Select at least one test to update.", sut.Error);
        _tests.Verify(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_MultiSelectWithNoFieldsEntered_SetsErrorAndDoesNotSave()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1), MakeTest(2)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.SelectedIds = [1, 2];

        await sut.OnPostUpdateAsync();

        Assert.Equal("Enter at least one field to apply to the selected tests.", sut.Error);
        _tests.Verify(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_OnHoldRow_IsNeverUpdated()
    {
        var onHold = new BlockTest { ID = 2, BlockID = 1, TestType = "Histology", Code = "1", HistologyRef = "24/001", BlockRef = "02", OnHold = true, RowStamp = [1] };
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1), onHold]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.SelectAllAcrossPages = true;
        sut.StainRef = "S1";
        sut.NumberOfSlides = 1;

        await sut.OnPostUpdateAsync();

        _tests.Verify(t => t.UpdateAsync(It.Is<BlockTest>(bt => bt.ID == 2), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _tests.Verify(t => t.UpdateAsync(It.Is<BlockTest>(bt => bt.ID == 1), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostUpdateAsync_AppliesOneFieldToEveryCheckedRow_LeavingOthersUnchanged()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1), MakeTest(2), MakeTest(3)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = new List<BlockTest>();
        _tests.Setup(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<BlockTest, int, CancellationToken>((bt, _, _) => saved.Add(bt))
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        sut.SelectedIds = [1, 3]; // row 2 left unselected
        sut.Result = Histo.Histology.Models.BlockTestResult.Passed;

        await sut.OnPostUpdateAsync();
        Assert.Null(sut.Error);
        Assert.Equal(2, saved.Count);
        Assert.All(saved, bt => Assert.Equal(Histo.Histology.Models.BlockTestResult.Passed, bt.Result));
        _tests.Verify(t => t.UpdateAsync(It.Is<BlockTest>(bt => bt.ID == 2), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_MultiSelect_BlankFieldsPreserveEachRowsExistingValue()
    {
        var existing = MakeTest(1, result: Histo.Histology.Models.BlockTestResult.Failed);
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[existing, MakeTest(2, result: Histo.Histology.Models.BlockTestResult.Failed)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = new List<BlockTest>();
        _tests.Setup(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<BlockTest, int, CancellationToken>((bt, _, _) => saved.Add(bt))
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        sut.SelectedIds = [1, 2];
        sut.StainRef = "S1"; // only this field entered

        await sut.OnPostUpdateAsync();

        Assert.Equal(2, saved.Count);
        Assert.All(saved, bt => Assert.Equal("S1", bt.StainRef));
        Assert.All(saved, bt => Assert.Equal(Histo.Histology.Models.BlockTestResult.Failed, bt.Result)); // untouched
    }

    [Fact]
    public async Task OnPostUpdateAsync_MultiSelectWithQcNoteTicked_CreatesOnlyOneSharedNote_AndAppendsComments()
    {
        // Legacy's iQCNoteID: exactly one new QC note per Edit click, not one per selected test.
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1), MakeTest(2)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        _qc.Setup(AnyNoteCreation).ReturnsAsync(new SubmissionQCNote(500, null));
        var sut = CreateSut();
        sut.SelectedIds = [1, 2];
        sut.QCNote = true;
        sut.Comment = "needs repeat";

        await sut.OnPostUpdateAsync();

        _qc.Verify(AnyNoteCreation, Times.Once);
        Assert.Equal(2, saved.Count);
        Assert.All(saved, bt => Assert.Equal(500, bt.QCNoteRef));
        Assert.Equal(500, sut.SubmissionQCNoteRef);
        _qc.Verify(q => q.AppendAsync(500,
            It.Is<IReadOnlyList<string>>(l => l.Count == 2 && l.All(s => s.EndsWith(": needs repeat"))),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private List<BlockTest> CaptureSaves()
    {
        var saved = new List<BlockTest>();
        _tests.Setup(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<BlockTest, int, CancellationToken>((bt, _, _) => saved.Add(bt))
            .Returns(Task.CompletedTask);
        return saved;
    }

    private static readonly System.Linq.Expressions.Expression<Func<IQCNoteService, Task<SubmissionQCNote?>>> AnyNoteCreation =
        q => q.GetOrCreateForSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>());

    private static BlockTest WithNote(BlockTest t, int? noteRef) => new()
    {
        ID = t.ID, BlockID = t.BlockID, TestType = t.TestType, Code = t.Code, HistologyRef = t.HistologyRef,
        BlockRef = t.BlockRef, RowStamp = t.RowStamp, QCNote = noteRef is > 0, QCNoteRef = noteRef,
    };

    [Fact]
    public async Task OnPostUpdateAsync_SingleSelectNoRef_QcNoteTicked_CreatesNote_AndExposesRef()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        _qc.Setup(q => q.GetOrCreateForSubmissionAsync(42, 1, "Histology", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionQCNote(700, [9, 9, 9]));
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.QCNote = true;
        sut.NumberOfSlides = 1;

        await sut.OnPostUpdateAsync();

        _qc.Verify(AnyNoteCreation, Times.Once);
        var row = Assert.Single(saved);
        Assert.Equal(700, row.QCNoteRef);
        Assert.Equal(new byte[] { 9, 9, 9 }, row.RowStamp); // the link bumped the row's rowstamp inside the note transaction
        Assert.Equal(700, sut.SubmissionQCNoteRef);
    }

    [Fact]
    public async Task OnPostUpdateAsync_NoteCreationFails_TestsSavedWithoutRef_WarningShown()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        _qc.Setup(AnyNoteCreation).ReturnsAsync((SubmissionQCNote?)null);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.QCNote = true;
        sut.NumberOfSlides = 1;

        await sut.OnPostUpdateAsync();

        Assert.Null(Assert.Single(saved).QCNoteRef);
        Assert.Contains("could not be created", sut.NoteWarning);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SingleSelectWithExistingRef_ReusesRef_NeverCreatesNote()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.QCNote = true;
        sut.NumberOfSlides = 1;
        sut.Comment = "second update";

        await sut.OnPostUpdateAsync();

        _qc.Verify(AnyNoteCreation, Times.Never);
        Assert.Equal(321, Assert.Single(saved).QCNoteRef);
        Assert.Equal(321, sut.SubmissionQCNoteRef);
        _qc.Verify(q => q.AppendAsync(321,
            It.Is<IReadOnlyList<string>>(l => l.Single() == "Histology - 1 (24/001 / 01): second update"),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostUpdateAsync_NoteFull_TestsSaved_WarningShown()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        _qc.Setup(q => q.AppendAsync(321, It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QCNoteAppendResult.TooLong);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.NumberOfSlides = 1;
        sut.Comment = "x";

        await sut.OnPostUpdateAsync();

        Assert.Single(saved);
        Assert.Contains("QC note 321 is full", sut.NoteWarning);
    }

    [Fact]
    public async Task OnPostUpdateAsync_MultiSelectMixed_RowsWithoutRefJoinTheExistingRef()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321), MakeTest(2), MakeTest(3)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        var sut = CreateSut();
        sut.SelectedIds = [1, 2, 3];
        sut.QCNote = true;

        await sut.OnPostUpdateAsync();

        _qc.Verify(AnyNoteCreation, Times.Never);
        Assert.Equal(3, saved.Count);
        Assert.All(saved, bt => Assert.Equal(321, bt.QCNoteRef));
        Assert.Equal(321, sut.SubmissionQCNoteRef);
    }

    [Fact]
    public async Task OnPostUpdateAsync_OlderSubmissionWithTwoRefs_NewRowsJoinTheLatest_ExistingKeepTheirs()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321), WithNote(MakeTest(2), 322), MakeTest(3)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        var sut = CreateSut();
        sut.SelectedIds = [1, 2, 3];
        sut.StainRef = "S1";

        await sut.OnPostUpdateAsync();

        _qc.Verify(AnyNoteCreation, Times.Never);
        Assert.Equal(321, saved.Single(t => t.ID == 1).QCNoteRef);
        Assert.Equal(322, saved.Single(t => t.ID == 2).QCNoteRef);
        Assert.Equal(322, saved.Single(t => t.ID == 3).QCNoteRef);
        Assert.Equal(322, sut.SubmissionQCNoteRef);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SubmissionAlreadyHasNote_UnselectedRefRowStillDrivesTheLink()
    {
        // Test 1 (not selected) already carries the submission's note; test 2 is updated with the box not posted.
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321), MakeTest(2)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        var sut = CreateSut();
        sut.SelectedIds = [2];
        sut.NumberOfSlides = 1;
        sut.Comment = "later finding";

        await sut.OnPostUpdateAsync();

        _qc.Verify(AnyNoteCreation, Times.Never);
        var row = Assert.Single(saved);
        Assert.Equal(321, row.QCNoteRef);
        Assert.True(row.QCNote);
        _qc.Verify(q => q.AppendAsync(321, It.Is<IReadOnlyList<string>>(l => l.Single().EndsWith(": later finding")),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_SubmissionWithNote_ExposesItsRef()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1), WithNote(MakeTest(2), 321)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(321, sut.SubmissionQCNoteRef);
        Assert.Equal("321", sut.QCNoteRefDisplay());
    }

    [Fact]
    public async Task OnGetAsync_WarningFromSingleTestEditor_IsShown()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.TempData[QCNoteText.WarningTempDataKey] = "QC note 5 is full";

        await sut.OnGetAsync();

        Assert.Equal("QC note 5 is full", sut.NoteWarning);
    }

    [Fact]
    public async Task OnPostUpdateAsync_OlderSubmissionWithTwoRefs_DisplaysEachSelectedTestsOwnRef()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321), WithNote(MakeTest(2), 322), MakeTest(3)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        CaptureSaves();
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.NumberOfSlides = 1;

        await sut.OnPostUpdateAsync();

        Assert.Equal("321", sut.QCNoteRefDisplay());
        Assert.Equal(new[] { 322, 321 }, sut.SubmissionQCNoteRefs);

        sut.SelectedIds = [1, 3]; // 3 has no ref, so it goes to the latest
        Assert.Equal("321, 322", sut.QCNoteRefDisplay());
    }

    [Fact]
    public async Task OnPostUpdateAsync_NoteLinesUseResolvedNameAndTestType_SoSameCodeDifferentTypeIsNotDeduped()
    {
        var histology = WithNote(MakeTest(1), 321);
        var antibody = new BlockTest
        {
            ID = 2, BlockID = 1, TestType = BlockTestType.Antibodies, Code = "1", HistologyRef = "24/001",
            BlockRef = "01", QCNote = true, QCNoteRef = 321, RowStamp = [1],
        };
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[histology, antibody]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Code = "1", Name = "EO" }]);
        CaptureSaves();
        IReadOnlyList<string>? appended = null;
        _qc.Setup(q => q.AppendAsync(321, It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<int, IReadOnlyList<string>, int, CancellationToken>((_, l, _, _) => appended = l)
            .ReturnsAsync(QCNoteAppendResult.Appended);
        var sut = CreateSut();
        sut.SelectedIds = [1, 2];
        sut.Comment = "same comment";

        await sut.OnPostUpdateAsync();

        Assert.Equal(
            new[] { "Histology - EO (24/001 / 01): same comment", "Antibodies - 1 (24/001 / 01): same comment" },
            appended);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SingleSelectWithRef_DisabledBoxNotPosted_KeepsRef()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.QCNote = false;
        sut.NumberOfSlides = 1;

        await sut.OnPostUpdateAsync();

        var row = Assert.Single(saved);
        Assert.Equal(321, row.QCNoteRef);
        Assert.True(row.QCNote);
        Assert.Equal(321, sut.SubmissionQCNoteRef);
        _qc.Verify(AnyNoteCreation, Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SingleSelectNoRef_QcNoteUnticked_LeavesNoRef()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.NumberOfSlides = 1;
        sut.Comment = "no note";

        await sut.OnPostUpdateAsync();

        Assert.Null(Assert.Single(saved).QCNoteRef);
        Assert.Null(sut.SubmissionQCNoteRef);
        _qc.Verify(q => q.AppendAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SingleSelect_WritesEveryFieldAsShown_ClearingBlanks()
    {
        var existing = MakeTest(1, result: Histo.Histology.Models.BlockTestResult.Failed);
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[existing]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        BlockTest? saved = null;
        _tests.Setup(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<BlockTest, int, CancellationToken>((bt, _, _) => saved = bt)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.StainRef = "S1";
        sut.NumberOfSlides = 1;
        // Result left blank — a single-row save clears it rather than preserving "Failed".

        await sut.OnPostUpdateAsync();

        Assert.NotNull(saved);
        Assert.Equal("S1", saved!.StainRef);
        Assert.Null(saved.Result);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SingleSelect_WithoutNumberOfSlides_SetsError()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.StainRef = "S1";

        await sut.OnPostUpdateAsync();

        Assert.Equal("Enter the number of blocks/slides.", sut.Errors["NumberOfSlides"]);
        _tests.Verify(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_ResultSetToFailedWithoutQcCode_SetsError()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.Result = Histo.Histology.Models.BlockTestResult.Failed;

        await sut.OnPostUpdateAsync();

        Assert.Equal("Enter a QC code when setting the result to Failed.", sut.Errors["QCCode"]);
        _tests.Verify(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_DispatchedYesWithoutRequiredFields_SetsError()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.Dispatched = true;

        await sut.OnPostUpdateAsync();

        Assert.Equal("Enter a dispatched date.", sut.Errors["DispatchedDate"]);
        _tests.Verify(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_ConcurrencyExceptionOnOneRow_ReportsCountAndSavesTheRest()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1), MakeTest(2)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        _tests.Setup(t => t.UpdateAsync(It.Is<BlockTest>(bt => bt.ID == 1), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BlockTestConcurrencyException());
        _tests.Setup(t => t.UpdateAsync(It.Is<BlockTest>(bt => bt.ID == 2), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        sut.SelectedIds = [1, 2];
        sut.StainRef = "S1";

        await sut.OnPostUpdateAsync();

        Assert.Contains("1 test(s)", sut.Error);
        Assert.Null(sut.SuccessMessage);
        _tests.Verify(t => t.UpdateAsync(It.Is<BlockTest>(bt => bt.ID == 2), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SelectAllAcrossPages_AppliesToEveryFilteredRow()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1), MakeTest(2), MakeTest(3)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = new List<int>();
        _tests.Setup(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<BlockTest, int, CancellationToken>((bt, _, _) => saved.Add(bt.ID))
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        sut.SelectAllAcrossPages = true;
        sut.StainRef = "S1";

        await sut.OnPostUpdateAsync();

        Assert.Equal([1, 2, 3], saved.Order());
    }

    [Fact]
    public async Task OnPostUpdateAsync_AllTestsDispatched_CompletesBatch()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1, dispatched: true)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.StainRef = "S1";
        sut.NumberOfSlides = 1;

        await sut.OnPostUpdateAsync();

        _batches.Verify(b => b.SetCompletedAsync(42, new DateTime(2026, 5, 1), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
