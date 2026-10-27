using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Archive;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="ArchiveTissuesModel"/>.</summary>
public class ArchiveTissuesModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public ArchiveTissuesModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
    }

    private ArchiveTissuesModel CreateSut() =>
        new(_session.Object, _submissions.Object, _batches.Object, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnGetAsync_NoBatchIdInSession_LeavesRowsEmpty()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Empty(sut.Rows);
        _submissions.Verify(s => s.GetBatchSubmissionTissuesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Filters — legacy ddlHistologyRefList / ddlTissueList applied via DataView.RowFilter ──

    /// <summary>Three tissues across two animals, giving two histology refs and two tissue types.</summary>
    private void SetupFilterFixture()
    {
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42 });
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)
            [
                new BatchSubmission { ID = 10, AnimalID = 7 },
                new BatchSubmission { ID = 11, AnimalID = 8 },
            ]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)
            [
                new Animal { ID = 7, SenderRef = "S1", HistologyRef = "24/001" },
                new Animal { ID = 8, SenderRef = "S2", HistologyRef = "24/002" },
            ]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)
            [
                new Tissue { ID = 1, OwnerID = 10, TissueCode = "1" },
                new Tissue { ID = 2, OwnerID = 10, TissueCode = "2" },
                new Tissue { ID = 3, OwnerID = 11, TissueCode = "2" },
            ]);
        _lookups.Setup(l => l.GetLookupDataAsync(9, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)
            [
                new LookupItem { Code = "1", Name = "Liver" },
                new LookupItem { Code = "2", Name = "Kidney" },
            ]);
    }

    [Fact]
    public async Task NoFilter_ReturnsEveryRow()
    {
        SetupFilterFixture();
        var sut = CreateSut();
        sut.BatchId = 42;

        await sut.OnGetAsync();

        Assert.False(sut.FilterApplied);
        Assert.Equal(3, sut.TotalCount);
        Assert.Equal(["24/001", "24/002"], sut.HistologyRefOptions);
        Assert.Equal(["Kidney", "Liver"], sut.TissueOptions);
    }

    [Fact]
    public async Task HistologyRefFilter_KeepsOnlyThatAnimalsTissues()
    {
        SetupFilterFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        sut.FilterHistologyRef = "24/001";

        await sut.OnGetAsync();

        Assert.True(sut.FilterApplied);
        Assert.Equal(2, sut.TotalCount);
        Assert.All(sut.FilteredRows, r => Assert.Equal("24/001", r.HistologyRef));
    }

    [Fact]
    public async Task TissueFilter_KeepsOnlyThatTissue()
    {
        SetupFilterFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        sut.FilterTissue = "Kidney";

        await sut.OnGetAsync();

        Assert.Equal(2, sut.TotalCount);
        Assert.All(sut.FilteredRows, r => Assert.Equal("Kidney", r.TissueDescription));
    }

    [Fact]
    public async Task BothFiltersCombine_AsAnAndCondition()
    {
        SetupFilterFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        sut.FilterHistologyRef = "24/001";
        sut.FilterTissue = "Kidney";

        await sut.OnGetAsync();

        var row = Assert.Single(sut.FilteredRows);
        Assert.Equal("S1", row.SenderRef);
    }

    [Fact]
    public async Task FilterOptions_AreBuiltFromAllRowsNotTheFilteredSubset()
    {
        SetupFilterFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        sut.FilterTissue = "Liver";

        await sut.OnGetAsync();

        // Options must stay complete so the user can change or clear the filter.
        Assert.Equal(["24/001", "24/002"], sut.HistologyRefOptions);
        Assert.Equal(1, sut.TotalCount);
    }

    [Fact]
    public async Task OnGetAsync_BatchIdInSession_LoadsRows()
    {
        _session.Object.BatchID = 5;
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 1, OwnerID = 1, Owner = TissueOwner.Submission, TissueCode = "T1" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Single(sut.Rows);
    }

    private void SetupArchivedFixture()
    {
        SetupFilterFixture();
        _lookups.Setup(l => l.GetLookupDataAsync(16, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)
            [
                new LookupItem { Code = "L1", Name = "Alpha" },
                new LookupItem { Code = "L2", Name = "Beta" },
            ]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)
            [
                new Tissue { ID = 1, OwnerID = 10, TissueCode = "1", ArchiveLocation = "L2", ArchivedDate = new DateTime(2024, 1, 2) },
                new Tissue { ID = 2, OwnerID = 10, TissueCode = "2", ArchiveLocation = "L1", ArchivedDate = new DateTime(2024, 1, 1) },
                new Tissue { ID = 3, OwnerID = 11, TissueCode = "2", ArchiveLocation = "L1", ArchivedDate = new DateTime(2024, 1, 3) },
            ]);
    }

    [Theory]
    [InlineData("HistologyRef", true, new[] { 3, 1, 2 })]
    [InlineData("HistologyRef", false, new[] { 1, 2, 3 })]
    [InlineData("Tissue", false, new[] { 2, 3, 1 })]
    [InlineData("ArchivedDate", false, new[] { 2, 1, 3 })]
    [InlineData("ArchiveLocation", false, new[] { 2, 3, 1 })]
    [InlineData(null, true, new[] { 3, 1, 2 })]
    public async Task PagedEntries_SortsByTheSelectedColumn(string? column, bool desc, int[] expectedIds)
    {
        SetupArchivedFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        await sut.OnGetAsync();
        sut.SortColumn = column;
        sut.SortDesc = desc;

        Assert.Equal(expectedIds, sut.PagedEntries.Select(r => r.ID));
    }

    [Fact]
    public async Task OnPostUpdateAsync_AllAcrossPagesSelected_UpdatesEveryFilteredRow()
    {
        SetupArchivedFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        await sut.OnGetAsync();
        sut.SelectAllAcrossPages = true;
        sut.ArchiveLocationCode = "L1";
        sut.ArchivedDate = DateTime.Today;

        await sut.OnPostUpdateAsync();

        _submissions.Verify(s => s.UpdateTissueAsync(It.IsAny<Tissue>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        Assert.Equal("Archive information updated for 3 tissue(s).", sut.SuccessMessage);
    }

    [Theory]
    [InlineData(null, true, false)]
    [InlineData("L1", false, false)]
    [InlineData(null, false, false)]
    [InlineData(null, true, true)]
    [InlineData("L1", false, true)]
    public async Task OnPostUpdateAsync_MissingLocationOrDate_IsRejectedAndNothingIsSaved(string? location, bool withDate, bool allRows)
    {
        SetupArchivedFixture();
        var sut = CreateSut();
        sut.BatchId = 42;
        await sut.OnGetAsync();
        if (allRows) sut.SelectAllAcrossPages = true; else sut.SelectedIds = [1];
        sut.ArchiveLocationCode = location;
        sut.ArchivedDate = withDate ? DateTime.Today : null;

        await sut.OnPostUpdateAsync();

        Assert.Equal("Select an archive location and enter an archived date.", sut.Error);
        Assert.Null(sut.SuccessMessage);
        _submissions.Verify(s => s.UpdateTissueAsync(It.IsAny<Tissue>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static string ReadArchiveView(string view)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "src", "Histo.Web", "Pages", "Archive", view)))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "src", "Histo.Web", "Pages", "Archive", view));
    }

    [Theory]
    [InlineData("ArchiveTissues.cshtml")]
    [InlineData("ArchiveBlocks.cshtml")]
    public void Markup_ErrorSummaryRendersAtTopOfPage_BeforeTheSubmissionDetails(string view)
    {
        var markup = ReadArchiveView(view);

        var errorAt = markup.IndexOf("govuk-error-summary", StringComparison.Ordinal);
        var detailsAt = markup.IndexOf("<dl", StringComparison.Ordinal);

        Assert.True(errorAt >= 0, "Error summary not found.");
        Assert.True(errorAt < detailsAt, "Error summary must come before the submission details list.");
    }

    [Theory]
    [InlineData("ArchiveTissues.cshtml")]
    [InlineData("ArchiveBlocks.cshtml")]
    public void Markup_ScriptDoesNotOverwriteSubmittedValuesAfterAPostback(string view)
    {
        var markup = ReadArchiveView(view);

        Assert.Matches(@"if\s*\(\s*!posted\s*\)\s*applySelection\(\);", markup);
    }

    [Theory]
    [InlineData("ArchiveTissues.cshtml")]
    [InlineData("ArchiveBlocks.cshtml")]
    public void Markup_ScriptTicksTheRowCheckboxesWhenAllCameBackChecked(string view)
    {
        var markup = ReadArchiveView(view);

        Assert.Matches(@"if\s*\(\s*selectAll\.checked\s*\)\s*checkboxes\.forEach\(function\s*\(cb\)\s*\{\s*cb\.checked\s*=\s*true;", markup);
    }

    [Theory]
    [InlineData("ArchiveTissues.cshtml")]
    [InlineData("ArchiveBlocks.cshtml")]
    public void Markup_RowCheckboxesStayEnabledWhenAllIsTicked_AsInLegacy(string view)
    {
        var markup = ReadArchiveView(view);

        Assert.DoesNotContain("cb.disabled", markup);
    }

    [Theory]
    [InlineData("ArchiveTissues.cshtml")]
    [InlineData("ArchiveBlocks.cshtml")]
    public void Markup_UntickingARowUnticksAll_SoTheServerUsesTheRowsStillTicked(string view)
    {
        var markup = ReadArchiveView(view);

        Assert.Matches(@"selectAll\.checked\s*=\s*false", markup);
    }
}
