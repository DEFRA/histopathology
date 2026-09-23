using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="BatchesForEditingModel"/>.</summary>
public class BatchesForEditingModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();

    public BatchesForEditingModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
        _session.SetupProperty(s => s.ReturnPageQuery, string.Empty);
        _session.SetupProperty(s => s.IsViewSubmissionMode);
    }

    private BatchesForEditingModel CreateSut(string queryString = "") =>
        new(_session.Object, _batches.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    Request = { QueryString = new Microsoft.AspNetCore.Http.QueryString(queryString) },
                },
            },
        };

    [Fact]
    public async Task OnGetAsync_NoColumnClicked_DefaultsToIdDescendingRegardlessOfSortDesc()
    {
        _batches.Setup(b => b.GetAllBatchesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchListResult>)[
                new BatchListResult { ID = 1 },
                new BatchListResult { ID = 2 },
                new BatchListResult { ID = 3 },
            ]);
        var sut = CreateSut();
        sut.SortDesc = false; // no SortColumn clicked — must still default to ID DESC

        await sut.OnGetAsync();

        Assert.Equal([3, 2, 1], sut.PagedEntries.Select(b => b.ID));
    }

    [Fact]
    public void OnPostSelect_CapturesQueryStringIntoReturnPageQuery()
    {
        var sut = CreateSut("?SortColumn=ProjectDescription&SortDesc=true&PageNumber=2");

        var result = sut.OnPostSelect(42);

        Assert.Equal("/Batches/BatchesForEditing", _session.Object.ReturnPage);
        Assert.Equal("?SortColumn=ProjectDescription&SortDesc=true&PageNumber=2", _session.Object.ReturnPageQuery);
    }
}
