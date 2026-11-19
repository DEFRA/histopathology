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

        var test = Assert.Single(sut.Tests);
        Assert.Equal(1, test.ID);
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
    public async Task OnPostUpdateAsync_MultiSelectWithQcNoteTicked_CreatesOnlyOneSharedNote_DoesNotAppendComment()
    {
        // One new note per save action, shared by every row ticked in THIS save — not one per row.
        // Edit-quality-data comments must never be auto-appended to the note's text (legacy parity).
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1), MakeTest(2)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        _qc.Setup(q => q.AddAsync(42, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(500);
        var sut = CreateSut();
        sut.SelectedIds = [1, 2];
        sut.QCNote = true;
        sut.Comment = "needs repeat";

        await sut.OnPostUpdateAsync();

        _qc.Verify(q => q.AddAsync(42, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(2, saved.Count);
        Assert.All(saved, bt => Assert.Equal(500, bt.QCNoteRef));
        _qc.Verify(q => q.AppendAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_TwoSeparateSaves_EachCreatesItsOwnIndependentNote()
    {
        // Confirms notes are never reused across separate saves — each save gets its own.
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        _qc.Setup(q => q.AddAsync(42, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.QCNote = true;
        sut.NumberOfSlides = 1;
        await sut.OnPostUpdateAsync();
        Assert.Equal(100, saved.Single().QCNoteRef);

        // A later, separate save for a different test with no ref gets a brand-new note, not 100.
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 100), MakeTest(2)]);
        _qc.Setup(q => q.AddAsync(42, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(200);
        saved.Clear();
        sut.SelectedIds = [2];

        await sut.OnPostUpdateAsync();

        Assert.Equal(200, saved.Single().QCNoteRef);
    }

    private List<BlockTest> CaptureSaves()
    {
        var saved = new List<BlockTest>();
        _tests.Setup(t => t.UpdateAsync(It.IsAny<BlockTest>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<BlockTest, int, CancellationToken>((bt, _, _) => saved.Add(bt))
            .Returns(Task.CompletedTask);
        return saved;
    }

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
        _qc.Setup(q => q.AddAsync(42, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(700);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.QCNote = true;
        sut.NumberOfSlides = 1;

        await sut.OnPostUpdateAsync();

        _qc.Verify(q => q.AddAsync(42, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        var row = Assert.Single(saved);
        Assert.Equal(700, row.QCNoteRef);
        Assert.Equal(new byte[] { 1, 2, 3 }, row.RowStamp); // unchanged — no more atomic link bumping it
    }

    [Fact]
    public async Task OnPostUpdateAsync_NoteCreationFails_TestsSavedWithoutRef_WarningShown()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        _qc.Setup(q => q.AddAsync(42, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.QCNote = true;
        sut.NumberOfSlides = 1;

        await sut.OnPostUpdateAsync();

        Assert.Null(Assert.Single(saved).QCNoteRef);
        Assert.Contains("could not be created", sut.NoteWarning);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SingleSelectWithExistingRef_TickedKeepsRef_NeverCreatesNote()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.QCNote = true; // pre-ticked client-side since the row already has a note
        sut.NumberOfSlides = 1;
        sut.Comment = "second update";

        await sut.OnPostUpdateAsync();

        _qc.Verify(q => q.AddAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(321, Assert.Single(saved).QCNoteRef);
        _qc.Verify(q => q.AppendAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_MultiSelectMixed_RowsWithoutRefGetANewSharedNote_ExistingRefUntouched()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321), MakeTest(2), MakeTest(3)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        _qc.Setup(q => q.AddAsync(42, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(900);
        var sut = CreateSut();
        sut.SelectedIds = [1, 2, 3];
        sut.QCNote = true;

        await sut.OnPostUpdateAsync();

        _qc.Verify(q => q.AddAsync(42, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(321, saved.Single(t => t.ID == 1).QCNoteRef); // its own existing ref, untouched
        Assert.Equal(900, saved.Single(t => t.ID == 2).QCNoteRef); // new shared note
        Assert.Equal(900, saved.Single(t => t.ID == 3).QCNoteRef); // new shared note
    }

    [Fact]
    public async Task OnPostUpdateAsync_MultiSelectMixed_EachRowWithExistingRefKeepsItsOwn_UntickedRowWithoutRefGetsNone()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321), WithNote(MakeTest(2), 322), MakeTest(3)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        var sut = CreateSut();
        sut.SelectedIds = [1, 2, 3];
        sut.StainRef = "S1";

        await sut.OnPostUpdateAsync();

        _qc.Verify(q => q.AddAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(321, saved.Single(t => t.ID == 1).QCNoteRef);
        Assert.Equal(322, saved.Single(t => t.ID == 2).QCNoteRef);
        Assert.Null(saved.Single(t => t.ID == 3).QCNoteRef); // no submission-wide fallback any more
    }

    [Fact]
    public async Task OnPostUpdateAsync_OtherTestsNoteDoesNotAutoLinkAnUnrelatedUpdate()
    {
        // Test 1 (not selected) already carries its own note; test 2's update must not auto-join it.
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[WithNote(MakeTest(1), 321), MakeTest(2)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var saved = CaptureSaves();
        var sut = CreateSut();
        sut.SelectedIds = [2];
        sut.NumberOfSlides = 1;
        sut.Comment = "later finding";

        await sut.OnPostUpdateAsync();

        _qc.Verify(q => q.AddAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        var row = Assert.Single(saved);
        Assert.Null(row.QCNoteRef);
        Assert.False(row.QCNote);
        _qc.Verify(q => q.AppendAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_SubmissionWithNote_NoSelection_DisplayIsEmpty()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1), WithNote(MakeTest(2), 321)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal("", sut.QCNoteRefDisplay());
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

        sut.SelectedIds = [1, 3]; // 3 has no ref of its own — no fallback, so it contributes nothing
        Assert.Equal("321", sut.QCNoteRefDisplay());
    }

    [Fact]
    public async Task OnPostUpdateAsync_SingleSelectWithRef_UntickedBox_UnlinksRef()
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
        Assert.Null(row.QCNoteRef);
        Assert.False(row.QCNote);
        _qc.Verify(q => q.AddAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
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

    /// <summary>
    /// Legacy: QualityData.aspx.vb::btnSave_Click always calls UpdateSubmissionStatus (sets the
    /// batch InProgress) on every save, regardless of whether every test ends up dispatched.
    /// </summary>
    [Fact]
    public async Task OnPostUpdateAsync_NotAllTestsDispatched_StillSetsBatchInProgress()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[MakeTest(1, dispatched: false)]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();
        sut.SelectedIds = [1];
        sut.StainRef = "S1";
        sut.NumberOfSlides = 1;

        await sut.OnPostUpdateAsync();

        _batches.Verify(b => b.SetInProgressAsync(42, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _batches.Verify(b => b.SetCompletedAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
