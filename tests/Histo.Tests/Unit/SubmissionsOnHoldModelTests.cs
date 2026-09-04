using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="SubmissionsOnHoldModel"/>.</summary>
public class SubmissionsOnHoldModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public SubmissionsOnHoldModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.IsViewSubmissionMode, true);
    }

    private SubmissionsOnHoldModel CreateSut() =>
        new(_session.Object, _batches.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    [Fact]
    public async Task OnGetAsync_LoadsOnHoldBatches()
    {
        _batches.Setup(b => b.GetOnHoldAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 1 }, new BatchListResult { ID = 2 }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(2, sut.TotalCount);
    }

    [Fact]
    public void OnPostSelect_SetsSessionStateAndRedirectsToBatchDetails()
    {
        var sut = CreateSut();

        var result = sut.OnPostSelect(42);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchDetails", redirect.PageName);
        Assert.Equal(42, _session.Object.BatchID);
        Assert.False(_session.Object.IsViewSubmissionMode);
    }

    [Fact]
    public async Task PagedEntries_DefaultSort_OrdersByIdAscending()
    {
        _batches.Setup(b => b.GetOnHoldAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 3 }, new BatchListResult { ID = 1 }, new BatchListResult { ID = 2 }]);
        var sut = CreateSut();
        await sut.OnGetAsync();

        Assert.Equal([1, 2, 3], sut.PagedEntries.Select(b => b.ID));
    }
}
