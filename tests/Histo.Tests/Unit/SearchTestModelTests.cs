using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SearchTestModel"/>'s premium-charge cross-tab and submissions
/// drill-down, which replace the legacy <c>CountHistologysTestItems</c>/<c>CountAntibodesTestItems</c>/
/// <c>CountStainTestItems</c>/<c>CountHistologysTestBatch</c>/<c>CountAntibodesTestBatch</c>/
/// <c>CountStainTestBatch</c> stored procedures (confirmed live, 2026-09-22, not present in the
/// database) with direct queries against the underlying tables.
/// </summary>
public class SearchTestModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();

    private static readonly LookupItem[] PremiumCharges =
    [
        new() { Code = "TC 0008", Name = "TC 0008" },
        new() { Code = "TC 1401", Name = "TC 1401" },
    ];

    public SearchTestModelTests()
    {
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetPremiumChargesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)PremiumCharges);
    }

    private SearchTestModel CreateSut() =>
        new(_session.Object, _batches.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnPostAsync_PivotsCountsPerProjectAndPremiumCode_FillingZeroForMissingCombinations()
    {
        _batches.Setup(b => b.GetTestPremiumChargeCountsAsync(
                It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TestPremiumChargeCount>)
            [
                new() { ProjectDescription = "Project A", PremiumCode = "TC 0008", Count = 3 },
                new() { ProjectDescription = "Project A", PremiumCode = "TC 1401", Count = 2 },
                new() { ProjectDescription = "Project B", PremiumCode = "TC 0008", Count = 5 },
            ]);
        var sut = CreateSut();
        sut.SelectedHistology = ["1"];

        await sut.OnPostAsync();

        Assert.True(sut.Searched);
        Assert.Equal(2, sut.CrossTabRows.Count);

        var projectA = sut.CrossTabRows.Single(r => r.ProjectDescription == "Project A");
        Assert.Equal(3, projectA.CountsByPremiumCode["TC 0008"]);
        Assert.Equal(2, projectA.CountsByPremiumCode["TC 1401"]);
        Assert.Equal(5, projectA.Total);

        var projectB = sut.CrossTabRows.Single(r => r.ProjectDescription == "Project B");
        Assert.Equal(5, projectB.CountsByPremiumCode["TC 0008"]);
        Assert.Equal(0, projectB.CountsByPremiumCode["TC 1401"]); // filled with 0, not omitted
        Assert.Equal(5, projectB.Total);

        Assert.Equal(8, sut.CrossTabColumnTotals["TC 0008"]);
        Assert.Equal(2, sut.CrossTabColumnTotals["TC 1401"]);
        Assert.Equal(10, sut.CrossTabGrandTotal);
    }

    [Fact]
    public async Task OnPostAnalyseSubmissionsAsync_GroupsByPremiumCode_WithDistinctSortedBatchIds()
    {
        _batches.Setup(b => b.GetTestPremiumChargeBatchesAsync(
                It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TestPremiumChargeBatchRef>)
            [
                new() { PremiumCode = "TC 1401", BatchID = 200 },
                new() { PremiumCode = "TC 0008", BatchID = 105 },
                new() { PremiumCode = "TC 0008", BatchID = 101 },
                new() { PremiumCode = "TC 0008", BatchID = 101 }, // duplicate — must be deduplicated
            ]);
        var sut = CreateSut();
        sut.SelectedAntibodies = ["Hantavirus"];

        await sut.OnPostAnalyseSubmissionsAsync();

        Assert.True(sut.SubmissionsSearched);
        Assert.Equal(2, sut.SubmissionGroups.Count);

        var tc0008 = sut.SubmissionGroups.Single(g => g.PremiumCode == "TC 0008");
        Assert.Equal([101, 105], tc0008.BatchIds); // deduplicated and sorted ascending

        var tc1401 = sut.SubmissionGroups.Single(g => g.PremiumCode == "TC 1401");
        Assert.Equal([200], tc1401.BatchIds);
    }

    [Fact]
    public async Task OnPostAsync_NoTestCodesSelected_QueriesWithEmptyListsAndShowsNoResults()
    {
        _batches.Setup(b => b.GetTestPremiumChargeCountsAsync(
                It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TestPremiumChargeCount>)[]);
        var sut = CreateSut();

        await sut.OnPostAsync();

        Assert.True(sut.Searched);
        Assert.Empty(sut.CrossTabRows);
        Assert.Equal(0, sut.CrossTabGrandTotal);
    }
}
