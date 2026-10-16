using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Infrastructure;
using Histo.Reporting.Reports;
using Histo.Reporting.Services;
using Histo.Web.Pages.Reports;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the Reports pages (scenario REPORT-01..03 in
/// .github/agents/histo-testing-agent.agent.md §5). The data-set builders query the database
/// directly via Dapper over <see cref="IDbConnectionFactory"/> rather than a mockable repository
/// interface, and the renderer/builder pairs already have thorough Unit/ coverage of their output
/// shape — so the regression value specific to the PageModel layer is its routing guard: an
/// invalid/missing report ID must redirect away *before* any database work is attempted, not
/// after a failed query. That's proven here by making the mocked connection factory throw if it's
/// ever actually called.
/// </summary>
public class ReportsRegressionTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IDbConnectionFactory> _db = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<IConfiguration> _config = new();

    public ReportsRegressionTests()
    {
        // Proves the PageModel's guard short-circuits before any DB access is attempted.
        _db.Setup(d => d.CreateConnection()).Throws(new InvalidOperationException("CreateConnection should not be called when the report ID is invalid."));
    }

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private HistologyReportModel CreateHistologyReport() => new(
        _session.Object,
        new HistologyReportDataSetBuilder(_db.Object, _config.Object, _lookups.Object, _users.Object),
        new HistologyReportRenderer(),
        _config.Object)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private QCNoteModel CreateQCNoteReport() => new(
        _session.Object,
        new QCNoteDataSetBuilder(_db.Object, _lookups.Object),
        new QCNoteRenderer())
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private SubmissionNotesModel CreateSubmissionNotesReport() => new(
        _session.Object,
        new SubmissionNotesDataSetBuilder(_db.Object),
        new SubmissionNotesRenderer())
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    [Fact]
    public async Task HistologyReport_NoBatchSelected_RedirectsWithoutTouchingTheDatabase()
    {
        _session.Setup(s => s.BatchID).Returns((int?)null);
        var sut = CreateHistologyReport();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        _db.Verify(d => d.CreateConnection(), Times.Never);
    }

    [Fact]
    public async Task HistologyReport_BatchIdZero_RedirectsWithoutTouchingTheDatabase()
    {
        _session.Setup(s => s.BatchID).Returns(0);
        var sut = CreateHistologyReport();

        var result = await sut.OnGetAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _db.Verify(d => d.CreateConnection(), Times.Never);
    }

    [Fact]
    public async Task QCNoteReport_InvalidRef_RedirectsWithoutTouchingTheDatabase()
    {
        var sut = CreateQCNoteReport();

        var result = await sut.OnGetAsync(qcNoteRef: 0);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        _db.Verify(d => d.CreateConnection(), Times.Never);
    }

    [Fact]
    public async Task QCNoteReport_NegativeRef_RedirectsWithoutTouchingTheDatabase()
    {
        var sut = CreateQCNoteReport();

        var result = await sut.OnGetAsync(qcNoteRef: -5);

        Assert.IsType<RedirectToPageResult>(result);
        _db.Verify(d => d.CreateConnection(), Times.Never);
    }

    [Fact]
    public async Task SubmissionNotesReport_NoBatchSelected_RedirectsWithoutTouchingTheDatabase()
    {
        _session.Setup(s => s.BatchID).Returns((int?)null);
        var sut = CreateSubmissionNotesReport();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        _db.Verify(d => d.CreateConnection(), Times.Never);
    }
}
