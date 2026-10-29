using Histo.Submissions.Interfaces;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="FixCompletedDatesModel"/>.</summary>
public class FixCompletedDatesModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    private FixCompletedDatesModel CreateSut() =>
        new(_session.Object, _batches.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public void OnGet_SetsPageTitles_WithoutRunningTheFix()
    {
        var sut = CreateSut();

        sut.OnGet();

        Assert.Equal("Fix Completed Dates", sut.ViewData["Title"]);
        Assert.Equal("Fix Completed Dates", sut.ViewData["PageTitle"]);
        Assert.Null(sut.BatchesUpdated);
        _batches.Verify(b => b.FixCompletedDatesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_RunsTheFixAndReportsBatchesUpdated()
    {
        _batches.Setup(b => b.FixCompletedDatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7);
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(7, sut.BatchesUpdated);
        Assert.Equal("Fix Completed Dates", sut.ViewData["Title"]);
        Assert.Equal("Fix Completed Dates", sut.ViewData["PageTitle"]);
    }
}
