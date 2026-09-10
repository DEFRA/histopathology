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

/// <summary>Unit tests for <see cref="BatchesForDispatchModel"/>.</summary>
public class BatchesForDispatchModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public BatchesForDispatchModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.IsViewSubmissionMode, true);
    }

    private BatchesForDispatchModel CreateSut() =>
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
        _batches.Setup(b => b.GetForDispatchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[
                new BatchListResult { ID = 1 }, new BatchListResult { ID = 3 }, new BatchListResult { ID = 2 }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal([3, 2, 1], sut.PagedEntries.Select(b => b.ID));
    }
}
