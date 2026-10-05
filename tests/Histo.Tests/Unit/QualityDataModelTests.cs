using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.QualityControl.Interfaces;
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
