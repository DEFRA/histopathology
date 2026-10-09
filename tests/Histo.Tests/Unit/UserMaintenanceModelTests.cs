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
/// Unit tests for <see cref="UserMaintenanceModel"/> — the User maintenance grid, covering the
/// active/deactivated filter, every sort-column branch, the Group/Area name resolution fallback
/// chain, focus-row page restoration, the AJAX grid partial, and the open-redirect guard.
/// </summary>
public class UserMaintenanceModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();

    public UserMaintenanceModelTests()
    {
        _lookups.Setup(l => l.GetUserGroupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Name = "DEFRA Data Entry" }]);
        _lookups.Setup(l => l.GetUserAreasAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 5, Name = "Histopath" }]);
    }

    private UserMaintenanceModel CreateSut()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns<string>(s => s?.StartsWith('/') == true);

        return new(_session.Object, _users.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = httpContext,
            },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
            Url = url.Object,
        };
    }

    // ── Active/deactivated filter ────────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_ShowDeactivatedTrue_IncludesInactiveUsers()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[
            new User { UserID = 1, Name = "Active", Active = true },
            new User { UserID = 2, Name = "Inactive", Active = false },
        ]);
        var sut = CreateSut();
        sut.ShowDeactivated = true;

        await sut.OnGetAsync();

        Assert.Equal(2, sut.TotalCount);
    }

    [Fact]
    public async Task OnGetAsync_ShowDeactivatedFalse_ExcludesInactiveUsers()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[
            new User { UserID = 1, Name = "Active", Active = true },
            new User { UserID = 2, Name = "Inactive", Active = false },
        ]);
        var sut = CreateSut();
        sut.ShowDeactivated = false;

        await sut.OnGetAsync();

        Assert.Equal(1, sut.TotalCount);
        Assert.Equal("Active", sut.GetPagedEntries()[0].Name);
    }

    [Fact]
    public async Task OnGetAsync_LookupThrows_SetsErrorMessageInsteadOfThrowing()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db down"));
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Contains("db down", sut.ErrorMessage);
        Assert.Equal(0, sut.TotalCount);
    }

    // ── Sorting (GetPagedEntries / GetSortedUsers switch) ───────────────────────

    [Fact]
    public async Task GetPagedEntries_SortByNameDefault_OrdersAscending()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[
            new User { UserID = 1, Name = "Zebedee" },
            new User { UserID = 2, Name = "Alice" },
        ]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal("Alice", sut.GetPagedEntries()[0].Name);
    }

    [Fact]
    public async Task GetPagedEntries_SortByEmailDescending_OrdersCorrectly()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[
            new User { UserID = 1, Name = "A", Email = "a@example.com" },
            new User { UserID = 2, Name = "B", Email = "z@example.com" },
        ]);
        var sut = CreateSut();
        sut.SortColumn = "Email";
        sut.SortDesc = true;

        await sut.OnGetAsync();

        Assert.Equal("z@example.com", sut.GetPagedEntries()[0].Email);
    }

    [Fact]
    public async Task GetPagedEntries_SortByActiveAscending_OrdersFalseBeforeTrue()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[
            new User { UserID = 1, Name = "A", Active = true },
            new User { UserID = 2, Name = "B", Active = false },
        ]);
        var sut = CreateSut();
        sut.SortColumn = "Active";

        await sut.OnGetAsync();

        Assert.False(sut.GetPagedEntries()[0].Active);
    }

    [Fact]
    public async Task GetPagedEntries_SortByGroup_UsesResolveGroupNameForOrdering()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[
            new User { UserID = 1, Name = "A", GroupCode = 1, GroupName = "Zeta Group" },
            new User { UserID = 2, Name = "B", GroupCode = 2, GroupName = "Alpha Group" },
        ]);
        var sut = CreateSut();
        sut.SortColumn = "Group";

        await sut.OnGetAsync();

        Assert.Equal("Alpha Group", sut.GetPagedEntries()[0].GroupName);
    }

    [Fact]
    public async Task GetPagedEntries_SortByArea_UsesResolveAreaNameForOrdering()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[
            new User { UserID = 1, Name = "A", AreaCode = 1, AreaName = "Zeta Area" },
            new User { UserID = 2, Name = "B", AreaCode = 2, AreaName = "Alpha Area" },
        ]);
        var sut = CreateSut();
        sut.SortColumn = "Area";

        await sut.OnGetAsync();

        Assert.Equal("Alpha Area", sut.GetPagedEntries()[0].AreaName);
    }

    // ── ResolveGroupName / ResolveAreaName fallback chain ───────────────────────

    [Fact]
    public void ResolveGroupName_GroupNameFromSp_UsesItDirectly()
    {
        var sut = CreateSut();
        var user = new User { GroupCode = 1, GroupName = "DEFRA Data Entry" };

        Assert.Equal("DEFRA Data Entry", sut.ResolveGroupName(user));
    }

    [Fact]
    public async Task ResolveGroupName_NoGroupNameFromSp_FallsBackToLookupDictionary()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        var sut = CreateSut();
        await sut.OnGetAsync(); // populates GroupNames from the lookup service

        Assert.Equal("DEFRA Data Entry", sut.ResolveGroupName(new User { GroupCode = 1 }));
    }

    [Fact]
    public void ResolveGroupName_NotInSpOrLookup_FallsBackToRawCode()
    {
        var sut = CreateSut();

        Assert.Equal("77", sut.ResolveGroupName(new User { GroupCode = 77 }));
    }

    [Fact]
    public void ResolveAreaName_AreaNameFromSp_UsesItDirectly()
    {
        var sut = CreateSut();
        var user = new User { AreaCode = 5, AreaName = "Histopath" };

        Assert.Equal("Histopath", sut.ResolveAreaName(user));
    }

    [Fact]
    public void ResolveAreaName_NotInSpOrLookup_FallsBackToRawCode()
    {
        var sut = CreateSut();

        Assert.Equal("88", sut.ResolveAreaName(new User { AreaCode = 88 }));
    }

    // ── Focus-row page restoration ───────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_FocusUserIdInTempData_JumpsToThatUsersPage()
    {
        var users = Enumerable.Range(1, 15).Select(n => new User { UserID = n, Name = $"User {n:D2}" }).ToList();
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)users);
        var sut = CreateSut();
        sut.TempData["FocusUserId"] = 12; // sorted by Name ascending -> index 11 -> page 2

        await sut.OnGetAsync();

        Assert.Equal(2, sut.PageNumber);
        Assert.Equal(12, sut.FocusUserId);
    }

    [Fact]
    public async Task OnGetAsync_FocusUserIdNotInList_LeavesPageNumberAndFocusUnset()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[
            new User { UserID = 1, Name = "Alice" },
        ]);
        var sut = CreateSut();
        sut.TempData["FocusUserId"] = 999;

        await sut.OnGetAsync();

        Assert.Equal(1, sut.PageNumber);
        Assert.Null(sut.FocusUserId);
    }

    [Fact]
    public async Task OnGetAsync_StatusMessageInTempData_IsSurfaced()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        var sut = CreateSut();
        sut.TempData["StatusMessage"] = "User 'Alice' was updated.";

        await sut.OnGetAsync();

        Assert.Equal("User 'Alice' was updated.", sut.StatusMessage);
    }

    // ── OnGetGridAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetGridAsync_ReturnsUserMaintenanceGridPartialCarryingViewData()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        var sut = CreateSut();

        var result = await sut.OnGetGridAsync();

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_UserMaintenanceGrid", partial.ViewName);
        Assert.Equal(1, partial.ViewData["TotalPages"]);
    }

    // ── SafeReturnUrl ────────────────────────────────────────────────────────────

    [Fact]
    public void SafeReturnUrl_LocalUrl_IsReturnedAsIs()
    {
        var sut = CreateSut();
        sut.ReturnUrl = "/Submissions/AddSubmission";

        Assert.Equal("/Submissions/AddSubmission", sut.SafeReturnUrl);
    }

    [Fact]
    public void SafeReturnUrl_ExternalUrl_ReturnsNull()
    {
        var sut = CreateSut();
        sut.ReturnUrl = "https://evil.example.com";

        Assert.Null(sut.SafeReturnUrl);
    }
}
