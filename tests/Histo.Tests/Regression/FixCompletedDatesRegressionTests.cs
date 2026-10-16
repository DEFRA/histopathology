using Histo.Submissions.Interfaces;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression test for the Fix completed dates admin utility — previously had zero test coverage
/// of any kind (Tier 1 gap). The page is a thin wrapper over a single service call, so a plain
/// Moq mock is appropriate rather than a stateful fake (no cross-page state to chain).
/// </summary>
public class FixCompletedDatesRegressionTests
{
    [Fact]
    public async Task OnPostAsync_ReportsHowManyBatchesWereCorrected()
    {
        var batches = new Mock<IBatchService>();
        batches.Setup(b => b.FixCompletedDatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        var sut = new FixCompletedDatesModel(Mock.Of<ISessionService>(), batches.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() },
        };

        await sut.OnPostAsync();

        Assert.Equal(3, sut.BatchesUpdated);
    }

    [Fact]
    public async Task OnPostAsync_NoBatchesNeededFixing_ReportsZero()
    {
        var batches = new Mock<IBatchService>();
        batches.Setup(b => b.FixCompletedDatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var sut = new FixCompletedDatesModel(Mock.Of<ISessionService>(), batches.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() },
        };

        await sut.OnPostAsync();

        Assert.Equal(0, sut.BatchesUpdated);
    }

    [Fact]
    public void OnGet_NothingUpdatedYet_BatchesUpdatedIsNull()
    {
        var sut = new FixCompletedDatesModel(Mock.Of<ISessionService>(), Mock.Of<IBatchService>())
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() },
        };

        sut.OnGet();

        Assert.Null(sut.BatchesUpdated); // the count only appears after the user clicks the button
    }
}
