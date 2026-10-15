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
        new(_session.Object, _submissions.Object, _blocks.Object, _batches.Object, _lookups.Object, _blockTests.Object, _histologyRefs.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BlockDetailsModel>.Instance)
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
        sut.UseWholeTissueList = false; // the filtered view is opt-in; the default is the full list

        await sut.OnGetAsync();

        Assert.Contains(sut.TissueOptions, o => o.Code == "AGAR");
        Assert.DoesNotContain(sut.TissueOptions, o => o.Code == "OTHER");
    }

    [Fact]
    public async Task LoadSupportingData_DefaultsToTheWholeTissueList()
    {
        var animal = new Animal { ID = 2, SenderRef = "S2" };
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[animal]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[
            new BatchSubmission { ID = 69945, BatchID = 5, AnimalID = 2, Order = 1 },
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

        Assert.True(sut.UseWholeTissueList);
        Assert.Contains(sut.TissueOptions, o => o.Code == "OTHER");
    }

    [Fact]
    public async Task LoadSupportingData_AssignTissueJourney_OnlyOffersTheSubmissionsTissues()
    {
        // Reached from Assign Tissues to Blocks (BatchBlocks): the list is the submission's tissues
        // only, even though the whole-list default is on and no toggle is offered.
        _session.Setup(s => s.SampleDetailReturnPage).Returns("/Batches/BatchBlocks?batchId=5");
        var animal = new Animal { ID = 2, SenderRef = "S2" };
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[animal]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[
            new BatchSubmission { ID = 69945, BatchID = 5, AnimalID = 2, Order = 1 },
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
        sut.UseWholeTissueList = true;

        await sut.OnGetAsync();

        Assert.True(sut.IsAssignTissueMode);
        Assert.Contains(sut.TissueOptions, o => o.Code == "AGAR");
        Assert.DoesNotContain(sut.TissueOptions, o => o.Code == "OTHER");
    }

    [Fact]
    public async Task LoadSupportingData_AssignTissueJourney_SampleWithNoSubmissionRow_OffersNoOtherSamplesTissues()
    {
        // The batch's first submission (the empty default, or another sample's) must never stand in
        // for a sample that has no submission of its own.
        _session.Setup(s => s.SampleDetailReturnPage).Returns("/Batches/BatchBlocks?batchId=5");
        var animal = new Animal { ID = 2, SenderRef = "S2" };
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[animal]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[
            new BatchSubmission { ID = 100, BatchID = 5, AnimalID = 9, Order = 1 },
        ]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 100, Owner = TissueOwner.Submission, TissueCode = "AGAR", NoPieces = 1 },
            new Tissue { ID = 2, OwnerID = 0, Owner = TissueOwner.Submission, TissueCode = "OTHER", NoPieces = 1 },
        ]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(9, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "AGAR", Name = "Agar" },
            new LookupItem { Code = "OTHER", Name = "Other tissue" },
        ]);
        var sut = CreateSut(animalId: 2, blockId: 319181);

        await sut.OnGetAsync();

        Assert.Empty(sut.TissueOptions);
    }

    [Fact]
    public async Task LoadSupportingData_AssignTissueJourney_SampleWithSeveralSubmissions_OffersAllTheirTissues()
    {
        _session.Setup(s => s.SampleDetailReturnPage).Returns("/Batches/BatchBlocks?batchId=5");
        var animal = new Animal { ID = 2, SenderRef = "S2" };
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[animal]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[
            new BatchSubmission { ID = 100, BatchID = 5, AnimalID = 2, Order = 1 },
            new BatchSubmission { ID = 101, BatchID = 5, AnimalID = 2, Order = 2 },
        ]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 100, Owner = TissueOwner.Submission, TissueCode = "AGAR", NoPieces = 1 },
            new Tissue { ID = 2, OwnerID = 101, Owner = TissueOwner.Submission, TissueCode = "BRN", NoPieces = 1 },
        ]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(9, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "AGAR", Name = "Agar" },
            new LookupItem { Code = "BRN", Name = "Brain" },
            new LookupItem { Code = "OTHER", Name = "Other tissue" },
        ]);
        var sut = CreateSut(animalId: 2, blockId: 319181);

        await sut.OnGetAsync();

        Assert.Equal(["AGAR", "BRN"], sut.TissueOptions.Select(o => o.Code));
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
    public async Task OnGetAsync_AddMode_PersistsBatchDefaultsForTheNewBlock()
    {
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
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 5, Code = HistologyCode.EO }, new BatchTestSelectionRow { ID = 2, BatchID = 5, Code = HistologyCode.HAndE }],
            Antibodies = [new BatchTestSelectionRow { ID = 3, BatchID = 5, Code = "AB01" }],
            Stains = [new BatchTestSelectionRow { ID = 4, BatchID = 5, Code = "ST01" }],
        });

        await CreateSut(animalId: 2, blockId: 0).OnGetAsync();

        _blockTests.Verify(t => t.SaveTestSelectionsAsync(
            5, 400,
            It.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { HistologyCode.EO, HistologyCode.HAndE })),
            It.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "AB01" })),
            It.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "ST01" })),
            7,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_AddMode_DefaultSaveFails_DeletesTheNewBlockAndShowsAnError()
    {
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
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 5, Code = HistologyCode.HAndE }],
        });
        _blockTests.Setup(t => t.SaveTestSelectionsAsync(5, 400, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(), 7, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var sut = CreateSut(animalId: 2, blockId: 0);

        var result = await sut.OnGetAsync();

        Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
        Assert.NotNull(sut.ErrorMessage);
        _blocks.Verify(b => b.DeleteBlockAsync(400, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_NewBlockRef_NeverGoesBelowTheAnimalsStoredNextBlockRef()
    {
        // 01/02 remain after 03 was deleted, and the animal's counter says 04: the next block must be 04.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", NextBlockRef = "04" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 1, BatchID = 5, AnimalID = 2, BlockRef = "01" },
            new Block { ID = 2, BatchID = 5, AnimalID = 2, BlockRef = "02" },
        ]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 2, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 2, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        _blocks.Setup(b => b.AddBlockAsync(5, 2, It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(9);
        var sut = CreateSut(animalId: 2, blockId: 2);
        sut.NewBlockRef = "02";
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        await sut.OnPostNextBlockAsync();

        _blocks.Verify(b => b.AddBlockAsync(5, 2, "04", It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task OnGetAsync_AddMode_PreviousBlockWasDeleted_NextRefContinuesPastIt()
    {
        // Block "01" was added and then deleted, which advanced the animal's NextBlockRef to "02".
        // With no blocks left, a computed max+1 alone would hand "01" out again.
        _session.Setup(s => s.UserArea).Returns("Neuropath");
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", NextBlockRef = "02" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _blocks.Setup(b => b.AddBlockAsync(5, 2, "02", It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(400);
        var sut = CreateSut(animalId: 2, blockId: 0);

        await sut.OnGetAsync();

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
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        _blocks.Setup(b => b.AddBlockAsync(5, 2, It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(319182);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = false; // editing an established block when "Next block" is clicked
        sut.NewBlockRef = "01";
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
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, IsPreCassetted = true });
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _blocks.Setup(b => b.GetPreBookedByAnimalAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = true; // original block was itself still in add-flow
        sut.NewBlockRef = "01";
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
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        var preBooked02 = new Block { ID = 901, AnimalID = 2, BlockRef = "02", Status = BlockStatus.PreBooked };
        _blocks.Setup(b => b.GetPreBookedByAnimalAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[preBooked02]);
        _blocks.Setup(b => b.ClaimPreBookedBlockAsync(preBooked02, 5, It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsAddFlow = false;
        sut.NewBlockRef = "01";
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostNextBlockAsync();

        _blocks.Verify(b => b.ClaimPreBookedBlockAsync(preBooked02, 5, It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()), Times.Once);
        _blocks.Verify(b => b.AddBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>(),
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal(901, redirect.RouteValues?["blockId"]);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_PersistsCurrentBlockRefCustomerRefRepeatAndComment()
    {
        // Phase F: legacy btnAddBlock_Click calls UpdateBlockDetails() same as btnDone_Click —
        // Next block must persist these fields for the current block too, not just Done.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        _blocks.Setup(b => b.UpdateBlockAsync(It.IsAny<Block>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _blocks.Setup(b => b.AddBlockAsync(5, 2, It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(319182);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.NewBlockRef = "01";
        sut.NewCustomerRef = "CUST-1";
        sut.NewRepeatBlock = true;
        sut.NewComment = "a comment";
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        await sut.OnPostNextBlockAsync();

        _blocks.Verify(b => b.UpdateBlockAsync(
            It.Is<Block>(blk => blk.ID == 319181 && blk.CustomerRef == "CUST-1" && blk.Comment == "a comment" && blk.RepeatBlock == true),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_NoTissuesAndNotRepeatBlock_SetsErrorAndDoesNotCreateNextBlock()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.NewBlockRef = "01";
        sut.NewRepeatBlock = false;
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostNextBlockAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Add at least one tissue to this block before completing it.", sut.ErrorMessage);
        _blocks.Verify(b => b.AddBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>(),
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_RepeatBlockTicked_ZeroTissuesStillProceeds()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.AddBlockAsync(5, 2, It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(319182);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.NewBlockRef = "01";
        sut.NewRepeatBlock = true;
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostNextBlockAsync();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal(319182, redirect.RouteValues?["blockId"]);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_NumberOfBlocksGreaterThanOne_DuplicatesSiblingsBeforeCreatingNextBlock()
    {
        // Legacy CreateMultiBlocks runs on Next Block too, not just Done.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2", NextBlockRef = "01" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        // SaveCurrentBlockDetailsAsync duplicates block 319181's tissues onto sibling 400 — this
        // must reflect that so block 400 is not mistaken for an abandoned/orphaned block by the
        // "next ref" computation's zero-tissue exclusion.
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
            new Tissue { ID = 2, OwnerID = 400, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        // First call (before the sibling is created) sees only the original block; the second
        // (after SaveCurrentBlockDetailsAsync's duplication) sees the newly-created sibling too —
        // mocks can't mutate themselves the way a real DB insert would, so simulate that with a
        // sequence rather than one static return value.
        _blocks.SetupSequence(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[
                new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01", Order = 1 },
            ])
            .ReturnsAsync((IReadOnlyList<Block>)[
                new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01", Order = 1 },
                new Block { ID = 400, BatchID = 5, AnimalID = 2, BlockRef = "02", Order = 2 },
            ]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        // "02" is the sibling duplicated by SaveCurrentBlockDetailsAsync; "03" is the actual next block.
        _blocks.Setup(b => b.AddBlockAsync(5, 2, "02", It.IsAny<IEnumerable<int>>(), 7, "CUST", null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(400);
        _blocks.Setup(b => b.AddBlockAsync(5, 2, "03", It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(500);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.NewBlockRef = "01";
        sut.NewCustomerRef = "CUST";
        sut.NewNumberOfBlocks = 2;
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostNextBlockAsync();

        _blocks.Verify(b => b.AddBlockAsync(5, 2, "02", It.IsAny<IEnumerable<int>>(), 7, "CUST", null, false, It.IsAny<CancellationToken>()), Times.Once);
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal(500, redirect.RouteValues?["blockId"]);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_CarryTestsToNextBlock_SavesSameCodesOntoNewBlock()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        _blocks.Setup(b => b.AddBlockAsync(5, 2, It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), 7, null, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(319182);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.NewBlockRef = "01";
        sut.SelectedHistologyCodes = [HistologyCode.EO];
        sut.SelectedAntibodyCodes = ["AB01"];
        sut.SelectedStainCodes = ["ST01"];
        sut.CarryTestsToNextBlock = true;

        await sut.OnPostNextBlockAsync();

        _blockTests.Verify(t => t.SaveTestSelectionsAsync(
            5, 319182,
            It.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { HistologyCode.EO })),
            It.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { "AB01" })),
            It.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { "ST01" })),
            7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostDoneAsync_Success_RedirectsToSubmissionDetailsBlock()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
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
        sut.NewBlockRef = "01";
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostDoneAsync();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
    }

    [Fact]
    public async Task LoadTestOptions_NarrowsToBatchLevelSelection()
    {
        // Legacy: HideTests() — the full lookup table has more codes than the submission chose
        // at Create/Edit Submission time; only the chosen ones must ever be offered.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = HistologyCode.EO, Name = "EO" },
            new LookupItem { Code = HistologyCode.HAndE, Name = "H&E" },
            new LookupItem { Code = HistologyCode.SpecialStain, Name = "Special Stain" },
        ]);
        _lookups.Setup(l => l.GetLookupDataAsync(4, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "AB01", Name = "Antibody 1" },
            new LookupItem { Code = "AB02", Name = "Antibody 2" },
        ]);
        _lookups.Setup(l => l.GetLookupDataAsync(6, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "ST01", Name = "Stain 1" },
            new LookupItem { Code = "ST02", Name = "Stain 2" },
        ]);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 5, Code = HistologyCode.EO }],
            Antibodies = [new BatchTestSelectionRow { ID = 2, BatchID = 5, Code = "AB01" }],
            Stains = [new BatchTestSelectionRow { ID = 3, BatchID = 5, Code = "ST01" }],
        });
        var sut = CreateSut(animalId: 2, blockId: 319181);

        await sut.OnGetAsync();

        Assert.Equal([HistologyCode.EO], sut.HistologyOptions.Select(o => o.Code));
        Assert.Equal(["AB01"], sut.AntibodyOptions.Select(o => o.Code));
        Assert.Equal(["ST01"], sut.StainOptions.Select(o => o.Code));
    }

    [Fact]
    public async Task LoadEditModeData_FirstBlockNoSavedTests_DefaultsToBatchSelections()
    {
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 5, Code = HistologyCode.EO }],
        });
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.IsFromNextBlock = false;

        await sut.OnGetAsync();

        Assert.Equal([HistologyCode.EO], sut.ExistingHistologyCodes);
    }

    [Fact]
    public async Task LoadEditModeData_NextBlockCreatedBlockNoSavedTests_StaysUnselected()
    {
        // Phase B fix: distinguishes a block created via "Next block" (should stay blank unless
        // carried forward) from the very first block (should default to the batch selections).
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319182, BatchID = 5, AnimalID = 2, BlockRef = "02" },
        ]);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 5, Code = HistologyCode.EO }],
        });
        var sut = CreateSut(animalId: 2, blockId: 319182);
        sut.IsFromNextBlock = true;

        await sut.OnGetAsync();

        Assert.Empty(sut.ExistingHistologyCodes);
    }

    [Fact]
    public async Task LoadEditModeData_BlockSelectionsAreIndependentOfOtherBlocks()
    {
        // Block 1 has its own saved EO selection; block 2 has none of its own — block 2 must not
        // inherit block 1's selection, nor (with IsFromNextBlock set) fall back to batch defaults.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 1, BatchID = 5, AnimalID = 2, BlockRef = "01" },
            new Block { ID = 2, BatchID = 5, AnimalID = 2, BlockRef = "02" },
        ]);
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[
            new BlockTest { ID = 1, BlockID = 1, TestType = BlockTestType.Histology, Code = HistologyCode.EO },
        ]);
        var sut = CreateSut(animalId: 2, blockId: 2);
        sut.IsFromNextBlock = true;

        await sut.OnGetAsync();

        Assert.Empty(sut.ExistingHistologyCodes);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_ValidationFailsForAnUnrelatedReason_StillShowsThePostedTestSelectionNotTheBatchDefaults()
    {
        // Regression: a validation error re-render used to call LoadEditModeDataAsync() (which
        // re-applies the batch-level defaults, here EO) AFTER echoing back the posted selection,
        // silently clobbering it — so a user who unchecked EO and checked H&E would see EO still
        // ticked on any error, however unrelated (e.g. "add a tissue"), and the error would keep
        // recurring no matter how many times they retried.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        // No tissues on the current block -> SaveCurrentBlockDetailsAsync fails with the
        // "add at least one tissue" error, unrelated to the test selection itself.
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        // Batch-level default is EO — if the bug were present, this is what would reappear.
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 5, Code = HistologyCode.EO }],
        });
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.NewBlockRef = "01";
        sut.NewCustomerRef = "edited customer ref";
        // User unchecked EO and ticked H&E instead.
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];

        var result = await sut.OnPostNextBlockAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Add at least one tissue to this block before completing it.", sut.ErrorMessage);
        Assert.Equal([HistologyCode.HAndE], sut.ExistingHistologyCodes);
        Assert.DoesNotContain(HistologyCode.EO, sut.ExistingHistologyCodes);
        // Block Ref/Customer Ref edits must also survive the error re-render, not revert to the saved row.
        Assert.Equal("01", sut.NewBlockRef);
        Assert.Equal("edited customer ref", sut.NewCustomerRef);
    }

    [Fact]
    public async Task ShowAntibodiesAndShowStains_NextBlockCreatedBlockWithNothingTicked_StillTrueWhenBatchHasOptions()
    {
        // Regression: a Next-Block-created block starts with no Histology codes ticked at all
        // (by design — see LoadEditModeData_NextBlockCreatedBlockNoSavedTests_StaysUnselected), so
        // gating the Antibodies/Special stain SECTIONS on "is IHC-PrP/Special Stain currently
        // ticked on this block" hid them entirely until the user re-ticked the parent Histology
        // box first — contradicting "all Antibody and Special Stain items should be displayed as
        // checkboxes in an unselected state". The sections must stay visible whenever the
        // submission has such options at all, regardless of this block's own tick state.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319182, BatchID = 5, AnimalID = 2, BlockRef = "02" },
        ]);
        _lookups.Setup(l => l.GetLookupDataAsync(4, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "AB01", Name = "Antibody 1" },
        ]);
        _lookups.Setup(l => l.GetLookupDataAsync(6, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "ST01", Name = "Stain 1" },
        ]);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 5, Code = HistologyCode.IhcPrp }],
            Antibodies = [new BatchTestSelectionRow { ID = 2, BatchID = 5, Code = "AB01" }],
            Stains = [new BatchTestSelectionRow { ID = 3, BatchID = 5, Code = "ST01" }],
        });
        var sut = CreateSut(animalId: 2, blockId: 319182);
        sut.IsFromNextBlock = true;

        await sut.OnGetAsync();

        Assert.Empty(sut.ExistingHistologyCodes);
        Assert.True(sut.ShowAntibodies);
        Assert.True(sut.ShowStains);
    }

    [Fact]
    public async Task AntibodiesEnabledAndStainsEnabled_AntibodyCodeSavedWithoutItsHistologyTrigger_StillTrue()
    {
        // Legacy parity: DisplayBatchLevelTests/DisplayBlockLevelTests enable chkblAntibodies
        // purely from an Antibody row's presence, independent of whether IHC-PrP/IHC-Other is also
        // saved for this block. Gating solely on the Histology trigger would render this orphaned
        // Antibody code disabled, and the next Done/Next-block save would silently delete it
        // (disabled checkboxes never post their value) — a real data-loss regression vs legacy.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
        ]);
        _lookups.Setup(l => l.GetLookupDataAsync(4, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "AB01", Name = "Antibody 1" },
        ]);
        _lookups.Setup(l => l.GetLookupDataAsync(6, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "ST01", Name = "Stain 1" },
        ]);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections());
        // This block has AB01/ST01 saved but no IHC-PrP/IHC-Other/Special Stain Histology code —
        // an inconsistent state that must not have arisen under the new rules, but may already
        // exist from data saved before this feature.
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[
            new BlockTest { ID = 1, BlockID = 319181, TestType = BlockTestType.Histology, Code = HistologyCode.HAndE },
            new BlockTest { ID = 2, BlockID = 319181, TestType = BlockTestType.Antibodies, Code = "AB01" },
            new BlockTest { ID = 3, BlockID = 319181, TestType = BlockTestType.Stain, Code = "ST01" },
        ]);
        var sut = CreateSut(animalId: 2, blockId: 319181);

        await sut.OnGetAsync();

        Assert.DoesNotContain(HistologyCode.IhcPrp, sut.ExistingHistologyCodes);
        Assert.DoesNotContain(HistologyCode.SpecialStain, sut.ExistingHistologyCodes);
        Assert.True(sut.AntibodiesEnabled);
        Assert.True(sut.StainsEnabled);
    }

    [Fact]
    public async Task AntibodiesEnabledAndStainsEnabled_NextBlockCreatedBlockWithNothingTicked_BothFalse()
    {
        // A Next-Block-created block starts with no Histology codes ticked — the Antibodies/
        // Special stain sections stay visible (ShowAntibodies/ShowStains) but their checkboxes
        // must render disabled until the user ticks IHC-PrP/IHC-Other/Special Stain on this block.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319182, BatchID = 5, AnimalID = 2, BlockRef = "02" },
        ]);
        _lookups.Setup(l => l.GetLookupDataAsync(4, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "AB01", Name = "Antibody 1" },
        ]);
        _lookups.Setup(l => l.GetLookupDataAsync(6, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "ST01", Name = "Stain 1" },
        ]);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 5, Code = HistologyCode.IhcPrp }],
            Antibodies = [new BatchTestSelectionRow { ID = 2, BatchID = 5, Code = "AB01" }],
            Stains = [new BatchTestSelectionRow { ID = 3, BatchID = 5, Code = "ST01" }],
        });
        var sut = CreateSut(animalId: 2, blockId: 319182);
        sut.IsFromNextBlock = true;

        await sut.OnGetAsync();

        Assert.False(sut.AntibodiesEnabled);
        Assert.False(sut.StainsEnabled);
    }

    [Fact]
    public async Task AntibodiesEnabledAndStainsEnabled_CarriedForwardTestsIncludeIhcAndSpecialStain_BothTrue()
    {
        // Carry-forward (CarryTestsToNextBlock) saves the prior block's codes onto the new block
        // before redirecting — IHC-PrP/Special Stain being among them must enable, not just show,
        // the dependent checkboxes on first load of that new block.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319182, BatchID = 5, AnimalID = 2, BlockRef = "02" },
        ]);
        _lookups.Setup(l => l.GetLookupDataAsync(4, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "AB01", Name = "Antibody 1" },
        ]);
        _lookups.Setup(l => l.GetLookupDataAsync(6, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { Code = "ST01", Name = "Stain 1" },
        ]);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Histo.Submissions.Models.BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 5, Code = HistologyCode.IhcPrp }],
            Antibodies = [new BatchTestSelectionRow { ID = 2, BatchID = 5, Code = "AB01" }],
            Stains = [new BatchTestSelectionRow { ID = 3, BatchID = 5, Code = "ST01" }],
        });
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[
            new BlockTest { ID = 1, BlockID = 319182, TestType = BlockTestType.Histology, Code = HistologyCode.IhcPrp },
            new BlockTest { ID = 2, BlockID = 319182, TestType = BlockTestType.Histology, Code = HistologyCode.SpecialStain },
            new BlockTest { ID = 3, BlockID = 319182, TestType = BlockTestType.Antibodies, Code = "AB01" },
            new BlockTest { ID = 4, BlockID = 319182, TestType = BlockTestType.Stain, Code = "ST01" },
        ]);
        var sut = CreateSut(animalId: 2, blockId: 319182);
        sut.IsFromNextBlock = true;

        await sut.OnGetAsync();

        Assert.True(sut.AntibodiesEnabled);
        Assert.True(sut.StainsEnabled);
    }

    [Fact]
    public async Task OnPostNextBlockAsync_AbandonedZeroTissueBlocksExist_SkipsThemAndResumesTheExactNextRef()
    {
        // Regression: editing block 01 and clicking "Next block" must always land on the true
        // next sequential ref (02), even when blocks 02/03 already exist as abandoned, never-
        // finished rows left behind by earlier "Next block" clicks the user didn't complete or
        // explicitly cancel. Before this fix, those zero-tissue rows permanently reserved their
        // ref numbers (only OnGetAsync's Add-block flow excluded/resumed them), so every repeat
        // of this sequence pushed the numbering one block further — 02, then 03, then 04 — instead
        // of resuming 02 as intended.
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 2, SenderRef = "S2" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[
            new Block { ID = 319181, BatchID = 5, AnimalID = 2, BlockRef = "01" },
            new Block { ID = 319182, BatchID = 5, AnimalID = 2, BlockRef = "02" }, // abandoned, 0 tissues
            new Block { ID = 319183, BatchID = 5, AnimalID = 2, BlockRef = "03" }, // abandoned, 0 tissues
        ]);
        // Only block 01 (the one being edited) has tissues — 02/03 are the abandoned rows.
        _submissions.Setup(s => s.GetTissuesByBatchAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(5, 319181, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[
            new Tissue { ID = 1, OwnerID = 319181, Owner = TissueOwner.Block, TissueCode = "AGAR", NoPieces = 1 },
        ]);
        var sut = CreateSut(animalId: 2, blockId: 319181);
        sut.NewBlockRef = "01";
        sut.SelectedHistologyCodes = [HistologyCode.EO];

        var result = await sut.OnPostNextBlockAsync();

        // Resumes the existing abandoned block 319182 (ref "02") instead of creating a new row.
        _blocks.Verify(b => b.AddBlockAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<int>>(),
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal(319182, redirect.RouteValues?["blockId"]);
    }
}

