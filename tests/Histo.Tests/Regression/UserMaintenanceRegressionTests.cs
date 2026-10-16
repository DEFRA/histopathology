using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the User Maintenance workflow (Admin/UserMaintenance, AddUser, EditUser).
///
/// Unlike the atomic tests in <c>Unit/AddUserModelTests.cs</c>/<c>EditUserModelTests.cs</c>, these
/// tests chain Add → List → Edit → Deactivate → Reactivate against a single shared
/// <see cref="FakeUserService"/> instance, so a regression in how one page model's write is
/// read back by another is caught even though no real database is involved (scenario ADMIN-01
/// in .github/agents/histo-testing-agent.agent.md §5).
/// </summary>
public class UserMaintenanceRegressionTests
{
    private readonly FakeUserService _users = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public UserMaintenanceRegressionTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _lookups.Setup(l => l.GetUserGroupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Name = "DEFRA Data Entry" }, new LookupItem { ID = 2, Name = "Maintenance" }]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 5, Name = "Histopath" }]);
    }

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private AddUserModel CreateAddUser()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns<string>(s => s?.StartsWith('/') == true);
        var httpContext = NewHttpContext();
        return new(_session.Object, _users, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
            Url = url.Object,
        };
    }

    private EditUserModel CreateEditUser()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns<string>(s => s?.StartsWith('/') == true);
        var httpContext = NewHttpContext();
        return new(_session.Object, _users, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
            Url = url.Object,
        };
    }

    private UserMaintenanceModel CreateGrid()
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, _users, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    [Fact]
    public async Task AddEditDeactivateReactivate_FullWorkflow_EachStepReflectedInTheGrid()
    {
        // 1. Add a new user.
        var add = CreateAddUser();
        add.Name = "Grace Hopper";
        add.Email = "grace.hopper@example.com";
        add.GroupCode = 2;
        add.AreaCode = 5;

        var addResult = await add.OnPostAsync();
        Assert.IsType<RedirectToPageResult>(addResult);

        var created = _users.Users.Single(u => u.Email == "grace.hopper@example.com");
        Assert.True(created.Active);

        // 2. The grid shows the new, active user by default.
        var gridAfterAdd = CreateGrid();
        await gridAfterAdd.OnGetAsync();
        Assert.Contains(gridAfterAdd.GetPagedEntries(), u => u.UserID == created.UserID && u.Name == "Grace Hopper");

        // 3. Edit the user's details.
        var edit = CreateEditUser();
        edit.UserId = created.UserID;
        await edit.OnGetAsync();
        edit.Name = "Grace M. Hopper";
        edit.Email = created.Email;
        edit.GroupCode = created.GroupCode;
        edit.AreaCode = created.AreaCode;
        edit.Active = true;

        var editResult = await edit.OnPostAsync();
        Assert.IsType<RedirectToPageResult>(editResult);
        Assert.Equal("Grace M. Hopper", _users.Users.Single(u => u.UserID == created.UserID).Name);

        // 4. Deactivate the user.
        var deactivate = CreateEditUser();
        deactivate.UserId = created.UserID;
        await deactivate.OnGetAsync();
        deactivate.Active = false;

        await deactivate.OnPostAsync();
        Assert.False(_users.Users.Single(u => u.UserID == created.UserID).Active);

        // 5. Deactivated user disappears from the default (active-only) grid view...
        var gridActiveOnly = CreateGrid();
        gridActiveOnly.ShowDeactivated = false;
        await gridActiveOnly.OnGetAsync();
        Assert.DoesNotContain(gridActiveOnly.GetPagedEntries(), u => u.UserID == created.UserID);

        // ...but is still visible when "show deactivated" is on.
        var gridShowDeactivated = CreateGrid();
        gridShowDeactivated.ShowDeactivated = true;
        await gridShowDeactivated.OnGetAsync();
        Assert.Contains(gridShowDeactivated.GetPagedEntries(), u => u.UserID == created.UserID && !u.Active);

        // 6. Reactivate the user.
        var reactivate = CreateEditUser();
        reactivate.UserId = created.UserID;
        await reactivate.OnGetAsync();
        reactivate.Active = true;

        await reactivate.OnPostAsync();
        Assert.True(_users.Users.Single(u => u.UserID == created.UserID).Active);

        var gridAfterReactivate = CreateGrid();
        gridAfterReactivate.ShowDeactivated = false;
        await gridAfterReactivate.OnGetAsync();
        Assert.Contains(gridAfterReactivate.GetPagedEntries(), u => u.UserID == created.UserID && u.Active);
    }

    [Fact]
    public async Task Add_DuplicateEmailAcrossTwoUsers_SecondIsRejected()
    {
        var first = CreateAddUser();
        first.Name = "Ada Lovelace";
        first.Email = "shared@example.com";
        first.GroupCode = 1;
        first.AreaCode = 5;
        await first.OnPostAsync();

        var second = CreateAddUser();
        second.Name = "Impersonator";
        second.Email = "shared@example.com";
        second.GroupCode = 1;
        second.AreaCode = 5;

        var result = await second.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("A user with this email already exists.", second.Errors["Email"]);
        Assert.Single(_users.Users); // the impersonating add never persisted
    }

    [Fact]
    public async Task Edit_RenamingToAnotherActiveUsersEmail_IsRejectedAndOriginalRowUnchanged()
    {
        var add1 = CreateAddUser();
        add1.Name = "User One";
        add1.Email = "one@example.com";
        add1.GroupCode = 1;
        add1.AreaCode = 5;
        await add1.OnPostAsync();

        var add2 = CreateAddUser();
        add2.Name = "User Two";
        add2.Email = "two@example.com";
        add2.GroupCode = 1;
        add2.AreaCode = 5;
        await add2.OnPostAsync();

        var userTwo = _users.Users.Single(u => u.Email == "two@example.com");
        var edit = CreateEditUser();
        edit.UserId = userTwo.UserID;
        await edit.OnGetAsync();
        edit.Email = "one@example.com"; // collides with User One

        var result = await edit.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("A user with this email already exists.", edit.Errors["Email"]);
        Assert.Equal("two@example.com", _users.Users.Single(u => u.UserID == userTwo.UserID).Email);
    }

    /// <summary>In-memory stand-in for the real <c>[User]</c> table, shared across page-model instances within a test.</summary>
    private sealed class FakeUserService : IUserService
    {
        private int _nextId = 1;
        public List<User> Users { get; } = [];

        public Task<User?> ResolveUserAsync(string ntLogin, CancellationToken ct = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.NtLogin == ntLogin));

        public Task<User?> ResolveUserByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Active && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<User>> GetAllUsersAsync(CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<User>)Users.ToList());

        public Task<IReadOnlyList<User>> GetUsersByAreaAsync(string userArea, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<User>)Users.Where(u => u.AreaName == userArea).ToList());

        public Task<bool> CreateUserAsync(User user, CancellationToken ct = default)
        {
            Users.Add(new User
            {
                UserID = _nextId++,
                Name = user.Name,
                Email = user.Email,
                GroupCode = user.GroupCode,
                AreaCode = user.AreaCode,
                Active = user.Active,
                NtLogin = user.NtLogin,
            });
            return Task.FromResult(true);
        }

        public Task<bool> UpdateUserAsync(User user, int userId, CancellationToken ct = default)
        {
            var index = Users.FindIndex(u => u.UserID == user.UserID);
            if (index < 0) return Task.FromResult(false);

            Users[index] = new User
            {
                UserID = user.UserID,
                Name = user.Name,
                Email = user.Email,
                GroupCode = user.GroupCode,
                AreaCode = user.AreaCode,
                Active = user.Active,
                NtLogin = user.NtLogin,
            };
            return Task.FromResult(true);
        }
    }
}
