using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="BatchesForArchivingModel"/>.</summary>
public class BatchesForArchivingModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public BatchesForArchivingModelTests()
    {
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
    }

    private BatchesForArchivingModel CreateSut() =>
        new(_session.Object, _batches.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnGetAsync_NoSortColumn_DefaultsToIdDescending()
    {
        _batches.Setup(b => b.GetCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[
                new BatchListResult { ID = 1 }, new BatchListResult { ID = 3 }, new BatchListResult { ID = 2 }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal([3, 2, 1], sut.PagedEntries.Select(b => b.ID));
    }

    [Fact]
    public async Task OnPostGoAsync_NoQuickGoId_ReturnsError()
    {
        _batches.Setup(b => b.GetCompletedAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchListResult>)[]);
        var sut = CreateSut();

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a submission number.", sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_SubmissionNotInList_ReturnsError()
    {
        _batches.Setup(b => b.GetCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 1 }]);
        var sut = CreateSut();
        sut.QuickGoId = 999;

        var result = await sut.OnPostGoAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("999", sut.GoError);
    }

    [Fact]
    public async Task OnPostGoAsync_ValidSubmission_RedirectsToArchiveMenu()
    {
        _batches.Setup(b => b.GetCompletedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[new BatchListResult { ID = 42 }]);
        var sut = CreateSut();
        sut.QuickGoId = 42;

        var result = await sut.OnPostGoAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Archive/ArchiveMenu", redirect.PageName);
        Assert.Equal(42, redirect.RouteValues!["batchId"]);
        Assert.Equal("/Batches/BatchesForArchiving", _session.Object.ReturnPage);
    }
}
