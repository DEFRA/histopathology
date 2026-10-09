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
/// Unit tests for <see cref="EditUserModel"/> — the inline "Edit" row restored from the legacy
/// UserMaintenance.aspx grid, covering load, field validation, email-uniqueness, the retired-area
/// visibility rule, and the open-redirect guard on <see cref="EditUserModel.SafeReturnUrl"/>.
/// </summary>
public class EditUserModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();

    public EditUserModelTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _lookups.Setup(l => l.GetUserGroupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Name = "DEFRA Data Entry" }]);
        _lookups.Setup(l => l.GetUserAreasAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 5, Name = "Histopath" }]);
    }

    private EditUserModel CreateSut()
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

    private static User SampleUser() => new() { UserID = 1, Name = "Alice", Email = "alice@example.com", GroupCode = 1, AreaCode = 5, Active = true };

    // ── OnGetAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_UserFound_PrePopulatesFieldsFromUser()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[SampleUser()]);
        var sut = CreateSut();
        sut.UserId = 1;

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Alice", sut.Name);
        Assert.Equal("alice@example.com", sut.Email);
        Assert.Equal(1, sut.GroupCode);
        Assert.Equal(5, sut.AreaCode);
        Assert.True(sut.Active);
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

    [Fact]
    public async Task OnGetAsync_UserHasRetiredArea_AddsItToAreaListSoItStillDisplays()
    {
        // Covers EnsureCurrentAreaVisibleAsync's "currentAreaCode not in active Areas" branch.
        var user = new User { UserID = 1, Name = "Bob", Email = "bob@example.com", GroupCode = 1, AreaCode = 99, Active = true };
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[user]);
        _lookups.Setup(l => l.GetUserAreasAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 99, Name = "Mouse Bioassay" }]);
        var sut = CreateSut();
        sut.UserId = 1;

        await sut.OnGetAsync();

        Assert.Contains(sut.Areas, a => a.ID == 99 && a.Name == "Mouse Bioassay");
    }

    [Fact]
    public async Task OnGetAsync_RetiredAreaNotFoundEvenInFullList_AreasUnchanged()
    {
        // Covers EnsureCurrentAreaVisibleAsync's "retired is null" no-op branch.
        var user = new User { UserID = 1, Name = "Bob", Email = "bob@example.com", GroupCode = 1, AreaCode = 999, Active = true };
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[user]);
        _lookups.Setup(l => l.GetUserAreasAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateSut();
        sut.UserId = 1;

        await sut.OnGetAsync();

        Assert.Single(sut.Areas); // unchanged from the active-only list set up in the constructor
    }

    // ── OnPostAsync validation ───────────────────────────────────────────────────

    [Fact]
    public async Task OnPostAsync_NameMissing_AddsErrorAndReturnsPage()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[SampleUser()]);
        var sut = CreateSut();
        sut.UserId = 1;
        sut.Name = "";
        sut.Email = "a@b.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter the user's name.", sut.Errors["Name"]);
        _users.Verify(u => u.UpdateUserAsync(It.IsAny<User>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_NameTooLong_AddsError()
    {
        var sut = CreateSut();
        sut.UserId = 1;
        sut.Name = new string('x', 36);
        sut.Email = "a@b.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[SampleUser()]);

        await sut.OnPostAsync();

        Assert.Equal("Name must be 35 characters or less.", sut.Errors["Name"]);
    }

    [Fact]
    public async Task OnPostAsync_EmailMissing_AddsError()
    {
        var sut = CreateSut();
        sut.UserId = 1;
        sut.Name = "Alice";
        sut.Email = "";
        sut.GroupCode = 1;
        sut.AreaCode = 5;
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[SampleUser()]);

        await sut.OnPostAsync();

        Assert.Equal("Enter the user's email.", sut.Errors["Email"]);
    }

    [Fact]
    public async Task OnPostAsync_EmailTooLong_AddsError()
    {
        var sut = CreateSut();
        sut.UserId = 1;
        sut.Name = "Alice";
        sut.Email = new string('x', 55) + "@b.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[SampleUser()]);

        await sut.OnPostAsync();

        Assert.Equal("Email must be 60 characters or less.", sut.Errors["Email"]);
    }

    [Fact]
    public async Task OnPostAsync_GroupNotSelected_AddsError()
    {
        var sut = CreateSut();
        sut.UserId = 1;
        sut.Name = "Alice";
        sut.Email = "a@b.com";
        sut.GroupCode = 0;
        sut.AreaCode = 5;
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[SampleUser()]);

        await sut.OnPostAsync();

        Assert.Equal("Select a user group.", sut.Errors["GroupCode"]);
    }

    [Fact]
    public async Task OnPostAsync_AreaNotSelected_AddsError()
    {
        var sut = CreateSut();
        sut.UserId = 1;
        sut.Name = "Alice";
        sut.Email = "a@b.com";
        sut.GroupCode = 1;
        sut.AreaCode = 0;
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[SampleUser()]);

        await sut.OnPostAsync();

        Assert.Equal("Select a user area.", sut.Errors["AreaCode"]);
    }

    [Fact]
    public async Task OnPostAsync_EmailBelongsToAnotherUser_AddsEmailError()
    {
        // Covers the EmailAlreadyExistsAsync branch — another user's row has the same email.
        var self = new User { UserID = 1, Name = "Alice", Email = "old@example.com", GroupCode = 1, AreaCode = 5 };
        var other = new User { UserID = 2, Name = "Bob", Email = "taken@example.com", GroupCode = 1, AreaCode = 5 };
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[self, other]);
        var sut = CreateSut();
        sut.UserId = 1;
        sut.Name = "Alice";
        sut.Email = "taken@example.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("A user with this email already exists.", sut.Errors["Email"]);
        _users.Verify(u => u.UpdateUserAsync(It.IsAny<User>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_EmailMatchesOwnRowOnly_IsAllowed()
    {
        // Covers EmailAlreadyExistsAsync excluding the user's own UserID from the duplicate check.
        var self = new User { UserID = 1, Name = "Alice", Email = "alice@example.com", GroupCode = 1, AreaCode = 5 };
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[self]);
        _users.Setup(u => u.UpdateUserAsync(It.IsAny<User>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.UserId = 1;
        sut.Name = "Alice";
        sut.Email = "alice@example.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        var result = await sut.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Empty(sut.Errors);
    }

    // ── OnPostAsync save ─────────────────────────────────────────────────────────

    [Fact]
    public async Task OnPostAsync_UpdateFails_SetsSaveErrorAndReturnsPage()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[SampleUser()]);
        _users.Setup(u => u.UpdateUserAsync(It.IsAny<User>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();
        sut.UserId = 1;
        sut.Name = "Alice";
        sut.Email = "alice@example.com";
        sut.GroupCode = 1;
        sut.AreaCode = 5;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to save changes. Please try again.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_HappyPath_UpdatesPreservesNtLoginAndRedirectsWithGridState()
    {
        var existing = new User { UserID = 1, Name = "Alice", Email = "alice@example.com", GroupCode = 1, AreaCode = 5, NtLogin = "DOMAIN\\alice" };
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[existing]);
        _users.Setup(u => u.UpdateUserAsync(It.IsAny<User>(), 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.UserId = 1;
        sut.Name = "  Alice Updated  ";
        sut.Email = "  alice.updated@example.com  ";
        sut.GroupCode = 2;
        sut.AreaCode = 5;
        sut.SortColumn = "Name";
        sut.ShowDeactivated = false;
        sut.ReturnUrl = "/Admin/UserMaintenance";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/UserMaintenance", redirect.PageName);
        _users.Verify(u => u.UpdateUserAsync(
            It.Is<User>(x => x.Name == "Alice Updated" && x.Email == "alice.updated@example.com" && x.NtLogin == "DOMAIN\\alice"),
            7, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("User 'Alice Updated' was updated.", sut.TempData["StatusMessage"]);
        Assert.Equal(1, sut.TempData["FocusUserId"]);
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

    [Fact]
    public void SafeReturnUrl_NoReturnUrlSet_ReturnsNull()
    {
        var sut = CreateSut();

        Assert.Null(sut.SafeReturnUrl);
    }
}
