using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

public class SearchArchiveLocationModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ILookupService> _lookups = new();

    public SearchArchiveLocationModelTests()
    {
        _lookups.Setup(l => l.GetLookupDataAsync(16, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Name = "Store A" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(9, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 2, Name = "Brain" }, new LookupItem { ID = 3, Name = "Spleen" }]);
        _submissions.Setup(s => s.GetTissueArchiveAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TissueArchiveInfo>)[new TissueArchiveInfo { BatchID = 1, TissueDescription = "Brain", NoPieces = 2 }]);
        _blocks.Setup(b => b.GetBlockArchiveAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockArchiveInfo>)[new BlockArchiveInfo { ID = 4, BlockID = 5, BlockRef = "01" }]);
        _blocks.Setup(b => b.GetSlideArchiveAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SlideArchiveInfo>)[new SlideArchiveInfo { BatchID = 6, BlockRef = "02" }]);
    }

    private SearchArchiveLocationModel CreateSut()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns<string>(s => s?.StartsWith('/') == true);
        return new(_session.Object, _submissions.Object, _blocks.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            Url = url.Object,
        };
    }

    [Fact]
    public async Task OnGetAsync_NotSubmitted_LoadsLookupsOnly()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.False(sut.Searched);
        Assert.Single(sut.ArchiveLocations);
        Assert.Equal(2, sut.Tissues.Count);
        _submissions.Verify(s => s.GetTissueArchiveAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", "")]
    public async Task OnGetAsync_SubmittedWithoutRefs_AddsValidationErrorAndDoesNotSearch(string? senderRef, string? histologyRef)
    {
        var sut = CreateSut();
        sut.Submitted = true;
        sut.SenderRef = senderRef;
        sut.HistologyRef = histologyRef;

        await sut.OnGetAsync();

        Assert.False(sut.Searched);
        Assert.True(sut.Errors.ContainsKey(nameof(sut.HistologyRef)));
    }

    [Fact]
    public async Task OnGetAsync_TissueMode_SearchesTissuesWithBlankFiltersAsNull()
    {
        var sut = CreateSut();
        sut.Submitted = true;
        sut.ArchiveType = "Tissue";
        sut.SenderRef = "C1";
        sut.ArchiveLocation = " ";
        sut.TissueCode = "";

        await sut.OnGetAsync();

        Assert.True(sut.Searched);
        Assert.Single(sut.TissueResults);
        _submissions.Verify(s => s.GetTissueArchiveAsync("C1", null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_BlockMode_SearchesBlocks()
    {
        var sut = CreateSut();
        sut.Submitted = true;
        sut.ArchiveType = "Block";
        sut.HistologyRef = "H1";
        sut.BlockRef = "01";
        sut.ArchiveLocation = "Store A";

        await sut.OnGetAsync();

        Assert.Single(sut.BlockResults);
        _blocks.Verify(b => b.GetBlockArchiveAsync(null, "H1", "01", "Store A", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_SlideMode_SearchesSlides()
    {
        var sut = CreateSut();
        sut.Submitted = true;
        sut.ArchiveType = "Slide";
        sut.SenderRef = "C1";

        await sut.OnGetAsync();

        Assert.Single(sut.SlideResults);
        _blocks.Verify(b => b.GetSlideArchiveAsync("C1", null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null, "/Search/SearchMenu")]
    [InlineData("", "/Search/SearchMenu")]
    [InlineData("https://evil.example.com", "/Search/SearchMenu")]
    [InlineData("/Home", "/Home")]
    public void BackLinkPage_OnlyAllowsLocalReturnPages(string? returnPage, string expected)
    {
        var sut = CreateSut();
        sut.ReturnPage = returnPage;

        Assert.Equal(expected, sut.BackLinkPage);
    }

    [Theory]
    [InlineData("Tissue", "Animal Tissue Archive.xlsx")]
    [InlineData("Block", "Animal Block Archive.xlsx")]
    [InlineData("Slide", "Animal Slide Archive.xlsx")]
    public async Task OnGetExportExcelAsync_ReturnsWorkbookForSelectedMode(string archiveType, string fileName)
    {
        var sut = CreateSut();
        sut.ArchiveType = archiveType;
        sut.SenderRef = "C1";

        var result = await sut.OnGetExportExcelAsync();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(fileName, file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }
}
