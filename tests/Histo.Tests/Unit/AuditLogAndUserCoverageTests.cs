using System.Data;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Administration.Repositories;
using Histo.Administration.Services;
using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.AuditLog.Repositories;
using Histo.AuditLog.Services;
using Histo.Infrastructure;
using Histo.Tests.Support;
using Moq;

namespace Histo.Tests.Unit;

public class UserServiceCoverageTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static InvalidOperationException Boom => new("boom");

    private UserService Sut() => new(_repo.Object, _logger.Object);

    private void VerifyLogged() =>
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception?>(), It.IsAny<object[]>()), Times.AtLeastOnce);

    [Fact]
    public async Task ResolveUserAsync_ReturnsUserOrNullOnFailure()
    {
        var user = new User { UserID = 4 };
        _repo.Setup(r => r.GetUserByNtLoginAsync("D\\u", Ct)).ReturnsAsync(user);
        Assert.Same(user, await Sut().ResolveUserAsync("D\\u", Ct));

        _repo.Setup(r => r.GetUserByNtLoginAsync("D\\u", Ct)).ThrowsAsync(Boom);
        Assert.Null(await Sut().ResolveUserAsync("D\\u", Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task ResolveUserByEmailAsync_ReturnsUserOrNullOnFailure()
    {
        var user = new User { UserID = 4 };
        _repo.Setup(r => r.GetUserByEmailAsync("a@b.c", Ct)).ReturnsAsync(user);
        Assert.Same(user, await Sut().ResolveUserByEmailAsync("a@b.c", Ct));

        _repo.Setup(r => r.GetUserByEmailAsync("a@b.c", Ct)).ThrowsAsync(Boom);
        Assert.Null(await Sut().ResolveUserByEmailAsync("a@b.c", Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsRepositoryUsers_AndPropagatesFailure()
    {
        IReadOnlyList<User> users = [new User { UserID = 1 }];
        _repo.Setup(r => r.GetUsersAsync(Ct)).ReturnsAsync(users);
        Assert.Same(users, await Sut().GetAllUsersAsync(Ct));

        _repo.Setup(r => r.GetUsersAsync(Ct)).ThrowsAsync(Boom);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().GetAllUsersAsync(Ct));
    }

    [Fact]
    public async Task GetUsersByAreaAsync_ReturnsRepositoryUsers()
    {
        IReadOnlyList<User> users = [new User { UserID = 1 }];
        _repo.Setup(r => r.GetUsersByAreaAsync("2", Ct)).ReturnsAsync(users);

        Assert.Same(users, await Sut().GetUsersByAreaAsync("2", Ct));
    }

    [Fact]
    public async Task CreateUserAsync_SuccessAndFailure()
    {
        var user = new User { Email = "a@b.c" };
        Assert.True(await Sut().CreateUserAsync(user, Ct));
        _repo.Verify(r => r.CreateUserAsync(user, Ct), Times.Once);

        _repo.Setup(r => r.CreateUserAsync(user, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().CreateUserAsync(user, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task UpdateUserAsync_SuccessAndFailure()
    {
        var user = new User { UserID = 3 };
        Assert.True(await Sut().UpdateUserAsync(user, 9, Ct));
        _repo.Verify(r => r.UpdateUserAsync(user, 9, Ct), Times.Once);

        _repo.Setup(r => r.UpdateUserAsync(user, 9, Ct)).ThrowsAsync(Boom);
        Assert.False(await Sut().UpdateUserAsync(user, 9, Ct));
        VerifyLogged();
    }
}

public class UserRepositoryCoverageTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DataSet One(DataTable t) => new DataSet().Also(ds => ds.Tables.Add(t));

    [Fact]
    public async Task CreateUser_PassesAllFieldsToAddUser()
    {
        var db = new FakeDb();
        var user = new User { NtLogin = "D\\x", Name = "X", Email = "x@d.c", GroupCode = 2, AreaCode = 3, Active = true };

        await new UserRepository(db).CreateUserAsync(user, Ct);

        var cmd = Assert.Single(db.Commands);
        Assert.Equal("AddUser", cmd.Text);
        Assert.Equal(("D\\x", "X", "x@d.c", 2, 3, true),
            ((string)cmd.Parameters["NTLogin"]!, (string)cmd.Parameters["Name"]!, (string)cmd.Parameters["Email"]!,
             (int)cmd.Parameters["UserGroup"]!, (int)cmd.Parameters["UserArea"]!, (bool)cmd.Parameters["Active"]!));
    }

    [Fact]
    public async Task UpdateUser_PassesRecordIdAndSessionEditorSeparately()
    {
        var db = new FakeDb();
        var user = new User { UserID = 11, NtLogin = "D\\x", Name = "X", Email = "x@d.c", GroupCode = 2, AreaCode = 3, Active = false };

        await new UserRepository(db).UpdateUserAsync(user, 99, Ct);

        var cmd = Assert.Single(db.Commands);
        Assert.Equal("EditUser", cmd.Text);
        Assert.Equal((11, 99, false), ((int)cmd.Parameters["ID"]!, (int)cmd.Parameters["UserID"]!, (bool)cmd.Parameters["Active"]!));
    }

    [Fact]
    public async Task GetUsers_ToleratesStringNumericColumnsAndMissingColumns()
    {
        var t = new DataTable();
        t.Columns.Add("id", typeof(long));
        t.Columns.Add("UserGroup", typeof(string));
        t.Columns.Add("userarea", typeof(decimal));
        t.Columns.Add("Active", typeof(bool));
        t.Columns.Add("Name", typeof(string));
        t.Rows.Add(5L, "", 7m, true, DBNull.Value);
        t.Rows.Add(6L, "12", 8m, false, "Zed");
        var db = new FakeDb { OnQuery = _ => One(t) };

        var users = await new UserRepository(db).GetUsersAsync(Ct);

        Assert.Equal([(5, 0, 7, true, ""), (6, 12, 8, false, "Zed")],
            users.Select(u => (u.UserID, u.GroupCode, u.AreaCode, u.Active, u.Name)));
        Assert.Equal(string.Empty, users[0].NtLogin);
    }

    [Fact]
    public async Task GetUsersByArea_PassesAreaAndMapsRows()
    {
        var t = new DataTable();
        t.Columns.Add("ID", typeof(int));
        t.Columns.Add("NTLogin", typeof(string));
        t.Columns.Add("Email", typeof(string));
        t.Rows.Add(2, "D\\a", "a@d.c");
        var db = new FakeDb { OnQuery = _ => One(t) };

        var user = Assert.Single(await new UserRepository(db).GetUsersByAreaAsync("NI", Ct));

        Assert.Equal(("D\\a", "a@d.c", false), (user.NtLogin, user.Email, user.Active));
        Assert.Equal("NI", db.Commands[0].Parameters["UserArea"]);
    }

    [Fact]
    public async Task GetUserByEmail_ActiveUserWithSparseColumns_DefaultsMissingOnes()
    {
        var t = new DataTable();
        t.Columns.Add("ID", typeof(short));
        t.Columns.Add("Active", typeof(bool));
        t.Columns.Add("Email", typeof(string));
        t.Rows.Add((short)4, true, "u@d.c");
        var db = new FakeDb { OnQuery = _ => One(t) };

        var user = await new UserRepository(db).GetUserByEmailAsync("u@d.c", Ct);

        Assert.NotNull(user);
        Assert.Equal((4, "u@d.c", 0, 0, ""), (user.UserID, user.Email, user.GroupCode, user.AreaCode, user.Name));
    }

    [Fact]
    public async Task GetUserByEmail_NoActiveColumn_TreatedAsInactive()
    {
        var t = new DataTable();
        t.Columns.Add("ID", typeof(int));
        t.Rows.Add(4);
        var db = new FakeDb { OnQuery = _ => One(t) };

        Assert.Null(await new UserRepository(db).GetUserByEmailAsync("u@d.c", Ct));
    }
}

public class AuditLogServiceCoverageTests
{
    private readonly Mock<IAuditLogRepository> _repo = new();
    private readonly Mock<IAppLogger> _logger = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static InvalidOperationException Boom => new("boom");

    private AuditLogService Sut() => new(_repo.Object, _logger.Object);

    private void VerifyLogged() =>
        _logger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception?>(), It.IsAny<object[]>()), Times.AtLeastOnce);

    [Fact]
    public async Task GetBySubmissionAsync_ReturnsEntriesOrEmptyOnFailure()
    {
        var from = new DateTime(2026, 1, 1);
        IReadOnlyList<AuditLogEntry> entries = [new AuditLogEntry { TableName = "BATCH_TABLE" }];
        _repo.Setup(r => r.GetBySubmissionAsync(3, from, null, Ct)).ReturnsAsync(entries);
        Assert.Same(entries, await Sut().GetBySubmissionAsync(3, from, ct: Ct));

        _repo.Setup(r => r.GetBySubmissionAsync(3, from, null, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetBySubmissionAsync(3, from, ct: Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetByDateAsync_ReturnsEntriesOrEmptyOnFailure()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 3);
        IReadOnlyList<AuditLogEntry> entries = [new AuditLogEntry { TableName = "BATCH_TABLE" }];
        _repo.Setup(r => r.GetByDateAsync(from, to, Ct)).ReturnsAsync(entries);
        Assert.Same(entries, await Sut().GetByDateAsync(from, to, Ct));

        _repo.Setup(r => r.GetByDateAsync(from, to, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetByDateAsync(from, to, Ct));
        VerifyLogged();
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsEntriesOrEmptyOnFailure()
    {
        IReadOnlyList<AuditLogEntry> entries = [new AuditLogEntry { TableName = "BATCH_TABLE" }];
        _repo.Setup(r => r.GetByUserAsync(8, null, null, Ct)).ReturnsAsync(entries);
        Assert.Same(entries, await Sut().GetByUserAsync(8, ct: Ct));

        _repo.Setup(r => r.GetByUserAsync(8, null, null, Ct)).ThrowsAsync(Boom);
        Assert.Empty(await Sut().GetByUserAsync(8, ct: Ct));
        VerifyLogged();
    }
}

public class AuditLogRepositoryCoverageTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DataSet Entries(params string[] tableNames)
    {
        var t = new DataTable();
        t.Columns.Add("TableName", typeof(string));
        t.Columns.Add("FieldName", typeof(string));
        foreach (var n in tableNames) t.Rows.Add(n, "F");
        return new DataSet().Also(ds => ds.Tables.Add(t));
    }

    private static DataSet Ids(params int[] ids)
    {
        var t = new DataTable();
        t.Columns.Add("ID", typeof(int));
        foreach (var id in ids) t.Rows.Add(id);
        return new DataSet().Also(ds => ds.Tables.Add(t));
    }

    [Fact]
    public async Task GetBySubmission_FansOutAcrossBatchTissueBlockTissueAndBlockAnimalRows()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        var db = new FakeDb
        {
            OnQuery = c => c.Text switch
            {
                "GetAuditLogBySubmission" => Entries("BATCH"),
                "GetBatchTissuesIDs" => Ids(11, 12),
                "GetBatchBlockTissuesIDs" => Ids(21),
                "GetBatchBlockAnimalIDs" => Ids(31),
                "GetAuditLogTissue" => Entries("T" + c.Parameters["ID"]),
                _ => null,
            },
        };

        var entries = await new AuditLogRepository(db).GetBySubmissionAsync(5, from, to, Ct);

        Assert.Equal(["BATCH", "T11", "T12", "T21", "T31"], entries.Select(e => e.TableName));
        var batch = db.Commands[0].Parameters;
        Assert.Equal((from, to, 5), ((DateTime)batch["StartDate"]!, (DateTime)batch["EndDate"]!, (int)batch["SubmissionID"]!));
        var tissueCalls = db.Commands.Where(c => c.Text == "GetAuditLogTissue").ToList();
        Assert.Equal([0, 0, 1, 1], tissueCalls.Select(c => (int)c.Parameters["Blocked"]!));
    }

    [Fact]
    public async Task GetBySubmission_NoDates_PassesDbNullBounds_AndNoChildRowsMeansOnlyBatchRows()
    {
        var db = new FakeDb { OnQuery = c => c.Text == "GetAuditLogBySubmission" ? Entries("BATCH") : null };

        var entries = await new AuditLogRepository(db).GetBySubmissionAsync(5, null, null, Ct);

        Assert.Equal("BATCH", Assert.Single(entries).TableName);
        Assert.Equal(DBNull.Value, db.Commands[0].Parameters["StartDate"]);
        Assert.Equal(DBNull.Value, db.Commands[0].Parameters["EndDate"]);
        Assert.DoesNotContain(db.Commands, c => c.Text == "GetAuditLogTissue");
    }

    [Fact]
    public async Task GetByDate_CallsProcOncePerCalendarDayInclusive()
    {
        var db = new FakeDb { OnQuery = c => Entries("D" + ((DateTime)c.Parameters["LogDate"]!).Day) };

        var entries = await new AuditLogRepository(db).GetByDateAsync(new DateTime(2026, 3, 1, 14, 0, 0), new DateTime(2026, 3, 3, 1, 0, 0), Ct);

        Assert.Equal(["D1", "D2", "D3"], entries.Select(e => e.TableName));
        Assert.Equal([new DateTime(2026, 3, 1), new DateTime(2026, 3, 2), new DateTime(2026, 3, 3)],
            db.Commands.Select(c => (DateTime)c.Parameters["LogDate"]!));
    }

    [Fact]
    public async Task GetByDate_EndBeforeStart_ReturnsEmptyWithoutQuerying()
    {
        var db = new FakeDb();

        Assert.Empty(await new AuditLogRepository(db).GetByDateAsync(new DateTime(2026, 3, 5), new DateTime(2026, 3, 1), Ct));
        Assert.Empty(db.Commands);
    }

    [Fact]
    public async Task GetByUser_ExtendsEndDateToEndOfDay()
    {
        var db = new FakeDb { OnQuery = _ => Entries("U") };

        var entries = await new AuditLogRepository(db).GetByUserAsync(8, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2, 9, 0, 0), Ct);

        Assert.Equal("U", Assert.Single(entries).TableName);
        var p = db.Commands[0].Parameters;
        Assert.Equal(8, p["UserID"]);
        Assert.Equal(new DateTime(2026, 1, 1), p["StartDate"]);
        Assert.Equal(new DateTime(2026, 1, 3).AddTicks(-1), p["EndDate"]);
    }

    [Fact]
    public async Task GetByUser_NoDates_PassesDbNullBounds()
    {
        var db = new FakeDb();

        Assert.Empty(await new AuditLogRepository(db).GetByUserAsync(8, null, null, Ct));
        Assert.Equal(DBNull.Value, db.Commands[0].Parameters["StartDate"]);
        Assert.Equal(DBNull.Value, db.Commands[0].Parameters["EndDate"]);
    }
}
