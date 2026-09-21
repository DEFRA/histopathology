using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using ExcelDataReader;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ViewSubmissionsModel"/>. Confirmed live: legacy's <c>ViewSubmissions</c>
/// has no "Submitted area" filter and does not restrict results by the logged-in user's area — a
/// Rejected-status record belonging to a different VLA still appeared in legacy's 2-row result. A
/// previous fix in this repo wrongly added both; this regression test guards against reintroducing
/// either.
/// </summary>
public class ViewSubmissionsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();

    public ViewSubmissionsModelTests()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[]);
    }

    private ViewSubmissionsModel CreateSut() =>
        new(_session.Object, _batches.Object, _users.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnGetAsync_NeverAppliesAreaRestriction_RegardlessOfCallerRole()
    {
        // Non-Histo/non-Maintenance role — must NOT be silently scoped to their own area.
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.IsMaintenance).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(7);
        var sut = CreateSut();

        await sut.OnGetAsync();

        _batches.Verify(b => b.SearchAsync(
            It.Is<BatchSearchCriteria>(c => c.SubmittedArea == null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void HasNoSubmittedAreaOrAreaRestrictionProperty()
    {
        // Legacy ViewSubmissions has no "Submitted area" search field at all (confirmed live);
        // this model must not expose one, nor any area-restriction concept, for users to filter by.
        var modelType = typeof(ViewSubmissionsModel);
        Assert.Null(modelType.GetProperty("SubmittedArea"));
        Assert.Null(modelType.GetProperty("IsAreaRestricted"));
    }

    [Fact]
    public async Task OnPostExportCsvAsync_ReproducesLegacy16ColumnExportTable()
    {
        // Legacy lbExportExcel_Click (SearchSubmissions.aspx.vb / ViewSubmissions.aspx.vb, identical
        // code in both) builds this exact 16-column table, not the 6-9 on-screen grid columns.
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)
            [
                new BatchSearchResult
                {
                    ID = 12345, ProjectDescription = "Project X", ContactDescription = "Dr Smith",
                    Species = "Ovine", BatchDate = new DateTime(2026, 1, 1), BatchType = "0",
                    SubmittedBy = "J Bloggs", SafeToHandle = "1", DateReceived = new DateTime(2026, 1, 2),
                    ReceivedTime = "09:30", ReceivedBy = "R Jones", OtherSubmittedBy = "Other Person",
                    Comments = "Test comment", CustomerReceivedDate = new DateTime(2026, 1, 3),
                    Status = "3", DateCompleted = new DateTime(2026, 1, 10),
                },
            ]);
        var sut = CreateSut();

        var result = await sut.OnPostExportCsvAsync();

        var file = Assert.IsType<Microsoft.AspNetCore.Mvc.FileContentResult>(result);
        using var stream = new MemoryStream(file.FileContents);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var table = reader.AsDataSet().Tables[0];

        Assert.Equal(2, table.Rows.Count); // header + 1 data row
        Assert.Equal(
            new[]
            {
                "Submission Number", "Project/Contract", "Pathologist", "Species", "Submitted Date",
                "Submission Type", "Submitted By", "Safe To Handle", "Received Date", "Time Received/Rejected",
                "Received By", "Other Submitted By", "Comments", "Customer Received Date", "Status", "Completed Date",
            },
            table.Rows[0].ItemArray.Select(v => v?.ToString()));
        Assert.Equal("TSE", table.Rows[1][5]);
        Assert.Equal("Yes", table.Rows[1][7]);
    }
}
