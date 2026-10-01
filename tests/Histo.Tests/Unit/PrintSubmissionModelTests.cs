using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="PrintSubmissionModel"/> — in particular that "Print submission
/// notes" is enabled from a block/tissue/test-level comment alone, with no header comment.
/// </summary>
public class PrintSubmissionModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockTestService> _tests = new();

    public PrintSubmissionModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.Object.BatchID = 42;
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42 });
        _blocks.Setup(b => b.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
    }

    private PrintSubmissionModel CreateSut() =>
        new(_session.Object, _batches.Object, _blocks.Object, _submissions.Object, _tests.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnGetAsync_NoCommentsAnywhere_HasNotesFalse()
    {
        var sut = CreateSut();
        await sut.OnGetAsync();
        Assert.False(sut.HasNotes);
    }

    [Fact]
    public async Task OnGetAsync_BlockCommentOnly_NoHeaderComment_HasNotesTrue()
    {
        _blocks.Setup(b => b.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, Comment = "Block note" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.True(sut.HasNotes);
    }

    [Fact]
    public async Task OnGetAsync_TissueCommentOnly_HasNotesTrue()
    {
        _submissions.Setup(s => s.GetTissuesByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 1, ArchiveComment = "Archived tissue note" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.True(sut.HasNotes);
    }
}
