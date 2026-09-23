using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Pages.AuditLog;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="AuditLogBySubmissionModel"/>.</summary>
public class AuditLogBySubmissionModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IAuditLogService> _auditLog = new();

    public AuditLogBySubmissionModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
    }

    private AuditLogBySubmissionModel CreateSut() =>
        new(_session.Object, _auditLog.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public void OnGet_NoBatchIdInSession_LeavesSubmissionIdBlank()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        sut.OnGet();

        Assert.Null(sut.SubmissionID);
    }

    [Fact]
    public void OnGet_BatchIdInSession_PrePopulatesSubmissionId()
    {
        _session.Object.BatchID = 42;
        var sut = CreateSut();

        sut.OnGet();

        Assert.Equal(42, sut.SubmissionID);
    }

    [Fact]
    public async Task OnPostAsync_NullSubmissionId_ReturnsError()
    {
        var sut = CreateSut();
        sut.SubmissionID = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Enter a submission number.", sut.Errors);
        _auditLog.Verify(a => a.GetBySubmissionAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ValidSubmissionId_SearchesAndReturnsResults()
    {
        _auditLog.Setup(a => a.GetBySubmissionAsync(42, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AuditLogEntry>)[new AuditLogEntry { TableName = "Batch" }]);
        var sut = CreateSut();
        sut.SubmissionID = 42;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.Searched);
        Assert.Single(sut.Results);
    }
}
