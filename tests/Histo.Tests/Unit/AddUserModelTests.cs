using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="AddUserModel"/> — the "Add new" row restored from the legacy
/// UserMaintenance.aspx grid, covering load, field validation, email-uniqueness, the
/// post-save focus-row lookup, and the open-redirect guard.
/// </summary>
public class AddUserModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();

    public AddUserModelTests()
    {
        _lookups.Setup(l => l.GetUserGroupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Name = "DEFRA Data Entry" }]);
        _lookups.Setup(l => l.GetUserAreasAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 5, Name = "Histopath" }]);
    }

    private AddUserModel CreateSut()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns<string>(s => s?.StartsWith('/') == true);

        return new(_session.Object, _users.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            Url = url.Object,
        };
    }

    // ── OnGetAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_LoadsGroupsAndAreasAndDefaultsActiveToTrue()
    {
        var sut = CreateSut();
        sut.Active = false;

        await sut.OnGetAsync();

        Assert.True(sut.Active);
        Assert.Single(sut.Groups);
        Assert.Single(sut.Areas);
    }

    // ── OnPostAsync validation ───────────────────────────────────────────────────

    [Fact]
    public async Task OnPostAsync_NameMissing_AddsErrorAndDoesNotCreate()
    {
        var sut = CreateSut();
        sut.Email = "a@b.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter the user's name.", sut.Errors["Name"]);
        _users.Verify(u => u.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_NameTooLong_AddsError()
    {
        var sut = CreateSut();
        sut.Name = new string('x', 36);
        sut.Email = "a@b.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        await sut.OnPostAsync();

        Assert.Equal("Name must be 35 characters or less.", sut.Errors["Name"]);
    }

    [Fact]
    public async Task OnPostAsync_EmailMissing_AddsError()
    {
        var sut = CreateSut();
        sut.Name = "Alice";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        await sut.OnPostAsync();

        Assert.Equal("Enter the user's email.", sut.Errors["Email"]);
    }

    [Fact]
    public async Task OnPostAsync_EmailTooLong_AddsError()
    {
        var sut = CreateSut();
        sut.Name = "Alice";
        sut.Email = new string('x', 55) + "@b.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        await sut.OnPostAsync();

        Assert.Equal("Email must be 60 characters or less.", sut.Errors["Email"]);
    }

    [Fact]
    public async Task OnPostAsync_GroupNotSelected_AddsError()
    {
        var sut = CreateSut();
        sut.Name = "Alice";
        sut.Email = "a@b.com";
        sut.AreaCode = 5;

        await sut.OnPostAsync();

        Assert.Equal("Select a user group.", sut.Errors["GroupCode"]);
    }

    [Fact]
    public async Task OnPostAsync_AreaNotSelected_AddsError()
    {
        var sut = CreateSut();
        sut.Name = "Alice";
        sut.Email = "a@b.com";
        sut.GroupCode = 1;

        await sut.OnPostAsync();

        Assert.Equal("Select a user area.", sut.Errors["AreaCode"]);
    }

    [Fact]
    public async Task OnPostAsync_EmailAlreadyExists_AddsErrorAndDoesNotCreate()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<User>)[new User { UserID = 1, Email = "taken@example.com" }]);
        var sut = CreateSut();
        sut.Name = "Alice";
        sut.Email = "taken@example.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("A user with this email already exists.", sut.Errors["Email"]);
        _users.Verify(u => u.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── OnPostAsync save ─────────────────────────────────────────────────────────

    [Fact]
    public async Task OnPostAsync_CreateFails_SetsSaveErrorAndReturnsPage()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _users.Setup(u => u.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();
        sut.Name = "Alice";
        sut.Email = "alice@example.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to save the new user. Please try again.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_HappyPath_CreatesWithNullNtLoginAndRedirectsWithGridState()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _users.Setup(u => u.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Name = "  Alice  ";
        sut.Email = "  Alice@Example.com  ";
        sut.GroupCode = 1;
        sut.AreaCode = 5;
        sut.SortColumn = "Name";
        sut.ReturnUrl = "/Admin/UserMaintenance";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/UserMaintenance", redirect.PageName);
        _users.Verify(u => u.CreateUserAsync(
            It.Is<User>(x => x.NtLogin == null && x.Name == "Alice" && x.Email == "Alice@Example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("User 'Alice' was added.", sut.TempData["StatusMessage"]);
    }

    [Fact]
    public async Task OnPostAsync_HappyPath_ResolvesNewlyCreatedUserIdForGridFocus()
    {
        // AddUser SP returns no identity, so the page re-reads the row by email to find its ID.
        _users.SetupSequence(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<User>)[]) // pre-create uniqueness check
            .ReturnsAsync((IReadOnlyList<User>)[new User { UserID = 42, Email = "alice@example.com" }]); // post-create lookup
        _users.Setup(u => u.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Name = "Alice";
        sut.Email = "alice@example.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        await sut.OnPostAsync();

        Assert.Equal(42, sut.TempData["FocusUserId"]);
    }

    [Fact]
    public async Task OnPostAsync_CreatedUserNotFoundOnReread_FocusUserIdNotSet()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _users.Setup(u => u.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Name = "Alice";
        sut.Email = "alice@example.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        await sut.OnPostAsync();

        Assert.Null(sut.TempData["FocusUserId"]);
    }

    // ── SafeReturnUrl ────────────────────────────────────────────────────────────

    [Fact]
    public void SafeReturnUrl_LocalUrl_IsReturnedAsIs()
    {
        var sut = CreateSut();
        sut.ReturnUrl = "/Admin/UserMaintenance";

        Assert.Equal("/Admin/UserMaintenance", sut.SafeReturnUrl);
    }

    [Fact]
    public void SafeReturnUrl_ExternalUrl_ReturnsNull()
    {
        var sut = CreateSut();
        sut.ReturnUrl = "https://evil.example.com";

        Assert.Null(sut.SafeReturnUrl);
    }
}
