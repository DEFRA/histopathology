using System.Data;
using System.Security.Claims;
using Histo.Administration.Interfaces;
using Histo.Reporting.Services;
using Histo.Tests.Support;
using Histo.Web.Pages;
using Histo.Web.Pages.Errors;
using Histo.Web.Pages.Reports;
using Histo.Web.Pages.Shared;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Report PDF endpoints and the small error/access pages: redirect guards, headers and trace-id handling.</summary>
public class ReportAndErrorPagesTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static PageContext Context(ClaimsPrincipal? user = null)
    {
        var http = new DefaultHttpContext();
        if (user is not null) http.User = user;
        return new PageContext { HttpContext = http };
    }

    private static HistologyReportDataSetBuilder HistologyBuilder() =>
        new(new FakeDb(), new ConfigurationBuilder().Build(), Mock.Of<ILookupService>(), Mock.Of<IUserService>());

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task HistologyReport_NoUsableBatch_RedirectsToIndex(int? sessionBatch)
    {
        var session = new Mock<ISessionService>();
        session.SetupGet(s => s.BatchID).Returns(sessionBatch);
        var sut = new HistologyReportModel(session.Object, HistologyBuilder(), new ConfigurationBuilder().Build())
        {
            PageContext = Context(),
        };

        var result = await sut.OnGetAsync(Ct);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task SubmissionNotes_NoUsableBatch_RedirectsToIndex(int? sessionBatch)
    {
        var session = new Mock<ISessionService>();
        session.SetupGet(s => s.BatchID).Returns(sessionBatch);
        var sut = new SubmissionNotesModel(session.Object, new SubmissionNotesDataSetBuilder(new FakeDb()))
        {
            PageContext = Context(),
        };

        var result = await sut.OnGetAsync(Ct);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task QCNote_NonPositiveRef_RedirectsToIndex(int qcNoteRef)
    {
        var sut = new QCNoteModel(Mock.Of<ISessionService>(), new QCNoteDataSetBuilder(new FakeDb(), Mock.Of<ILookupService>()))
        {
            PageContext = Context(),
        };

        var result = await sut.OnGetAsync(qcNoteRef, Ct);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task SubmissionNotes_BatchInQueryString_ReturnsInlinePdf()
    {
        var db = new FakeDb
        {
            OnQuery = _ => new DataSet().Also(ds =>
            {
                for (var i = 0; i < 6; i++) ds.Tables.Add(new DataTable().Also(t => t.Columns.Add("ID", typeof(int))));
            }),
        };
        var sut = new SubmissionNotesModel(Mock.Of<ISessionService>(), new SubmissionNotesDataSetBuilder(db))
        {
            PageContext = Context(),
            BatchId = 42,
        };

        var result = await sut.OnGetAsync(Ct);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Contains("SubmissionNotes-42.pdf", sut.Response.Headers.ContentDisposition.ToString());
    }

    [Fact]
    public void AccessDenied_EmailClaim_IsExposed()
    {
        var sut = new AccessDeniedModel { PageContext = Context(Principal(new Claim(ClaimTypes.Email, "a@b.test"))) };

        sut.OnGet();

        Assert.Equal("a@b.test", sut.Email);
    }

    [Theory]
    [InlineData("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "wsfed@b.test")]
    [InlineData("preferred_username", "upn@b.test")]
    public void AccessDenied_FallbackClaims_AreUsed(string claimType, string value)
    {
        var sut = new AccessDeniedModel { PageContext = Context(Principal(new Claim(claimType, value))) };

        sut.OnGet();

        Assert.Equal(value, sut.Email);
    }

    [Fact]
    public void AccessDenied_NoEmailClaim_LeavesEmailNull()
    {
        var sut = new AccessDeniedModel { PageContext = Context(Principal()) };

        sut.OnGet();

        Assert.Null(sut.Email);
    }

    [Fact]
    public void Error_OnGet_SetsRequestIdFromTraceIdentifier()
    {
        var context = Context();
        context.HttpContext.TraceIdentifier = "trace-1";
        var sut = new ErrorModel { PageContext = context };

        sut.OnGet();

        Assert.False(string.IsNullOrEmpty(sut.RequestId));
    }

    [Fact]
    public void ServiceProblem_OnGet_LogsErrorWithTraceId()
    {
        var logger = new Mock<ILogger<ServiceProblemModel>>();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var sut = new ServiceProblemModel(logger.Object) { PageContext = Context() };

        sut.OnGet();

        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "test"));
}
