using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="GridPageModel"/> — the abstract base class behind every sortable,
/// paginated grid page (<see cref="GridPageModel.SortBase"/> and the paging-clamp logic in
/// <see cref="GridPageModel.PopulateGridViewData"/>). Exercised through <see cref="SpeciesItemsModel"/>,
/// the simplest concrete subclass, since the base class itself cannot be instantiated.
/// </summary>
public class GridPageModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    private SpeciesItemsModel CreateSut(string queryString = "")
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(queryString))
            httpContext.Request.QueryString = new QueryString(queryString);

        return new SpeciesItemsModel(_session.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = httpContext,
            },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    // ── SortBase ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SortBase_ExcludesSortAndPagingAndHandlerParams_KeepsOtherFilters()
    {
        // Covers: the Where() predicate's four exclusion conditions, and the SelectMany/Join
        // that rebuilds the remaining query string.
        var sut = CreateSut("?SortColumn=Species&SortDesc=true&PageNumber=2&handler=Grid&Status=Active");

        Assert.Equal("Status=Active", sut.SortBase);
    }

    [Fact]
    public void SortBase_NoQueryString_ReturnsEmptyString()
    {
        var sut = CreateSut();

        Assert.Equal(string.Empty, sut.SortBase);
    }

    [Fact]
    public void SortBase_MultipleOtherFilters_JoinsThemWithAmpersand()
    {
        var sut = CreateSut("?Status=Active&Area=Histopath");

        Assert.Equal("Status=Active&Area=Histopath", sut.SortBase);
    }

    [Fact]
    public void SortBase_ValueNeedingEncoding_EscapesKeyAndValue()
    {
        // Covers: the Uri.EscapeDataString(...) calls in the SelectMany projection.
        var sut = CreateSut("?Name=A%20B");

        Assert.Equal("Name=A%20B", sut.SortBase);
    }

    // ── PopulateGridViewData paging clamp (via OnGetAsync -> PopulateGridViewData) ──

    [Fact]
    public async Task PopulateGridViewData_PageNumberBelowOne_ClampsToOne()
    {
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[new SpeciesItem { SpeciesID = 1, Species = "Bovine" }]);
        var sut = CreateSut();
        sut.PageNumber = 0;

        await sut.OnGetAsync();

        Assert.Equal(1, sut.PageNumber);
    }

    [Fact]
    public async Task PopulateGridViewData_PageNumberAboveTotalPages_ClampsToTotalPages()
    {
        var items = Enumerable.Range(1, 15).Select(n => new SpeciesItem { SpeciesID = n, Species = $"S{n}" }).ToList();
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)items);
        var sut = CreateSut();
        sut.PageNumber = 99; // 15 items / PageSize 10 -> 2 total pages

        await sut.OnGetAsync();

        Assert.Equal(2, sut.PageNumber);
        Assert.Equal(2, sut.TotalPages);
    }

    [Fact]
    public async Task PopulateGridViewData_NoItems_TotalPagesIsOneNotZero()
    {
        // Covers: CalculateTotalPages' "totalCount == 0 ? 1 : ..." branch.
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SpeciesItem>());
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(1, sut.TotalPages);
        Assert.Equal(1, sut.PageNumber);
    }

    [Fact]
    public async Task PopulateGridViewData_PageNumberWithinRange_IsNotChanged()
    {
        var items = Enumerable.Range(1, 15).Select(n => new SpeciesItem { SpeciesID = n, Species = $"S{n}" }).ToList();
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)items);
        var sut = CreateSut();
        sut.PageNumber = 2;

        await sut.OnGetAsync();

        Assert.Equal(2, sut.PageNumber);
    }
}
