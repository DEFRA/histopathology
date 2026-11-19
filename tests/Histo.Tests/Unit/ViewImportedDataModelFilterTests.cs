using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Filtering, sorting and export of <see cref="ViewImportedDataModel"/>.</summary>
public class ViewImportedDataModelFilterTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<ISubmissionService> _submissions = new();

    public ViewImportedDataModelFilterTests()
    {
        _lookups.Setup(l => l.GetImportedTablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Name = "ICC_Sub" }]);
        _submissions.Setup(s => s.GetImportedDataAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ImportedDataRow>)[
                new ImportedDataRow { SenderRef = "S2", HistologyRef = "H1", BlockRef = "01", Project = "Alpha", Species = "Ovine", Tissue = "Brain", Comments = "none", DateSubmitted = new DateTime(2020, 1, 2) },
                new ImportedDataRow { SenderRef = "S1", HistologyRef = "H2", BlockRef = "02", Project = "Beta", Species = "Bovine", Tissue = "Spleen", Comments = "urgent" },
                new ImportedDataRow { SenderRef = "S3", HistologyRef = "H3", BlockRef = "03", Project = "Gamma", Species = "Caprine", Tissue = "Obex", Comments = null }]);
    }

    private ViewImportedDataModel CreateSut() =>
        new(_session.Object, _lookups.Object, _submissions.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnGetAsync_NoTableSelected_LoadsTablesOnly()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Single(sut.Tables);
        Assert.Empty(sut.Results);
        _submissions.Verify(s => s.GetImportedDataAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData("   ", 3)]
    [InlineData("\"\"", 3)]
    [InlineData("alpha", 1)]
    [InlineData("OVINE alpha", 1)]
    [InlineData("alpha beta", 0)]
    [InlineData("\"urgent\"", 1)]
    [InlineData("S3", 1)]
    [InlineData("01", 1)]
    [InlineData("obex", 1)]
    [InlineData("H2", 1)]
    [InlineData("none", 1)]
    [InlineData("zzz", 0)]
    public async Task OnGetAsync_FilterTerms_AreAndedAcrossAllColumns(string? filter, int expectedRows)
    {
        var sut = CreateSut();
        sut.SelectedTable = "ICC_Sub";
        sut.Filter = filter;

        await sut.OnGetAsync();

        Assert.Equal(expectedRows, sut.Results.Count);
    }

    [Fact]
    public async Task OnGetAsync_FilterMatchesDateSubmitted()
    {
        var sut = CreateSut();
        sut.SelectedTable = "ICC_Sub";
        sut.Filter = new DateTime(2020, 1, 2).ToShortDateString();

        await sut.OnGetAsync();

        Assert.Equal("S2", Assert.Single(sut.Results).SenderRef);
    }

    [Theory]
    [InlineData("HistologyRef", "H3")]
    [InlineData("BlockRef", "03")]
    [InlineData("Project", "Gamma")]
    [InlineData("Species", "Ovine")]
    [InlineData("Tissue", "Spleen")]
    [InlineData("Comments", "urgent")]
    public async Task GetPagedResults_DescendingByColumn_PutsLargestValueFirst(string column, string expectedFirstValue)
    {
        var sut = CreateSut();
        sut.SelectedTable = "ICC_Sub";
        sut.SortColumn = column;
        sut.SortDesc = true;
        await sut.OnGetAsync();

        var first = sut.GetPagedResults()[0];

        var actual = column switch
        {
            "HistologyRef" => first.HistologyRef,
            "BlockRef" => first.BlockRef,
            "Project" => first.Project,
            "Species" => first.Species,
            "Tissue" => first.Tissue,
            _ => first.Comments,
        };
        Assert.Equal(expectedFirstValue, actual);
    }

    [Fact]
    public async Task GetPagedResults_DateSubmittedAscending_PutsRowWithoutDateFirst()
    {
        var sut = CreateSut();
        sut.SelectedTable = "ICC_Sub";
        sut.SortColumn = "DateSubmitted";
        await sut.OnGetAsync();

        Assert.Null(sut.GetPagedResults()[0].DateSubmitted);
        Assert.Equal("S2", sut.GetPagedResults()[^1].SenderRef);
    }

    [Fact]
    public async Task GetPagedResults_DefaultSort_OrdersBySenderRef()
    {
        var sut = CreateSut();
        sut.SelectedTable = "ICC_Sub";
        await sut.OnGetAsync();

        Assert.Equal(["S1", "S2", "S3"], sut.GetPagedResults().Select(r => r.SenderRef));
    }

    [Fact]
    public async Task OnGetExportExcelAsync_ExportsFilteredRowsAsWorkbook()
    {
        var sut = CreateSut();
        sut.SelectedTable = "ICC_Sub";
        sut.Filter = "alpha";

        var result = await sut.OnGetExportExcelAsync();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("ImportedData.xlsx", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }
}
