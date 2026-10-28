using Dapper;
using Histo.AuditLog;
using Histo.AuditLog.Models;
using Histo.Administration.Interfaces;
using Histo.Histology.Interfaces;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages;
using Histo.Web.Pages.Archive;
using Histo.Web.Pages.Search;
using Histo.Web.Pages.Shared;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Small shared page helpers, view-model records and select-action links.</summary>
public class WebPageHelpersCoverageTests
{
    private sealed class ArchivePageProbe(ISessionService session)
        : ArchivePageModelBase(session, Mock.Of<IBatchService>(), Mock.Of<ILookupService>(), Mock.Of<IUserService>())
    {
        public int? Requested => RequestedBatchId;
    }

    private static string Render(Microsoft.AspNetCore.Html.IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        return writer.ToString();
    }

    [Fact]
    public void SummaryListRow_RendersKeyAndValueWithGovukClasses_AndEncodesText()
    {
        var html = Render(SummaryList.Row("Name <b>", "A & B"));

        Assert.Equal(
            "<div class=\"govuk-summary-list__row\"><dt class=\"govuk-summary-list__key\">Name &lt;b&gt;</dt>"
            + "<dd class=\"govuk-summary-list__value\">A &amp; B</dd></div>",
            html);
    }

    [Fact]
    public void SummaryListRow_NullValue_RendersEmptyValueCell()
    {
        var html = Render(SummaryList.Row("Key", null));

        Assert.EndsWith("<dd class=\"govuk-summary-list__value\"></dd></div>", html);
    }

    [Fact]
    public void BuildSelectFormAction_EncodesSortColumnAndIncludesState()
    {
        var action = BatchSearchGridHelper.BuildSelectFormAction("Sp&ecies x", true, 3);

        Assert.Equal("?handler=Select&SortColumn=Sp%26ecies%20x&SortDesc=true&PageNumber=3", action);
    }

    [Fact]
    public void BuildSelectFormAction_NoSortColumn_UsesEmptyValueAndAscending()
    {
        Assert.Equal("?handler=Select&SortColumn=&SortDesc=false&PageNumber=1", BatchSearchGridHelper.BuildSelectFormAction(null, false, 1));
    }

    [Fact]
    public void ArchiveRecords_KeepTheirValues()
    {
        var page = Mock.Of<IArchivePage>();
        var date = new DateTime(2026, 1, 2);

        var filter = new ArchiveFilter("f", "Label", ["a", "b"], "b");
        var checkbox = new ArchiveRowCheckbox(page, 4, "LOC", date, "note", "row 4");

        Assert.Equal(("f", "Label", "b"), (filter.Id, filter.Label, filter.Selected));
        Assert.Equal(["a", "b"], filter.Options);
        Assert.Same(page, checkbox.Page);
        Assert.Equal((4, "LOC", date, "note", "row 4"), (checkbox.Id, checkbox.ArchiveLocation, checkbox.ArchivedDate, checkbox.Comment, checkbox.Label));
    }

    [Fact]
    public void BlockGridRecords_KeepTheirValues()
    {
        var tissues = new List<Tissue> { new() { ID = 1 } };

        var cell = new BlockTissuesCell(tissues, code => code.ToUpperInvariant());
        var histology = new BlockHistologyCells(9, ["H1"], true);

        Assert.Same(tissues, cell.Tissues);
        Assert.Equal("ABC", cell.TissueName("abc"));
        Assert.Equal((9, true), (histology.BlockId, histology.IsTse));
        Assert.Equal(["H1"], histology.Codes);
    }

    [Fact]
    public void AuditLogTypeMap_RedirectsDateTimeColumnToChangedAt_AndLeavesOthersByName()
    {
        AuditLogDapperSetup.RegisterTypeMaps();

        var map = SqlMapper.GetTypeMap(typeof(AuditLogEntry));

        Assert.Equal(nameof(AuditLogEntry.ChangedAt), map.GetMember("datetime")!.Property!.Name);
        Assert.Equal(nameof(AuditLogEntry.UserName), map.GetMember("UserName")!.Property!.Name);
    }

    [Theory]
    [InlineData(7, 3, 7)]
    [InlineData(0, 3, 3)]
    [InlineData(null, 3, 3)]
    public void ArchivePage_RequestedBatchId_PrefersPositiveBatchIdOverSession(int? batchId, int sessionBatch, int expected)
    {
        var session = new Mock<ISessionService>();
        session.SetupGet(s => s.BatchID).Returns(sessionBatch);
        var sut = new ArchivePageProbe(session.Object) { BatchId = batchId };

        Assert.Equal(expected, sut.Requested);
    }

    [Fact]
    public void ArchivePage_BackLinkPage_UsesSessionReturnPageWhenSet_OtherwiseArchiveMenu()
    {
        var session = new Mock<ISessionService>();
        session.SetupProperty(s => s.ReturnPage, string.Empty);
        var sut = new ArchivePageProbe(session.Object);

        Assert.Equal("/Archive/ArchiveMenu", sut.BackLinkPage);

        session.Object.ReturnPage = "/Batches/BatchesForArchiving";

        Assert.Equal("/Batches/BatchesForArchiving", sut.BackLinkPage);
    }

    [Fact]
    public void ViewSubmissionsGridColumns_ListEveryColumnInOrderWithSortKeys()
    {
        Assert.Equal(
            ["ID", "ProjectDescription", "ContactDescription", "Species", "BatchDate", "DateReceived", "DateCompleted", "CustomerReceivedDate", "Status"],
            ViewSubmissionsModel.GridColumns.Select(c => c.Column));
        Assert.Equal("Species", ViewSubmissionsModel.GridColumns.Single(c => c.Column == "Species").Label);
        Assert.Equal("Status", ViewSubmissionsModel.GridColumns.Single(c => c.Column == "Status").Label);
    }

    [Fact]
    public void ViewSubmissions_SelectFormAction_CarriesSortAndPageState()
    {
        var sut = new ViewSubmissionsModel(
            Mock.Of<ISessionService>(), Mock.Of<IBatchService>(), Mock.Of<IUserService>(), Mock.Of<ILookupService>(),
            Mock.Of<IBlockService>(), Mock.Of<ISubmissionService>(), Mock.Of<IBlockTestService>())
        { SortColumn = "Species", SortDesc = true, PageNumber = 2 };

        Assert.Equal("?handler=Select&SortColumn=Species&SortDesc=true&PageNumber=2", sut.SelectFormAction);
    }

    private static SearchSubmissionsModel SearchSut(Mock<IBatchService> batches) =>
        new(Mock.Of<ISessionService>(), batches.Object, Mock.Of<IUserService>(), Mock.Of<ILookupService>(),
            Mock.Of<IBlockService>(), Mock.Of<ISubmissionService>(), Mock.Of<IBlockTestService>())
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new DefaultHttpContext(),
            },
        };

    [Fact]
    public void SearchSubmissions_SelectFormAction_CarriesSortAndPageState()
    {
        var sut = SearchSut(new Mock<IBatchService>());
        sut.SortColumn = "ID";
        sut.SortDesc = false;
        sut.PageNumber = 4;

        Assert.Equal("?handler=Select&SortColumn=ID&SortDesc=false&PageNumber=4", sut.SelectFormAction);
    }

    [Fact]
    public async Task SearchSubmissions_OnPostExportExcelAsync_ReturnsXlsxOfSearchResults()
    {
        var batches = new Mock<IBatchService>();
        batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[new BatchSearchResult { ID = 1, Status = "1" }]);
        var sut = SearchSut(batches);

        var result = await sut.OnPostExportExcelAsync();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("search-submissions.xlsx", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }
}
