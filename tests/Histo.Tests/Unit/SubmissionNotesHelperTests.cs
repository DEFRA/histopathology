using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="SubmissionNotesHelper.HasAnyNotesAsync"/> — the "Print submission
/// notes" enablement check. Confirms it detects comments at every level (header, block, tissue,
/// test), not just <c>Batch.Comments</c>/<c>StatusComments</c> — the reported gap where a
/// submission with only a block/tissue comment left the button disabled.
/// </summary>
public class SubmissionNotesHelperTests
{
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockTestService> _tests = new();

    public SubmissionNotesHelperTests()
    {
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42 });
        _blocks.Setup(b => b.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
    }

    private Task<bool> InvokeAsync() =>
        SubmissionNotesHelper.HasAnyNotesAsync(42, _batches.Object, _blocks.Object, _submissions.Object, _tests.Object);

    [Fact]
    public async Task NoCommentsAnywhere_ReturnsFalse()
    {
        Assert.False(await InvokeAsync());
    }

    [Fact]
    public async Task HeaderCommentOnly_ReturnsTrue()
    {
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42, Comments = "Header note" });
        Assert.True(await InvokeAsync());
    }

    [Fact]
    public async Task HeaderStatusCommentOnly_ReturnsTrue()
    {
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42, StatusComments = "Status note" });
        Assert.True(await InvokeAsync());
    }

    [Fact]
    public async Task BlockCommentOnly_NoHeaderComment_ReturnsTrue()
    {
        _blocks.Setup(b => b.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, Comment = "Block note" }]);
        Assert.True(await InvokeAsync());
    }

    [Fact]
    public async Task BlockArchiveCommentOnly_ReturnsTrue()
    {
        _blocks.Setup(b => b.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, ArchiveComment = "Archived note" }]);
        Assert.True(await InvokeAsync());
    }

    [Fact]
    public async Task TissueCommentOnly_NoHeaderComment_ReturnsTrue()
    {
        _submissions.Setup(s => s.GetTissuesByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 1, Comment = "Tissue note" }]);
        Assert.True(await InvokeAsync());
    }

    [Fact]
    public async Task TestCommentOnly_NoHeaderComment_ReturnsTrue()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[new BlockTest { ID = 1, Comment = "Test note" }]);
        Assert.True(await InvokeAsync());
    }

    [Fact]
    public async Task WhitespaceOnlyComments_TreatedAsNoComment()
    {
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42, Comments = "   " });
        _blocks.Setup(b => b.GetByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 1, Comment = "" }]);
        Assert.False(await InvokeAsync());
    }
}
