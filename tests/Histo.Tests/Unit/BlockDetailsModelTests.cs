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
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
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
    public async Task ExistingHistologyCodes_IncludesSpecialStainIhcPrpAndIhcOther()
    {
        // Regression: GetByBatchAsync excludes Histology codes 3/4/6 (QC-worklist-only gating
        // flags there) — the Tests checkbox pre-population on Edit block must read from
        // GetAllSelectionsByBatchAsync, or a block that already has Special Stain/IHC-Prp/
        // IHC-Other selected would show them unchecked when reopened, risking a duplicate
        // insert next time "Done" is saved (SaveTestSelectionsAsync would never see the existing row).
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[
            new BlockTest { ID = 1, BlockID = 319181, TestType = BlockTestType.Histology, Code = HistologyCode.SpecialStain },
            new BlockTest { ID = 2, BlockID = 319181, TestType = BlockTestType.Histology, Code = HistologyCode.IhcOther },
        ]);
        var sut = CreateSut(animalId: 2, blockId: 319181);

        await sut.OnGetAsync();

        Assert.Contains(HistologyCode.SpecialStain, sut.ExistingHistologyCodes);
        Assert.Contains(HistologyCode.IhcOther, sut.ExistingHistologyCodes);
    }

    [Fact]
    public async Task OnPostCancelAsync_StillInAddFlow_DeletesTheProvisionalBlock()
    {
        // Regression: clicking "Add block" auto-provisions a real DB row on first load (see
        // OnGetAsync's Add-mode branch) — Back must discard it if the user leaves without doing
        // anything, matching legacy btnCancel_Click, instead of leaving an empty orphan block.
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.DeleteBlockAsync(319181, 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = true;

        var result = await sut.OnPostCancelAsync();

        _blocks.Verify(b => b.DeleteBlockAsync(319181, 7, It.IsAny<CancellationToken>()), Times.Once);
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
    }

    [Fact]
    public async Task OnPostCancelAsync_StillInAddFlow_WithTissuesAdded_DeletesTissuesAndBlock()
    {
        // Regression: EO/H&E (or any) test-selection conflict blocks "Done", but "Add tissue" saves
        // immediately and independently — Cancel must still be able to fully discard a never-"Done"
        // block even when tissues were already added, instead of silently no-oping on the FK-guarded
        // block delete and leaving an incomplete block + its tissues behind.
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR" },
            new Tissue { ID = 2, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "OTHER" },
        ]);
        _submissions.Setup(s => s.DeleteTissueAsync(It.IsAny<int>(), TissueOwner.Block, 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _blocks.Setup(b => b.DeleteBlockAsync(319181, 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = true;

        await sut.OnPostCancelAsync();

        _submissions.Verify(s => s.DeleteTissueAsync(1, TissueOwner.Block, 7, It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.DeleteTissueAsync(2, TissueOwner.Block, 7, It.IsAny<CancellationToken>()), Times.Once);
        _blocks.Verify(b => b.DeleteBlockAsync(319181, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostCancelAsync_SaveTestSelectionsThrows_StillDeletesBlockAndRedirects()
    {
        // Regression: BlockTestService.SaveTestSelectionsAsync re-throws on failure (unlike the
        // other cleanup calls here) — Cancel must still complete and redirect, not surface a 500.
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blockTests.Setup(t => t.SaveTestSelectionsAsync(5, 319181, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(), 7, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
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

    [Fact]
    public async Task OnPostDoneAsync_NoTissuesAssigned_SetsErrorAndDoesNotSaveTheBlock()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.NewBlockRef = "01";
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostDoneAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Add at least one tissue to this block before completing it.", sut.ErrorMessage);
        _blocks.Verify(b => b.UpdateBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_AddMode_HistopathAreaUser_NoHistologyRef_RedirectsWithGdsError()
    {
        // Mirrors SubmissionDetailsBlockModel.HistologyRefRequired — Histopath-area users must set
        // a Histology Reference before a block can even be auto-provisioned.
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", HistoRefSet = false }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        var sut = CreateSut(animalId: 2, blockId: 0);

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
        Assert.Equal("Enter a Histology Reference for this sample before adding a block.", sut.TempData["SubmissionDetailsBlock_Error"]);
        Assert.Equal("EditHistologyRef", sut.TempData["SubmissionDetailsBlock_ErrorFieldId"]);
        _blocks.Verify(b => b.AddBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>(),
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_AddMode_NonHistopathArea_PreCassetted_NoHistologyRef_RedirectsWithGdsError()
    {
        _session.Setup(s => s.UserArea).Returns("Neuropath");
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, IsPreCassetted = true });
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", HistoRefSet = false }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        var sut = CreateSut(animalId: 2, blockId: 0);

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
        _blocks.Verify(b => b.AddBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>(),
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_AddMode_NonHistopathArea_NotPreCassetted_NoHistologyRef_StillCreatesBlock()
    {
        // Regression guard: non-Histopath, non-Pre-Cassetted submissions keep the Histology Ref
        // genuinely optional — must NOT be gated.
        _session.Setup(s => s.UserArea).Returns("Neuropath");
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", HistoRefSet = false }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _blocks.Setup(b => b.AddBlockAsync(5, 2, It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(400);
        var sut = CreateSut(animalId: 2, blockId: 0);

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Null(redirect.PageName);
        _blocks.Verify(b => b.AddBlockAsync(5, 2, It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_AddMode_ExistingBlockHasTissue_SkipsItsRefForTheNewBlock()
    {
        // Block "01" already has a tissue, so it was genuinely used — the next block must get
        // "02", not reuse "01".
        _session.Setup(s => s.UserArea).Returns("Neuropath");
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", HistoRefSet = false }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 300, BatchID = 5, AnimalID = 2, BlockRef = "01", Order = 1 },
        ]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 300, Owner = TissueOwner.Block, TissueCode = "AGAR" },
        ]);
        _blocks.Setup(b => b.AddBlockAsync(5, 2, "02", It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(400);
        var sut = CreateSut(animalId: 2, blockId: 0);

        var result = await sut.OnGetAsync();

        Assert.Equal("02", sut.NewBlockRef);
        _blocks.Verify(b => b.AddBlockAsync(5, 2, "02", It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_AddMode_ExistingBlockIsAbandoned_ReusesItInsteadOfCreatingDuplicate()
    {
        // Block "01" exists but has zero tissues — never actually finished (e.g. backed out of
        // earlier). The next "Add block" click must resume it, not skip to "02" and create a
        // second, duplicate row.
        _session.Setup(s => s.UserArea).Returns("Neuropath");
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", HistoRefSet = false }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 300, BatchID = 5, AnimalID = 2, BlockRef = "01", Order = 1 },
        ]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        var sut = CreateSut(animalId: 2, blockId: 0);

        var result = await sut.OnGetAsync();

        Assert.Equal("01", sut.NewBlockRef);
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal(300, redirect.RouteValues?["blockId"]);
        _blocks.Verify(b => b.AddBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>(),
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_AddMode_PreCassetted_ClaimsThePreBookedPlaceholderInsteadOfInserting()
    {
        // Regression (UAT TEST002.5): block "01" was already used for an earlier submission on this
        // sender ref, but its pre-booked placeholder (BatchID NULL, Status PreBooked) was never
        // retired — so it kept being returned as the "next" ref for every later submission. The
        // fix claims the matching placeholder row in place (ClaimPreBookedBlockAsync) instead of
        // inserting a new row via AddBlockAsync, which would otherwise create a duplicate "01".
        _session.Setup(s => s.UserArea).Returns("Neuropath");
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, IsPreCassetted = true });
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", HistoRefSet = true }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        // This batch (a secondary submission) has no blocks of its own yet.
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        // "01" is a stale pre-booked placeholder (already used elsewhere for this animal); "02" is genuinely free.
        var preBooked01 = new Block { ID = 900, AnimalID = 2, BlockRef = "01", Status = BlockStatus.PreBooked };
        _blocks.Setup(b => b.GetPreBookedByAnimalAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            preBooked01,
            new Block { ID = 901, AnimalID = 2, BlockRef = "02", Status = BlockStatus.PreBooked },
        ]);
        _blocks.Setup(b => b.ClaimPreBookedBlockAsync(preBooked01, 5, It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = CreateSut(animalId: 2, blockId: 0);

        var result = await sut.OnGetAsync();

        Assert.Equal("01", sut.NewBlockRef);
        _blocks.Verify(b => b.ClaimPreBookedBlockAsync(preBooked01, 5, It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()), Times.Once);
        _blocks.Verify(b => b.AddBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>(),
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal(900, redirect.RouteValues?["blockId"]);
    }

    [Fact]
    public async Task OnPostDoneAsync_AddFlow_HistopathAreaUser_NoHistologyRef_SetsErrorAndDoesNotSave()
    {
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", HistoRefSet = false }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = true;
        sut.NewBlockRef = "01";
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostDoneAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a Histology Reference for this sample before completing this block.", sut.ErrorMessage);
        _blocks.Verify(b => b.UpdateBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostDoneAsync_EditingExistingBlock_NoHistologyRef_StillCompletes()
    {
        // Legacy's own ValidateMandatoryFields never checks Histology Ref — only Block Ref/Number
        // of blocks — so editing an already-existing block must not become a new, stricter rule.
        _session.Setup(s => s.UserArea).Returns("Histopath");
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", HistoRefSet = false }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _blocks.Setup(b => b.UpdateBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = false;
        sut.NewBlockRef = "01";
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostDoneAsync();

        Assert.IsNotType<PageResult>(result);
        _blocks.Verify(b => b.UpdateBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_Success_NewBlockIsCancellableViaBack()
    {
        // Regression: next-block-form previously had no IsAddFlow hidden field, so the sibling
        // block created here always rendered with a plain Back link instead of the Cancel-and-
        // delete button, orphaning it if the user backed out without adding anything.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _blocks.Setup(b => b.AddBlockAsync(5, 2, It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(319182);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = false; // editing an established block when "Next block" is clicked
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostNextBlockAsync();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal(true, redirect.RouteValues?["isAddFlow"]);
        Assert.Equal(319182, redirect.RouteValues?["blockId"]);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_PreCassetted_NoPreBookedRefsLeft_SetsErrorAndDoesNotCreateABlock()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, IsPreCassetted = true });
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _blocks.Setup(b => b.GetPreBookedByAnimalAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = true; // original block was itself still in add-flow
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostNextBlockAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("There are no more pre-booked block references available for this sample.", sut.ErrorMessage);
        _blocks.Verify(b => b.AddBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>(),
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_PreCassetted_ClaimsThePreBookedPlaceholderInsteadOfInserting()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, IsPreCassetted = true });
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        var preBooked02 = new Block { ID = 901, AnimalID = 2, BlockRef = "02", Status = BlockStatus.PreBooked };
        _blocks.Setup(b => b.GetPreBookedByAnimalAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[preBooked02]);
        _blocks.Setup(b => b.ClaimPreBookedBlockAsync(preBooked02, 5, It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = false;
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostNextBlockAsync();

        _blocks.Verify(b => b.ClaimPreBookedBlockAsync(preBooked02, 5, It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()), Times.Once);
        _blocks.Verify(b => b.AddBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>(),
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal(901, redirect.RouteValues?["blockId"]);
    }
}

