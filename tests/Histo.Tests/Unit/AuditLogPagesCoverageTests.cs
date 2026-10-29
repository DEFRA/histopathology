using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Pages.AuditLog;
using Histo.Web.Pages.Bookings;
using Histo.Web.Pages.Help;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Validation, export and initial-load behaviour of the audit log pages and static menu pages.</summary>
public class AuditLogPagesCoverageTests
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly Mock<IUserService> _users = new();

    private static readonly AuditLogEntry[] Entries =
    [
        new() { ID = "1", TableName = "Batch", FieldName = "Status", UserName = "u", BeforeValue = "a", AfterValue = "b", Reason = "r", KeyID = "9", ChangedAt = new DateTime(2024, 1, 1) },
    ];

    public AuditLogPagesCoverageTests()
    {
        _session.SetupProperty(s => s.BatchID);
    }

    private static PageContext NewPageContext() => new()
    {
        HttpContext = new DefaultHttpContext(),
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
    };

    private AuditLogByDateModel ByDate() => new(_session.Object, _auditLog.Object) { PageContext = NewPageContext() };
    private AuditLogByUserModel ByUser() => new(_session.Object, _auditLog.Object, _users.Object) { PageContext = NewPageContext() };
    private AuditLogBySubmissionModel BySubmission() => new(_session.Object, _auditLog.Object) { PageContext = NewPageContext() };

    private static void AssertXlsx(IActionResult result, string fileName)
    {
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(fileName, file.FileDownloadName);
        Assert.Equal(XlsxContentType, file.ContentType);
        Assert.NotEmpty(file.FileContents);
    }

    // ── ByDate ──

    [Fact]
    public void ByDate_OnGet_SetsTitleAndLeavesResultsEmpty()
    {
        var sut = ByDate();

        sut.OnGet();

        Assert.Equal("Audit log by date", sut.ViewData["Title"]);
        Assert.False(sut.Searched);
        Assert.Equal(1, sut.TotalPages);
    }

    [Fact]
    public async Task ByDate_OnPost_MissingDates_ReportsBothErrorsWithoutSearching()
    {
        var sut = ByDate();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(["Enter a start date.", "Enter an end date."], sut.Errors);
        Assert.False(sut.Searched);
        _auditLog.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ByDate_OnPost_EndBeforeStart_ReportsOrderingError()
    {
        var sut = ByDate();
        sut.StartDate = new DateTime(2024, 2, 1);
        sut.EndDate = new DateTime(2024, 1, 1);

        await sut.OnPostAsync();

        Assert.Equal(["The end date must be the same as or after the start date."], sut.Errors);
    }

    [Fact]
    public async Task ByDate_OnPost_ValidRange_LoadsResults()
    {
        _auditLog.Setup(s => s.GetByDateAsync(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31), It.IsAny<CancellationToken>())).ReturnsAsync(Entries);
        var sut = ByDate();
        sut.StartDate = new DateTime(2024, 1, 1);
        sut.EndDate = new DateTime(2024, 1, 31);

        await sut.OnPostAsync();

        Assert.True(sut.Searched);
        Assert.Empty(sut.Errors);
        Assert.Equal(1, sut.TotalCount);
    }

    [Fact]
    public async Task ByDate_Export_MissingDates_RedirectsToPage()
    {
        var sut = ByDate();

        var result = await sut.OnPostExportExcelAsync();

        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task ByDate_Export_ValidRange_ReturnsXlsx()
    {
        _auditLog.Setup(s => s.GetByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(Entries);
        var sut = ByDate();
        sut.StartDate = new DateTime(2024, 1, 1);
        sut.EndDate = new DateTime(2024, 1, 31);

        var result = await sut.OnPostExportExcelAsync();

        AssertXlsx(result, "AuditLogByDate.xlsx");
    }

    // ── ByUser ──

    [Fact]
    public async Task ByUser_OnGet_LoadsUsers()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[new User { UserID = 3 }]);
        var sut = ByUser();

        await sut.OnGetAsync();

        Assert.Single(sut.Users);
        Assert.Equal("Audit log by user", sut.ViewData["Title"]);
    }

    [Fact]
    public async Task ByUser_OnPost_NoUserAndNoDates_ReportsAllErrors()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        var sut = ByUser();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(["Select a user.", "Enter a start date.", "Enter an end date."], sut.Errors);
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task ByUser_OnPost_Valid_LoadsResults()
    {
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _auditLog.Setup(s => s.GetByUserAsync(5, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Entries);
        var sut = ByUser();
        sut.UserID = 5;
        sut.StartDate = new DateTime(2024, 1, 1);
        sut.EndDate = new DateTime(2024, 1, 2);

        await sut.OnPostAsync();

        Assert.True(sut.Searched);
        Assert.Equal(1, sut.TotalCount);
    }

    [Fact]
    public async Task ByUser_Export_ReturnsXlsx()
    {
        _auditLog.Setup(s => s.GetByUserAsync(5, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Entries);
        var sut = ByUser();
        sut.UserID = 5;

        var result = await sut.OnPostExportExcelAsync();

        AssertXlsx(result, "UserAuditlogResult.xlsx");
    }

    // ── BySubmission ──

    [Fact]
    public void BySubmission_OnGet_BatchInSession_PrePopulatesSubmissionId()
    {
        _session.Object.BatchID = 77;
        var sut = BySubmission();

        sut.OnGet();

        Assert.Equal(77, sut.SubmissionID);
    }

    [Fact]
    public void BySubmission_OnGet_NoBatchInSession_LeavesSubmissionIdBlank()
    {
        var sut = BySubmission();

        sut.OnGet();

        Assert.Null(sut.SubmissionID);
    }

    [Fact]
    public async Task BySubmission_OnPost_NoSubmission_ReportsError()
    {
        var sut = BySubmission();

        await sut.OnPostAsync();

        Assert.Equal(["Enter a submission number."], sut.Errors);
        Assert.False(sut.Searched);
    }

    [Fact]
    public async Task BySubmission_OnPost_Valid_LoadsResults()
    {
        _auditLog.Setup(s => s.GetBySubmissionAsync(12, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Entries);
        var sut = BySubmission();
        sut.SubmissionID = 12;

        await sut.OnPostAsync();

        Assert.True(sut.Searched);
        Assert.Equal(1, sut.TotalCount);
    }

    [Fact]
    public async Task BySubmission_Export_NoSubmission_RedirectsToPage()
    {
        var sut = BySubmission();

        var result = await sut.OnPostExportExcelAsync();

        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task BySubmission_Export_Valid_ReturnsXlsx()
    {
        _auditLog.Setup(s => s.GetBySubmissionAsync(12, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Entries);
        var sut = BySubmission();
        sut.SubmissionID = 12;

        var result = await sut.OnPostExportExcelAsync();

        AssertXlsx(result, "SubmissionAuditlog.xlsx");
    }

    // ── Static menu pages: OnGet must not touch the session ──

    [Fact]
    public void MenuPages_OnGet_DoNotTouchSession()
    {
        var strict = new Mock<ISessionService>(MockBehavior.Strict);

        new AuditLogMenuModel(strict.Object).OnGet();
        new BookingMenuModel(strict.Object).OnGet();
        new SearchMenuModel(strict.Object).OnGet();
        new HelpModel(strict.Object).OnGet();

        strict.VerifyNoOtherCalls();
    }
}
