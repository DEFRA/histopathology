using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="AddUserModel"/> — the Add user form.</summary>
public class AddUserModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUrlHelper> _urlHelper = new();

    public AddUserModelTests()
    {
        _lookups.Setup(l => l.GetUserGroupsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private AddUserModel CreateSut() =>
        new(_session.Object, _users.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            Url = _urlHelper.Object,
            TempData = Mock.Of<ITempDataDictionary>(),
        };

    [Fact]
    public async Task OnGetAsync_SetsActiveTrueAndLoadsLookups()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.True(sut.Active);
    }

    [Theory]
    [InlineData("", "Name", 1, 1)]
    [InlineData("login", "", 1, 1)]
    [InlineData("login", "Name", 0, 1)]
    [InlineData("login", "Name", 1, 0)]
    public async Task OnPostAsync_MissingRequiredField_ReturnsPageWithErrors(string ntLogin, string name, int groupCode, int areaCode)
    {
        var sut = CreateSut();
        sut.NtLogin = ntLogin;
        sut.Name = name;
        sut.GroupCode = groupCode;
        sut.AreaCode = areaCode;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotEmpty(sut.Errors);
    }

    [Fact]
    public async Task OnPostAsync_CreateUserFails_ReturnsPageWithError()
    {
        _users.Setup(u => u.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();
        sut.NtLogin = "login";
        sut.Name = "Name";
        sut.GroupCode = 1;
        sut.AreaCode = 1;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Failed to save the new user. Please try again.", sut.Errors);
    }

    [Fact]
    public async Task OnPostAsync_Success_RedirectsToUserMaintenance()
    {
        _users.Setup(u => u.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.NtLogin = "login";
        sut.Name = "Name";
        sut.GroupCode = 1;
        sut.AreaCode = 1;

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/UserMaintenance", redirect.PageName);
    }

    [Fact]
    public void SafeReturnUrl_LocalUrl_IsReturned()
    {
        _urlHelper.Setup(u => u.IsLocalUrl("/Submissions/AddSubmission")).Returns(true);
        var sut = CreateSut();
        sut.ReturnUrl = "/Submissions/AddSubmission";

        Assert.Equal("/Submissions/AddSubmission", sut.SafeReturnUrl);
    }

    [Fact]
    public void SafeReturnUrl_ExternalUrl_IsBlocked()
    {
        _urlHelper.Setup(u => u.IsLocalUrl("https://evil.example.com")).Returns(false);
        var sut = CreateSut();
        sut.ReturnUrl = "https://evil.example.com";

        Assert.Null(sut.SafeReturnUrl);
    }

    [Fact]
    public void SafeReturnUrl_NullReturnUrl_IsNull()
    {
        var sut = CreateSut();
        sut.ReturnUrl = null;

        Assert.Null(sut.SafeReturnUrl);
    }
}
