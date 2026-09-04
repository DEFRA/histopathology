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

/// <summary>Unit tests for <see cref="EditUserModel"/> — the Edit user form.</summary>
public class EditUserModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUrlHelper> _urlHelper = new();

    public EditUserModelTests()
    {
        _session.Setup(s => s.UserID).Returns(99);
        _lookups.Setup(l => l.GetUserGroupsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private EditUserModel CreateSut() =>
        new(_session.Object, _users.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            Url = _urlHelper.Object,
            TempData = Mock.Of<ITempDataDictionary>(),
        };

    private static User MakeUser(int id = 1) => new()
    {
        UserID = id,
        NtLogin = "DOMAIN\\user",
        Name = "Test User",
        Email = "test@example.com",
        GroupCode = 2,
        AreaCode = 3,
        Active = true,
    };

    [Fact]
    public async Task OnGetAsync_UserFound_PopulatesFields()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[MakeUser()]);
        var sut = CreateSut();
        sut.UserId = 1;

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Test User", sut.Name);
        Assert.Equal(2, sut.GroupCode);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_RedirectsToUserMaintenance()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        var sut = CreateSut();
        sut.UserId = 999;

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/UserMaintenance", redirect.PageName);
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
    public async Task OnPostAsync_UpdateFails_ReturnsPageWithError()
    {
        _users.Setup(u => u.UpdateUserAsync(It.IsAny<User>(), 99, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();
        sut.NtLogin = "login";
        sut.Name = "Name";
        sut.GroupCode = 1;
        sut.AreaCode = 1;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Failed to save changes. Please try again.", sut.Errors);
    }

    [Fact]
    public async Task OnPostAsync_Success_RedirectsToUserMaintenance()
    {
        _users.Setup(u => u.UpdateUserAsync(It.IsAny<User>(), 99, It.IsAny<CancellationToken>())).ReturnsAsync(true);
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
    public async Task GroupSelectList_PreSelectsCurrentGroupCode()
    {
        _lookups.Setup(l => l.GetUserGroupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 2, Name = "Maintenance" }]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[MakeUser()]);
        var sut = CreateSut();
        sut.UserId = 1;
        await sut.OnGetAsync();

        var selected = sut.GroupSelectList.FirstOrDefault(i => i.Selected);

        Assert.NotNull(selected);
        Assert.Equal("2", selected!.Value);
    }
}
