using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Tier 2 gap closure: regression suite for SearchTest (premium-charge cross-tab/submissions
/// analysis) and ViewSamples (standalone animal/tissue search), which previously only had
/// atomic Unit/ coverage. Plain Moq — both pages are GET-based with no cross-page shared state.
/// </summary>
public class SearchTestAndViewSamplesRegressionTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<ILookupService> _lookups = new();

    public SearchTestAndViewSamplesRegressionTests()
    {
        _session.SetupAllProperties();
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetPremiumChargesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private static PageContext NewPageContext() => new()
    {
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
    };

    private SearchTestModel CreateSearchTestSut() => new(_session.Object, _batches.Object, _lookups.Object) { PageContext = NewPageContext(), Url = Mock.Of<IUrlHelper>() };
    private ViewSamplesModel CreateViewSamplesSut() => new(_session.Object, _submissions.Object, _lookups.Object) { PageContext = NewPageContext(), Url = Mock.Of<IUrlHelper>() };

    // ── SearchTest ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchTest_BareFirstVisit_ShowsEmptyFormWithoutSearching()
    {
        var sut = CreateSearchTestSut();
        sut.Submitted = false;

        await sut.OnGetAsync();

        Assert.False(sut.Searched);
        _batches.Verify(b => b.GetTestPremiumChargeCountsAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchTest_Submitted_BuildsCrossTabGroupedByProjectSummingAcrossTestTypes()
    {
        _batches.Setup(b => b.GetTestPremiumChargeCountsAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TestPremiumChargeCount>)[
                new TestPremiumChargeCount { ProjectDescription = "ProjA", PremiumCode = "TC 1401", Count = 3 },
                new TestPremiumChargeCount { ProjectDescription = "ProjA", PremiumCode = "TC 1401", Count = 2 }, // second test type, same project+code: summed
            ]);
        _lookups.Setup(l => l.GetPremiumChargesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "TC1401", Name = "TC 1401" },
        ]);
        var sut = CreateSearchTestSut();
        sut.Submitted = true;

        await sut.OnGetAsync();

        Assert.True(sut.Searched);
        var row = Assert.Single(sut.CrossTabRows);
        Assert.Equal("ProjA", row.ProjectDescription);
        Assert.Equal(5, row.Total);
        Assert.Equal(5, sut.CrossTabGrandTotal);
    }

    [Fact]
    public async Task SearchTest_AnalyseSubmissions_SavesReturnPageWithFullQueryString()
    {
        var sut = CreateSearchTestSut();
        var httpContext = (Microsoft.AspNetCore.Http.DefaultHttpContext)sut.PageContext.HttpContext;
        httpContext.Request.Path = "/Search/SearchTest";
        httpContext.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?projectDescription=ProjA");
        _batches.Setup(b => b.GetTestPremiumChargeBatchesAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TestPremiumChargeBatchRef>)[]);

        await sut.OnGetAnalyseSubmissionsAsync();

        Assert.True(sut.SubmissionsSearched);
        Assert.Equal("/Search/SearchTest?projectDescription=ProjA", _session.Object.ReturnPage);
    }

    [Fact]
    public void SearchTest_BackLinkPage_RejectsExternalUrl_FallsBackToSearchMenu()
    {
        var sut = CreateSearchTestSut();
        sut.ReturnPage = "https://evil.example.com/phish";

        Assert.Equal("/Search/SearchMenu", sut.BackLinkPage);
    }

    // ── ViewSamples ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ViewSamples_BothRefsBlank_IsRejected()
    {
        var sut = CreateViewSamplesSut();
        sut.Submitted = true;
        sut.SenderRef = null;
        sut.HistologyRef = null;

        await sut.OnGetAsync();

        Assert.False(sut.Searched);
        Assert.Equal("Enter the Sender Ref or the Histology Ref.", sut.Errors[nameof(sut.SenderRef)]);
    }

    [Fact]
    public async Task ViewSamples_BothRefsGiven_IsRejected()
    {
        var sut = CreateViewSamplesSut();
        sut.Submitted = true;
        sut.SenderRef = "S1";
        sut.HistologyRef = "H1";

        await sut.OnGetAsync();

        Assert.False(sut.Searched);
        Assert.True(sut.Errors.ContainsKey(nameof(sut.SenderRef)));
    }

    [Fact]
    public async Task ViewSamples_SenderRefOnly_TissueMode_ResolvesOtherFieldLabelFromFirstResult()
    {
        _submissions.Setup(s => s.GetAnimalTissuesAsync("S1", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AnimalTissueSearchResult>)[
                new AnimalTissueSearchResult { ID = 1, SenderRef = "S1", HistologyRef = "H99" },
            ]);
        var sut = CreateViewSamplesSut();
        sut.Submitted = true;
        sut.SenderRef = "S1";
        sut.Mode = "Tissue";

        await sut.OnGetAsync();

        Assert.True(sut.Searched);
        Assert.Single(sut.Results);
        Assert.Equal("Histology Ref: H99", sut.OtherFieldLabel);
    }

    [Fact]
    public async Task ViewSamples_HistologyRefOnly_BlockMode_CallsBlockTissueSearch()
    {
        _submissions.Setup(s => s.GetAnimalBlockTissuesAsync(null, "H1", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AnimalTissueSearchResult>)[
                new AnimalTissueSearchResult { ID = 2, SenderRef = "S77", HistologyRef = "H1" },
            ]);
        var sut = CreateViewSamplesSut();
        sut.Submitted = true;
        sut.HistologyRef = "H1";
        sut.Mode = "Block";

        await sut.OnGetAsync();

        Assert.True(sut.Searched);
        Assert.Equal("Sender Ref: S77", sut.OtherFieldLabel);
    }

    [Fact]
    public void ViewSamples_BackLinkPage_RejectsExternalUrl_FallsBackToSearchMenu()
    {
        var sut = CreateViewSamplesSut();
        sut.ReturnPage = "//evil.example.com/phish";

        Assert.Equal("/Search/SearchMenu", sut.BackLinkPage);
    }
}
