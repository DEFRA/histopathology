using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Blocks;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Verifies <see cref="BlockDetailsModel"/>'s tissue-type dropdown filter correctly resolves the
/// CURRENT animal's own submission, not just the batch's first one.
///
/// Regression: neither <c>GetBatchAnimal</c> nor <c>GetBatchBlocksByID</c>'s animal result-set
/// ever returns BatchSubmissionID (confirmed live via the actual stored procedure output), so
/// <see cref="Animal.BatchSubmissionID"/> is always 0 in this page — falling back to the batch's
/// FIRST submission silently returned the WRONG animal's tissue list (or an empty one) for any
/// animal that isn't that first submission. Reproduced live: batch 33425/animal 103547 is
/// submission order 2 — its real tissue ("AGAR") at submission 69945 was skipped in favour of
/// submission 69944 (order 1, a different animal), leaving the dropdown empty.
/// </summary>
public class BlockDetailsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IBlockTestService> _blockTests = new();
    private readonly Mock<IHistologyRefService> _histologyRefs = new();

    public BlockDetailsModelTests()
    {
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _session.Setup(s => s.UserID).Returns(7);
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections());
        _blockTests.Setup(t => t.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private BlockDetailsModel CreateSut(int animalId, int blockId) =>
        new(_session.Object, _submissions.Object, _blocks.Object, _batches.Object, _lookups.Object, _blockTests.Object, _histologyRefs.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            BatchId = 5,
            AnimalId = animalId,
            BlockId = blockId,
        };

    [Fact]
    public async Task LoadSupportingData_ResolvesTissuesFromOwnSubmission_NotJustTheFirstOne()
    {
        // Animal 2's own submission (69945, order 2) has "AGAR" — the batch's FIRST submission
        // (69944, order 1) belongs to a different animal and has no tissues at all.
        var animal = new Animal { ID = 2, SenderRef = "S2" };
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[animal]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[
            new BatchSubmission { ID = 69944, BatchID = 5, AnimalID = 0, Order = 1 },
            new BatchSubmission { ID = 69945, BatchID = 5, AnimalID = 2, Order = 2 },
        ]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 69945, Owner = TissueOwner.Submission, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01", Order = 1 },
        ]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(9, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "AGAR", Name = "Agar" },
            new LookupItem { Code = "OTHER", Name = "Other tissue" },
        ]);
        var sut = CreateSut(animalId: 2, blockId: 319181);

        await sut.OnGetAsync();

        Assert.Contains(sut.TissueOptions, o => o.Code == "AGAR");
        Assert.DoesNotContain(sut.TissueOptions, o => o.Code == "OTHER");
    }

    [Fact]
    public async Task OnPostCancelAsync_StillInAddFlow_DeletesTheProvisionalBlock()
    {
        // Regression: clicking "Add block" auto-provisions a real DB row on first load (see
        // OnGetAsync's Add-mode branch) — Back must discard it if the user leaves without doing
        // anything, matching legacy btnCancel_Click, instead of leaving an empty orphan block.
        _blocks.Setup(b => b.DeleteBlockAsync(319181, 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = true;

        var result = await sut.OnPostCancelAsync();

        _blocks.Verify(b => b.DeleteBlockAsync(319181, 7, It.IsAny<CancellationToken>()), Times.Once);
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
    }

    [Fact]
    public async Task OnPostCancelAsync_EditingEstablishedBlock_DoesNotDeleteIt()
    {
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = false;

        await sut.OnPostCancelAsync();

        _blocks.Verify(b => b.DeleteBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
