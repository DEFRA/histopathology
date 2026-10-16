using System.Security.Claims;
using Histo.Web.Pages;
using Histo.Web.Pages.AuditLog;
using Histo.Web.Pages.Bookings;
using Histo.Web.Pages.Errors;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the small root/standalone pages that previously had zero test coverage
/// of any kind (Tier 1 gaps): Index (home), AccessDenied, Errors/ServiceProblem, and the three
/// no-op menu pages (AuditLogMenu/BookingMenu/SearchMenu — smoke-tested only, since there is no
/// behaviour beyond "renders").
/// </summary>
public class MiscPagesRegressionTests
{
    private static PageContext NewPageContext(Microsoft.AspNetCore.Http.DefaultHttpContext? httpContext = null) => new()
    {
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        HttpContext = httpContext ?? new Microsoft.AspNetCore.Http.DefaultHttpContext(),
    };

    // ── Index (home) ─────────────────────────────────────────────────────────────

    [Fact]
    public void Index_OnGet_ClearsAllInProgressWorkflowStateFromSession()
    {
        var session = new Mock<ISessionService>();
        var sut = new IndexModel(session.Object) { PageContext = NewPageContext() };

        sut.OnGet();

        session.VerifySet(s => s.BatchID = null, Times.Once);
        session.VerifySet(s => s.BatchSubmissionID = null, Times.Once);
        session.VerifySet(s => s.AnimalID = null, Times.Once);
        session.VerifySet(s => s.BlockID = null, Times.Once);
    }

    // ── AccessDenied ─────────────────────────────────────────────────────────────

    [Fact]
    public void AccessDenied_EmailClaimPresent_IsUsedFirst()
    {
        var sut = CreateAccessDenied(
            new Claim(ClaimTypes.Email, "primary@example.com"),
            new Claim("preferred_username", "fallback@example.com"));

        sut.OnGet();

        Assert.Equal("primary@example.com", sut.Email);
    }

    [Fact]
    public void AccessDenied_NoEmailClaim_FallsBackToPreferredUsername()
    {
        var sut = CreateAccessDenied(new Claim("preferred_username", "upn@example.com"));

        sut.OnGet();

        Assert.Equal("upn@example.com", sut.Email);
    }

    [Fact]
    public void AccessDenied_NoClaimsAtAll_EmailIsNull()
    {
        var sut = CreateAccessDenied();

        sut.OnGet();

        Assert.Null(sut.Email);
    }

    private static AccessDeniedModel CreateAccessDenied(params Claim[] claims)
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")),
        };
        return new AccessDeniedModel { PageContext = NewPageContext(httpContext) };
    }

    // ── Errors/ServiceProblem ────────────────────────────────────────────────────

    [Fact]
    public void ServiceProblem_OnGet_DoesNotThrow()
    {
        var sut = new ServiceProblemModel(NullLogger<ServiceProblemModel>.Instance) { PageContext = NewPageContext() };

        var exception = Record.Exception(sut.OnGet);

        Assert.Null(exception);
    }

    // ── No-op menu pages (smoke only) ────────────────────────────────────────────

    [Fact]
    public void AuditLogMenu_OnGet_DoesNotThrow() =>
        Assert.Null(Record.Exception(() => new AuditLogMenuModel(Mock.Of<ISessionService>()) { PageContext = NewPageContext() }.OnGet()));

    [Fact]
    public void BookingMenu_OnGet_DoesNotThrow() =>
        Assert.Null(Record.Exception(() => new BookingMenuModel(Mock.Of<ISessionService>()) { PageContext = NewPageContext() }.OnGet()));

    [Fact]
    public void SearchMenu_OnGet_DoesNotThrow() =>
        Assert.Null(Record.Exception(() => new SearchMenuModel(Mock.Of<ISessionService>()) { PageContext = NewPageContext() }.OnGet()));
}
