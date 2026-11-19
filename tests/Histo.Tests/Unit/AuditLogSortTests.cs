using Histo.Administration.Interfaces;
using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Pages.AuditLog;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Column sort behaviour of the audit log result grids.</summary>
public class AuditLogSortTests
{
    private static readonly AuditLogEntry[] Entries =
    [
        Entry("2", new DateTime(2024, 1, 2)),
        Entry("3", new DateTime(2024, 1, 1)),
        Entry("1", new DateTime(2024, 1, 3)),
    ];

    private static AuditLogEntry Entry(string value, DateTime changedAt) => new()
    {
        ID = value,
        TableName = value,
        FieldName = value,
        UserName = value,
        BeforeValue = value,
        AfterValue = value,
        Reason = value,
        KeyID = value,
        ChangedAt = changedAt,
    };

    private static PageContext NewPageContext() => new()
    {
        HttpContext = new DefaultHttpContext(),
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
    };

    private static async Task<IReadOnlyList<AuditLogEntry>> ByDatePage(string? column, bool desc)
    {
        var svc = new Mock<IAuditLogService>();
        svc.Setup(s => s.GetByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Entries);
        var model = new AuditLogByDateModel(new Mock<ISessionService>().Object, svc.Object)
        {
            PageContext = NewPageContext(),
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31),
            SortColumn = column,
            SortDesc = desc,
        };
        await model.OnPostAsync();
        return model.GetPagedEntries();
    }

    private static async Task<IReadOnlyList<AuditLogEntry>> ByUserPage(string? column, bool desc)
    {
        var svc = new Mock<IAuditLogService>();
        svc.Setup(s => s.GetByUserAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Entries);
        var users = new Mock<IUserService>();
        users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var model = new AuditLogByUserModel(new Mock<ISessionService>().Object, svc.Object, users.Object)
        {
            PageContext = NewPageContext(),
            UserID = 5,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31),
            SortColumn = column,
            SortDesc = desc,
        };
        await model.OnPostAsync();
        return model.GetPagedEntries();
    }

    private static async Task<IReadOnlyList<AuditLogEntry>> BySubmissionPage(string? column, bool desc)
    {
        var svc = new Mock<IAuditLogService>();
        svc.Setup(s => s.GetBySubmissionAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Entries);
        var model = new AuditLogBySubmissionModel(new Mock<ISessionService>().Object, svc.Object)
        {
            PageContext = NewPageContext(),
            SubmissionID = 7,
            SortColumn = column,
            SortDesc = desc,
        };
        await model.OnPostAsync();
        return model.GetPagedEntries();
    }

    private static string Order(IReadOnlyList<AuditLogEntry> rows) => string.Join(",", rows.Select(r => r.ID));

    public static TheoryData<string?, bool, string> Cases => new()
    {
        { "FieldName", false, "1,2,3" },
        { "FieldName", true, "3,2,1" },
        { "UserName", false, "1,2,3" },
        { "TableName", false, "1,2,3" },
        { "BeforeValue", false, "1,2,3" },
        { "AfterValue", true, "3,2,1" },
        { "Reason", false, "1,2,3" },
        { "KeyID", true, "3,2,1" },
        { null, false, "3,2,1" },
        { null, true, "1,2,3" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ByDate_OrdersByRequestedColumn(string? column, bool desc, string expected) =>
        Assert.Equal(expected, Order(await ByDatePage(column, desc)));

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ByUser_OrdersByRequestedColumn(string? column, bool desc, string expected) =>
        Assert.Equal(expected, Order(await ByUserPage(column, desc)));

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task BySubmission_OrdersByRequestedColumn(string? column, bool desc, string expected) =>
        Assert.Equal(expected, Order(await BySubmissionPage(column, desc)));
}
