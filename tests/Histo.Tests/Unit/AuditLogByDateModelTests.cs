using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Models;
using Histo.Web.Pages.AuditLog;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="AuditLogByDateModel"/>.</summary>
public class AuditLogByDateModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IAuditLogService> _auditLog = new();

    private AuditLogByDateModel CreateSut() =>
        new(_session.Object, _auditLog.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnPostAsync_MissingStartDate_ReturnsPageWithError()
    {
        var sut = CreateSut();
        sut.StartDate = null;
        sut.EndDate = DateTime.Today;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Enter a start date.", sut.Errors);
    }

    [Fact]
    public async Task OnPostAsync_MissingEndDate_ReturnsPageWithError()
    {
        var sut = CreateSut();
        sut.StartDate = DateTime.Today;
        sut.EndDate = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("Enter an end date.", sut.Errors);
    }

    [Fact]
    public async Task OnPostAsync_EndDateBeforeStartDate_ReturnsPageWithError()
    {
        var sut = CreateSut();
        sut.StartDate = DateTime.Today;
        sut.EndDate = DateTime.Today.AddDays(-1);

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("The end date must be the same as or after the start date.", sut.Errors);
    }

    [Fact]
    public async Task OnPostAsync_ValidRange_SearchesAndSetsResults()
    {
        _auditLog.Setup(a => a.GetByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AuditLogEntry>)[new AuditLogEntry { FieldName = "Status" }]);
        var sut = CreateSut();
        sut.StartDate = DateTime.Today.AddDays(-1);
        sut.EndDate = DateTime.Today;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.Searched);
        Assert.Single(sut.Results);
    }

    [Fact]
    public async Task OnPostExportCsvAsync_MissingDates_RedirectsToSelf()
    {
        var sut = CreateSut();
        sut.StartDate = null;

        var result = await sut.OnPostExportCsvAsync();

        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostExportCsvAsync_ValidDates_ReturnsFileResult()
    {
        _auditLog.Setup(a => a.GetByDateAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AuditLogEntry>)[]);
        var sut = CreateSut();
        sut.StartDate = DateTime.Today.AddDays(-1);
        sut.EndDate = DateTime.Today;

        var result = await sut.OnPostExportCsvAsync();

        Assert.IsAssignableFrom<FileResult>(result);
    }
}
