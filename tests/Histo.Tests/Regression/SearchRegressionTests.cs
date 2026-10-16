using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Regression;

/// <summary>
/// Regression suite for the standalone search pages (scenario SEARCH-01..05 in
/// .github/agents/histo-testing-agent.agent.md §5). Unlike the other Regression files, these
/// pages are single-dependency and don't share state across page instances, so plain Moq mocks
/// are used rather than a stateful fake — the regression value here is in the parameter-routing
/// business rules (blank search = "browse all", Sender Ref takes precedence over Histology Ref),
/// not in persisted state.
/// </summary>
public class SearchRegressionTests
{
    private readonly Mock<ISessionService> _session = new();

    private static Microsoft.AspNetCore.Http.DefaultHttpContext NewHttpContext() => new();

    private SearchSenderModel CreateSearchSender(ISubmissionService submissions)
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, submissions)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()),
        };
    }

    private SearchBlockRefsModel CreateSearchBlockRefs(IBlockService blocks)
    {
        var httpContext = NewHttpContext();
        return new(_session.Object, blocks)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()), HttpContext = httpContext },
        };
    }

    // ── SearchSender ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchSender_BlankTerm_BrowsesAllSenderRefs()
    {
        var submissions = new Mock<ISubmissionService>();
        submissions.Setup(s => s.GetAnimalsBySenderRefAsync("", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 1, SenderRef = "A1" }, new SenderSearchResult { ID = 2, SenderRef = "A2" }]);
        var sut = CreateSearchSender(submissions.Object);

        await sut.OnGetAsync();

        Assert.True(sut.HasSearched);
        Assert.Equal(2, sut.Results.Count);
        submissions.Verify(s => s.GetAnimalsBySenderRefAsync("", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchSender_WithTerm_PassesTrimmedTermThrough()
    {
        var submissions = new Mock<ISubmissionService>();
        submissions.Setup(s => s.GetAnimalsBySenderRefAsync("ABC1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)[new SenderSearchResult { ID = 5, SenderRef = "ABC1" }]);
        var sut = CreateSearchSender(submissions.Object);
        sut.SenderRef = "  ABC1  ";

        await sut.OnGetAsync();

        Assert.Single(sut.Results);
        submissions.Verify(s => s.GetAnimalsBySenderRefAsync("ABC1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchSender_MoreThanOnePageOfResults_PagedResultsSlicesCorrectly()
    {
        var all = Enumerable.Range(1, 15).Select(n => new SenderSearchResult { ID = n, SenderRef = $"S{n:D2}" }).ToList();
        var submissions = new Mock<ISubmissionService>();
        submissions.Setup(s => s.GetAnimalsBySenderRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SenderSearchResult>)all);
        var sut = CreateSearchSender(submissions.Object);
        sut.PageNumber = 2;

        await sut.OnGetAsync();

        Assert.Equal(15, sut.Results.Count);
        Assert.Equal(5, sut.PagedResults.Count); // page size 10 -> 5 remaining on page 2
        Assert.Equal("S11", sut.PagedResults[0].SenderRef);
    }

    // ── SearchBlockRefs ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchBlockRefs_NeitherFieldFilled_SubmittedTrue_ShowsValidationErrorAndDoesNotCallTheService()
    {
        var blocks = new Mock<IBlockService>(MockBehavior.Strict);
        var sut = CreateSearchBlockRefs(blocks.Object);
        sut.Submitted = true;

        await sut.OnGetAsync();

        Assert.Equal("Enter the Sender Ref or the Histology Ref.", sut.Errors["SenderRef"]);
        Assert.False(sut.Searched);
        blocks.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchBlockRefs_NeitherFieldFilled_BareFirstVisit_ShowsNoErrorAndDoesNotCallTheService()
    {
        var blocks = new Mock<IBlockService>(MockBehavior.Strict);
        var sut = CreateSearchBlockRefs(blocks.Object);
        // Submitted defaults to false — a bare GET with no query string at all.

        await sut.OnGetAsync();

        Assert.Empty(sut.Errors);
        blocks.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchBlockRefs_SenderRefOnly_CallsBySenderRef()
    {
        var blocks = new Mock<IBlockService>();
        blocks.Setup(b => b.GetUsedBlockRefsBySenderRefAsync("S001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[new UsedBlockRef { BlockRef = 1, Status = BlockStatus.Used }]);
        var sut = CreateSearchBlockRefs(blocks.Object);
        sut.SenderRef = "S001";

        await sut.OnGetAsync();

        Assert.True(sut.Searched);
        blocks.Verify(b => b.GetUsedBlockRefsBySenderRefAsync("S001", It.IsAny<CancellationToken>()), Times.Once);
        blocks.Verify(b => b.GetUsedBlockRefsByHistologyRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchBlockRefs_HistologyRefOnly_CallsByHistologyRef()
    {
        var blocks = new Mock<IBlockService>();
        blocks.Setup(b => b.GetUsedBlockRefsByHistologyRefAsync("23/00100", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        var sut = CreateSearchBlockRefs(blocks.Object);
        sut.HistologyRef = "23/00100";

        await sut.OnGetAsync();

        blocks.Verify(b => b.GetUsedBlockRefsByHistologyRefAsync("23/00100", It.IsAny<CancellationToken>()), Times.Once);
        blocks.Verify(b => b.GetUsedBlockRefsBySenderRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchBlockRefs_BothFieldsFilled_SenderRefTakesPrecedence()
    {
        var blocks = new Mock<IBlockService>();
        blocks.Setup(b => b.GetUsedBlockRefsBySenderRefAsync("S002", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        var sut = CreateSearchBlockRefs(blocks.Object);
        sut.SenderRef = "S002";
        sut.HistologyRef = "23/00200"; // present too, but must be ignored

        await sut.OnGetAsync();

        blocks.Verify(b => b.GetUsedBlockRefsBySenderRefAsync("S002", It.IsAny<CancellationToken>()), Times.Once);
        blocks.Verify(b => b.GetUsedBlockRefsByHistologyRefAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchBlockRefs_NoUsedBlocksFound_ShowsTheOpenEndedUnusedPlaceholderRow()
    {
        var blocks = new Mock<IBlockService>();
        blocks.Setup(b => b.GetUsedBlockRefsBySenderRefAsync("S003", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<UsedBlockRef>)[]);
        var sut = CreateSearchBlockRefs(blocks.Object);
        sut.SenderRef = "S003";

        await sut.OnGetAsync();

        var row = Assert.Single(sut.Results);
        Assert.Null(row.UsedBlockRefs);
        Assert.Equal("01+", row.UnusedBlockRefs);
        Assert.Null(row.PreBookedBlockRefs);
    }
}
