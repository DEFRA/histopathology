using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Column sorting of the Batches* list pages; rows 1,2,3 carry ranks 2,3,1 so ascending order is 3,1,2.</summary>
public class BatchListSortingTests
{
    private static PageContext Ctx() => new()
    {
        HttpContext = new DefaultHttpContext(),
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
    };

    private static BatchListResult Make(string col, int id, int rank) => col switch
    {
        "ID" => new() { ID = rank },
        "ProjectDescription" => new() { ID = id, ProjectDescription = rank.ToString() },
        "ContactDescription" => new() { ID = id, ContactDescription = rank.ToString() },
        "Species" => new() { ID = id, Species = rank.ToString() },
        "BatchDate" => new() { ID = id, BatchDate = new DateTime(2024, 1, rank) },
        "CompletedDate" => new() { ID = id, CompletedDate = new DateTime(2024, 1, rank) },
        "SubmissionDate" => new() { ID = id, SubmissionDate = rank.ToString() },
        "OtherSubmittedBy" => new() { ID = id, OtherSubmittedBy = rank.ToString() },
        "Status" => new() { ID = id, Status = rank.ToString() },
        "AllTissuesAssigned" => new() { ID = id, AllTissuesAssigned = rank != 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(col)),
    };

    private static IReadOnlyList<BatchListResult> Rows(string col) => [Make(col, 1, 2), Make(col, 2, 3), Make(col, 3, 1)];

    private static void AssertSorted(string col, bool desc, IReadOnlyList<BatchListResult> paged)
    {
        var ids = paged.Select(b => b.ID).ToArray();
        if (col == "AllTissuesAssigned")
        {
            // Two rows tie on the flag; only the single unchecked row (ID 3) has a fixed position.
            Assert.Equal(3, desc ? ids[^1] : ids[0]);
            return;
        }

        int[] ascending = col == "ID" ? [1, 2, 3] : [3, 1, 2];
        Assert.Equal(desc ? ascending.Reverse().ToArray() : ascending, ids);
    }

    private static ISessionService Session() => new Mock<ISessionService>().Object;

    [Theory]
    [InlineData("ProjectDescription", false)]
    [InlineData("ProjectDescription", true)]
    [InlineData("ContactDescription", false)]
    [InlineData("Species", false)]
    [InlineData("CompletedDate", false)]
    [InlineData("CompletedDate", true)]
    [InlineData("ID", false)]
    public async Task ForArchiving_SortsByColumn(string col, bool desc)
    {
        var batches = new Mock<IBatchService>();
        batches.Setup(b => b.GetCompletedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Rows(col));
        var sut = new BatchesForArchivingModel(Session(), batches.Object) { PageContext = Ctx(), SortColumn = col, SortDesc = desc };

        await sut.OnGetAsync();

        AssertSorted(col, desc, sut.GetPagedEntries());
    }

    [Theory]
    [InlineData("ProjectDescription", false)]
    [InlineData("ContactDescription", true)]
    [InlineData("Species", false)]
    [InlineData("BatchDate", false)]
    [InlineData("BatchDate", true)]
    [InlineData("ID", false)]
    public async Task ForDispatch_SortsByColumn(string col, bool desc)
    {
        var batches = new Mock<IBatchService>();
        batches.Setup(b => b.GetForDispatchAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Rows(col));
        var sut = new BatchesForDispatchModel(Session(), batches.Object) { PageContext = Ctx(), SortColumn = col, SortDesc = desc };

        await sut.OnGetAsync();

        AssertSorted(col, desc, sut.GetPagedEntries());
    }

    [Theory]
    [InlineData("ProjectDescription", false)]
    [InlineData("ContactDescription", true)]
    [InlineData("Species", false)]
    [InlineData("BatchDate", false)]
    [InlineData("Status", false)]
    [InlineData("Status", true)]
    public async Task ForEditing_SortsByColumn(string col, bool desc)
    {
        var batches = new Mock<IBatchService>();
        batches.Setup(b => b.GetAllBatchesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Rows(col));
        var sut = new BatchesForEditingModel(Session(), batches.Object) { PageContext = Ctx(), SortColumn = col, SortDesc = desc };

        await sut.OnGetAsync();

        AssertSorted(col, desc, sut.GetPagedEntries());
    }

    [Theory]
    [InlineData("ProjectDescription", false)]
    [InlineData("ContactDescription", false)]
    [InlineData("Species", true)]
    [InlineData("SubmissionDate", false)]
    [InlineData("SubmissionDate", true)]
    [InlineData("OtherSubmittedBy", false)]
    public async Task NotReceived_SortsByColumn(string col, bool desc)
    {
        var batches = new Mock<IBatchService>();
        batches.Setup(b => b.GetNotReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Rows(col));
        var sut = new BatchesNotReceivedModel(Session(), batches.Object) { PageContext = Ctx(), SortColumn = col, SortDesc = desc };

        await sut.OnGetAsync();

        AssertSorted(col, desc, sut.GetPagedEntries());
    }

    [Theory]
    [InlineData("ID", false)]
    [InlineData("ProjectDescription", false)]
    [InlineData("ContactDescription", true)]
    [InlineData("Species", false)]
    [InlineData("BatchDate", false)]
    [InlineData("AllTissuesAssigned", false)]
    [InlineData("AllTissuesAssigned", true)]
    public async Task Received_SortsByColumn(string col, bool desc)
    {
        var batches = new Mock<IBatchService>();
        batches.Setup(b => b.GetReceivedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Rows(col));
        var sut = new BatchesReceivedModel(Session(), batches.Object) { PageContext = Ctx(), SortColumn = col, SortDesc = desc };

        await sut.OnGetAsync();

        AssertSorted(col, desc, sut.GetPagedEntries());
    }
}
