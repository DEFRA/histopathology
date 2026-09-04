using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Pages.AuditLog;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="AuditLogByUserModel"/>.</summary>
public class AuditLogByUserModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly Mock<IUserService> _users = new();

    public AuditLogByUserModelTests()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
    }

    private AuditLogByUserModel CreateSut() =>
        new(_session.Object, _auditLog.Object, _users.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnPostAsync_NoUserSelected_ReturnsPageWithError()
    {
        var sut = CreateSut();
        sut.UserID = 0;
        sut.StartDate = DateTime.Today;
        sut.EndDate = DateTime.Today;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Select a user.", sut.Errors);
    }

    [Fact]
    public async Task OnPostAsync_MissingStartDate_ReturnsPageWithError()
    {
        var sut = CreateSut();
        sut.UserID = 5;
        sut.StartDate = null;
        sut.EndDate = DateTime.Today;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Enter a start date.", sut.Errors);
    }
}
