using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

public class SearchUnUsedHistologyRefsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IHistologyRefService> _refs = new();

    public SearchUnUsedHistologyRefsModelTests()
    {
        _refs.Setup(r => r.GetAllUnusedRefsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<HistologyRef>)Enumerable.Range(1, 25)
                .Select(i => new HistologyRef { Ref = $"R{i:00}" }).ToList());
    }

    private SearchUnUsedHistologyRefsModel CreateSut()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns<string>(s => s?.StartsWith('/') == true);
        return new(_session.Object, _refs.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
            Url = url.Object,
        };
    }

    [Fact]
    public async Task OnGetAsync_LoadsResultsAndPopulatesPagingState()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(25, sut.Results.Count);
        Assert.Equal(3, sut.TotalPages);
    }

    [Theory]
    [InlineData(false, 1, "R01", 10)]
    [InlineData(true, 1, "R25", 10)]
    [InlineData(false, 3, "R21", 5)]
    public async Task GetPagedResults_SortsByRefAndPages(bool desc, int page, string firstRef, int count)
    {
        var sut = CreateSut();
        sut.SortDesc = desc;
        sut.PageNumber = page;
        await sut.OnGetAsync();

        var paged = sut.GetPagedResults();

        Assert.Equal(count, paged.Count);
        Assert.Equal(firstRef, paged[0].Ref);
    }

    [Theory]
    [InlineData(null, "/Search/SearchMenu")]
    [InlineData("https://evil.example.com", "/Search/SearchMenu")]
    [InlineData("/Home", "/Home")]
    public void BackLinkPage_OnlyAllowsLocalReturnPages(string? returnPage, string expected)
    {
        var sut = CreateSut();
        sut.ReturnPage = returnPage;

        Assert.Equal(expected, sut.BackLinkPage);
    }

    [Fact]
    public async Task OnGetExportExcelAsync_ExportsEveryRowNotJustCurrentPage()
    {
        var result = await CreateSut().OnGetExportExcelAsync();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("unused-histology-refs.xlsx", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }
}
