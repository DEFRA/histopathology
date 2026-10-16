using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for three Search pages that previously had zero test coverage of any kind
/// (Tier 1 gaps): SearchArchiveLocation (three search modes sharing one page), SearchUnUsedHistologyRefs,
/// and ViewImportedData (legacy ICC_Sub browser). Single-page Moq tests — no cross-page state to chain.
/// </summary>
public class SearchTier1RegressionTests
{
    private readonly Mock<ISessionService> _session = new();

    private static Microsoft.AspNetCore.Mvc.RazorPages.PageContext NewPageContext() =>
        new() { ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary()), HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() };

    private SearchArchiveLocationModel CreateSearchArchiveLocation(ISubmissionService submissions, IBlockService blocks, ILookupService lookups) =>
        new(_session.Object, submissions, blocks, lookups) { PageContext = NewPageContext() };

    private SearchUnUsedHistologyRefsModel CreateUnusedRefs(IHistologyRefService refs) =>
        new(_session.Object, refs) { PageContext = NewPageContext() };

    private ViewImportedDataModel CreateViewImportedData(ILookupService lookups, ISubmissionService submissions) =>
        new(_session.Object, lookups, submissions) { PageContext = NewPageContext() };

    // ── SearchArchiveLocation ────────────────────────────────────────────────────

    [Fact]
    public async Task SearchArchiveLocation_BareFirstVisit_ShowsNoErrorAndDoesNotSearch()
    {
        var submissions = new Mock<ISubmissionService>(MockBehavior.Strict);
        var blocks = new Mock<IBlockService>(MockBehavior.Strict);
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateSearchArchiveLocation(submissions.Object, blocks.Object, lookups.Object);
        // Submitted defaults to false.

        await sut.OnGetAsync();

        Assert.Empty(sut.Errors);
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task SearchArchiveLocation_SubmittedWithNeitherRef_ShowsValidationError()
    {
        var submissions = new Mock<ISubmissionService>(MockBehavior.Strict);
        var blocks = new Mock<IBlockService>(MockBehavior.Strict);
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateSearchArchiveLocation(submissions.Object, blocks.Object, lookups.Object);
        sut.Submitted = true;

        await sut.OnGetAsync();

        Assert.Equal("Enter the Sender Ref or the Histology Ref.", sut.Errors["HistologyRef"]);
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task SearchArchiveLocation_TissueMode_CallsTissueArchiveOnly()
    {
        var submissions = new Mock<ISubmissionService>();
        submissions.Setup(s => s.GetTissueArchiveAsync("S001", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TissueArchiveInfo>)[new TissueArchiveInfo { BatchID = 1 }]);
        var blocks = new Mock<IBlockService>(MockBehavior.Strict);
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateSearchArchiveLocation(submissions.Object, blocks.Object, lookups.Object);
        sut.Submitted = true;
        sut.SenderRef = "S001";
        // ArchiveType defaults to "Tissue".

        await sut.OnGetAsync();

        Assert.True(sut.Searched);
        Assert.Single(sut.TissueResults);
        submissions.Verify(s => s.GetTissueArchiveAsync("S001", null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchArchiveLocation_BlockMode_CallsBlockArchiveOnly()
    {
        var submissions = new Mock<ISubmissionService>(MockBehavior.Strict);
        var blocks = new Mock<IBlockService>();
        blocks.Setup(b => b.GetBlockArchiveAsync("S002", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockArchiveInfo>)[new BlockArchiveInfo { ID = 1 }]);
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateSearchArchiveLocation(submissions.Object, blocks.Object, lookups.Object);
        sut.Submitted = true;
        sut.ArchiveType = "Block";
        sut.SenderRef = "S002";

        await sut.OnGetAsync();

        Assert.Single(sut.BlockResults);
        blocks.Verify(b => b.GetSlideArchiveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchArchiveLocation_SlideMode_CallsSlideArchiveOnly()
    {
        var submissions = new Mock<ISubmissionService>(MockBehavior.Strict);
        var blocks = new Mock<IBlockService>();
        blocks.Setup(b => b.GetSlideArchiveAsync("S003", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SlideArchiveInfo>)[new SlideArchiveInfo { BatchID = 1 }]);
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateSearchArchiveLocation(submissions.Object, blocks.Object, lookups.Object);
        sut.Submitted = true;
        sut.ArchiveType = "Slide";
        sut.SenderRef = "S003";

        await sut.OnGetAsync();

        Assert.Single(sut.SlideResults);
    }

    // ── SearchUnUsedHistologyRefs ────────────────────────────────────────────────

    [Fact]
    public async Task SearchUnUsedHistologyRefs_ListsAllUnusedRefs_SortedByRefByDefault()
    {
        var refs = new Mock<IHistologyRefService>();
        refs.Setup(r => r.GetAllUnusedRefsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)[
                new HistologyRef { Ref = "26/00200" },
                new HistologyRef { Ref = "26/00100" },
            ]);
        var sut = CreateUnusedRefs(refs.Object);

        await sut.OnGetAsync();

        Assert.Equal(2, sut.Results.Count);
        Assert.Equal("26/00100", sut.PagedResults[0].Ref);
    }

    // ── ViewImportedData ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ViewImportedData_NoTableSelected_DoesNotCallGetImportedData()
    {
        var submissions = new Mock<ISubmissionService>(MockBehavior.Strict);
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetImportedTablesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateViewImportedData(lookups.Object, submissions.Object);

        await sut.OnGetAsync();

        Assert.Empty(sut.Results);
    }

    [Fact]
    public async Task ViewImportedData_TableSelected_LoadsItsRows()
    {
        var submissions = new Mock<ISubmissionService>();
        submissions.Setup(s => s.GetImportedDataAsync("ICC_Sub1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ImportedDataRow>)[
                new ImportedDataRow { SenderRef = "S1", Species = "Ovine" },
                new ImportedDataRow { SenderRef = "S2", Species = "Bovine" },
            ]);
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetImportedTablesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateViewImportedData(lookups.Object, submissions.Object);
        sut.SelectedTable = "ICC_Sub1";

        await sut.OnGetAsync();

        Assert.Equal(2, sut.Results.Count);
    }

    [Fact]
    public async Task ViewImportedData_FilterTerm_OnlyMatchesRowsContainingItInAnyColumn()
    {
        var submissions = new Mock<ISubmissionService>();
        submissions.Setup(s => s.GetImportedDataAsync("ICC_Sub1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ImportedDataRow>)[
                new ImportedDataRow { SenderRef = "S1", Species = "Sheep" },
                new ImportedDataRow { SenderRef = "S2", Species = "Cattle" },
            ]);
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetImportedTablesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateViewImportedData(lookups.Object, submissions.Object);
        sut.SelectedTable = "ICC_Sub1";
        sut.Filter = "Sheep";

        await sut.OnGetAsync();

        var row = Assert.Single(sut.Results);
        Assert.Equal("S1", row.SenderRef);
    }

    [Fact]
    public async Task ViewImportedData_MultipleFilterTerms_RequiresAllTermsToMatch()
    {
        var submissions = new Mock<ISubmissionService>();
        submissions.Setup(s => s.GetImportedDataAsync("ICC_Sub1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ImportedDataRow>)[
                new ImportedDataRow { SenderRef = "S1", Species = "Ovine", Project = "ProjA" },
                new ImportedDataRow { SenderRef = "S2", Species = "Ovine", Project = "ProjB" },
            ]);
        var lookups = new Mock<ILookupService>();
        lookups.Setup(l => l.GetImportedTablesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateViewImportedData(lookups.Object, submissions.Object);
        sut.SelectedTable = "ICC_Sub1";
        sut.Filter = "Ovine ProjB"; // both terms must match -> only row S2 qualifies

        await sut.OnGetAsync();

        var row = Assert.Single(sut.Results);
        Assert.Equal("S2", row.SenderRef);
    }
}
