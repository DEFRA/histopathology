using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Pages.AuditLog;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the Audit log search pages (By date / By user / By submission),
/// scenario AUDIT-02 in .github/agents/histo-testing-agent.agent.md §5, backed by a shared
/// <see cref="FakeAuditLogService"/> seeded with representative entries so filter-routing
/// (which criteria reach which repository call) is genuinely exercised.
/// </summary>
public class AuditLogRegressionTests
{
    private readonly FakeAuditLogService _auditLog = new();
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IUserService> _users = new();

    public AuditLogRegressionTests()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<User>)[new User { UserID = 1, Name = "Alice" }]);
    }

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private AuditLogByDateModel CreateByDate() => new(_session.Object, _auditLog)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private AuditLogByUserModel CreateByUser() => new(_session.Object, _auditLog, _users.Object)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    private AuditLogBySubmissionModel CreateBySubmission() => new(_session.Object, _auditLog)
    {
        PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = NewHttpContext() },
    };

    // ── By date ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ByDate_MissingBothDates_ShowsBothValidationErrorsAndDoesNotSearch()
    {
        var sut = CreateByDate();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Enter a start date.", sut.Errors);
        Assert.Contains("Enter an end date.", sut.Errors);
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task ByDate_EndBeforeStart_IsRejected()
    {
        var sut = CreateByDate();
        sut.StartDate = new DateTime(2026, 6, 10);
        sut.EndDate = new DateTime(2026, 6, 1);

        await sut.OnPostAsync();

        Assert.Contains("The end date must be the same as or after the start date.", sut.Errors);
    }

    [Fact]
    public async Task ByDate_ValidRange_OnlyReturnsEntriesInsideIt()
    {
        _auditLog.Seed(new AuditLogEntry { TableName = "Batch", ChangedAt = new DateTime(2026, 6, 5) });
        _auditLog.Seed(new AuditLogEntry { TableName = "User", ChangedAt = new DateTime(2026, 7, 1) }); // outside the range below
        var sut = CreateByDate();
        sut.StartDate = new DateTime(2026, 6, 1);
        sut.EndDate = new DateTime(2026, 6, 30);

        await sut.OnPostAsync();

        Assert.True(sut.Searched);
        var row = Assert.Single(sut.Results);
        Assert.Equal("Batch", row.TableName);
    }

    // ── By user ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ByUser_NoUserSelected_IsRejected()
    {
        var sut = CreateByUser();
        sut.UserID = 0;
        sut.StartDate = DateTime.Today;
        sut.EndDate = DateTime.Today;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Select a user.", sut.Errors);
    }

    [Fact]
    public async Task ByUser_ValidCriteria_OnlyReturnsThatUsersEntries()
    {
        _auditLog.Seed(new AuditLogEntry { UserName = "Alice", TableName = "Batch" }, userId: 1);
        _auditLog.Seed(new AuditLogEntry { UserName = "Bob", TableName = "User" }, userId: 2);
        var sut = CreateByUser();
        sut.UserID = 1;
        sut.StartDate = DateTime.Today.AddDays(-7);
        sut.EndDate = DateTime.Today;

        await sut.OnPostAsync();

        var row = Assert.Single(sut.Results);
        Assert.Equal("Alice", row.UserName);
    }

    // ── By submission ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BySubmission_NoSubmissionNumber_IsRejected()
    {
        var sut = CreateBySubmission();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Enter a submission number.", sut.Errors);
    }

    [Fact]
    public async Task BySubmission_PrePopulatesFromSessionBatchId()
    {
        _session.Setup(s => s.BatchID).Returns(42);
        var sut = CreateBySubmission();

        sut.OnGet();

        Assert.Equal(42, sut.SubmissionID);
    }

    [Fact]
    public async Task BySubmission_ValidSubmission_OnlyReturnsThatSubmissionsEntries()
    {
        _auditLog.Seed(new AuditLogEntry { TableName = "Batch" }, submissionId: 100);
        _auditLog.Seed(new AuditLogEntry { TableName = "Animal" }, submissionId: 200);
        var sut = CreateBySubmission();
        sut.SubmissionID = 100;

        await sut.OnPostAsync();

        var row = Assert.Single(sut.Results);
        Assert.Equal("Batch", row.TableName);
    }

    /// <summary>In-memory stand-in for the audit log query stored procedures, seeded with entries tagged by user/submission so filter routing is genuinely exercised.</summary>
    private sealed class FakeAuditLogService : IAuditLogService
    {
        private readonly List<(AuditLogEntry Entry, int? UserId, int? SubmissionId)> _entries = [];

        public void Seed(AuditLogEntry entry, int? userId = null, int? submissionId = null) =>
            _entries.Add((entry, userId, submissionId));

        public Task<IReadOnlyList<AuditLogEntry>> GetBySubmissionAsync(int submissionId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<AuditLogEntry>)_entries.Where(e => e.SubmissionId == submissionId).Select(e => e.Entry).ToList());

        public Task<IReadOnlyList<AuditLogEntry>> GetByDateAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<AuditLogEntry>)_entries
                .Where(e => e.Entry.ChangedAt >= startDate.Date && e.Entry.ChangedAt <= endDate.Date.AddDays(1).AddTicks(-1))
                .Select(e => e.Entry).ToList());

        public Task<IReadOnlyList<AuditLogEntry>> GetByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<AuditLogEntry>)_entries.Where(e => e.UserId == userId).Select(e => e.Entry).ToList());
    }
}
