using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
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

/// <summary>
/// Regression coverage for the Create-submission Histology checkbox exclusivity rule.
///
/// Legacy source: BatchDetails.aspx.vb::chkblHistology_SelectedIndexChanged — only "Archive" is
/// mutually exclusive with every other Histology test; EO is a normal, freely-combinable option
/// there (EO is only exclusive on the separate per-block BlockDetails.aspx page, a different rule
/// for a different page). The migrated Create-mode page incorrectly also made EO exclusive,
/// diverging from both legacy and the already-correct EditBatch.cshtml/.cs behaviour.
/// </summary>
public class BatchDetailsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBlockTestService> _blockTests = new();

    public BatchDetailsModelTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.UserAreaID).Returns(5);
        _batches.Setup(b => b.SaveBatchTestSelectionsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // LoadCreateLookupsAsync runs unconditionally at the top of OnPostCreateAsync.
        _lookups.Setup(l => l.GetProjectsByAreaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetContactsByAreaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Histo.Administration.Models.User>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[]);
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[]);
    }

    private BatchDetailsModel CreateSut() =>
        new(_session.Object, _batches.Object, _lookups.Object, _users.Object, _submissions.Object, _blocks.Object, _blockTests.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            Create_ProjectContractCode = "PC1",
            Create_ContactName = "Dr Smith",
            Create_SpeciesId = "1",
            Create_BatchDateStr = "2026-10-01",
            Create_SafeToHandle = true,
            Create_OtherSubmittedBy = 7,
            Create_OtherSubmittedArea = "5",
        };

    [Fact]
    public async Task OnPostCreateAsync_EOWithOtherHistologyCodes_IsAllowed()
    {
        _batches.Setup(b => b.AddAsync(It.IsAny<Batch>(), 7, It.IsAny<CancellationToken>())).ReturnsAsync(100);
        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO, HistologyCode.IhcPrp];
        sut.Create_SelectedAntibodyCodes = ["ABC"];

        var result = await sut.OnPostCreateAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.False(sut.Errors?.ContainsKey("Create_Histology"));
    }

    [Fact]
    public async Task OnPostCreateAsync_Success_ClearsStaleSampleSummaryReturnPage()
    {
        // Regression: Session.SampleSummaryReturnPage is only ever set (by BatchDetails/EditBatch's
        // own "Samples" button), never cleared — a leftover value from an earlier, unrelated
        // submission made SampleSummary.OnPostFinishAsync redirect "Finish" back to that old page
        // instead of Print Submission for a brand-new submission in the same browser session.
        _session.SetupProperty(s => s.SampleSummaryReturnPage, "/Batches/EditBatch");
        _batches.Setup(b => b.AddAsync(It.IsAny<Batch>(), 7, It.IsAny<CancellationToken>())).ReturnsAsync(100);
        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];

        await sut.OnPostCreateAsync();

        Assert.Null(_session.Object.SampleSummaryReturnPage);
    }

    [Fact]
    public async Task OnPostCreateAsync_ArchiveWithOtherHistologyCodes_StillRejected()
    {
        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.Archive, HistologyCode.IhcPrp];

        var result = await sut.OnPostCreateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Archive cannot be combined with other histology types.", sut.Errors?["Create_Histology"]);
        _batches.Verify(b => b.AddAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Batch MakeSourceBatchForCopy() => new()
    {
        ID = 10,
        ProjectContractCode = "PROJ1",
        ContactName = "A Contact",
        Species = "Mouse",
        Fixation = "Formalin",
        SafeToHandle = true,
        OtherSubmittedBy = 3,
        OtherSubmittedArea = "5",
        Comments = "source comments",
        IsPreCassetted = true,
    };

    /// <summary>
    /// Verifies the "Copy submission" prefill path: a <see cref="CopyBatchModel.PendingCopy"/>
    /// staged in TempData by CopyBatchModel.OnPostAsync populates the Create Submission form
    /// from the source batch instead of the normal blank-form defaults.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_PendingCopyStaged_PrefillsFormFromSourceBatch()
    {
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(MakeSourceBatchForCopy());
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 10, Code = "2" }],
        });
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync("WT");
        _lookups.Setup(l => l.GetLookupDataAsync(11, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 99, Code = "WT", Name = "Wet Tissue" }]);

        var sut = CreateSut();
        sut.Mode = "create";
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1" }], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        await sut.OnGetAsync();

        Assert.Equal("PROJ1", sut.Create_ProjectContractCode);
        Assert.Equal("A Contact", sut.Create_ContactName);
        Assert.Equal("Mouse", sut.Create_SpeciesId);
        Assert.Equal(["2"], sut.Create_SelectedHistologyCodes);
        Assert.Equal("99", sut.TempData["CreateSubmittedAsId"]);
        Assert.Equal("WT", sut.TempData["CreateSubmittedAsCode"]);
        Assert.Equal("Wet Tissue", sut.Create_SubmittedAsName);
        Assert.Equal("True", sut.TempData["CreateIsPreCassetted"]);
        Assert.Equal(["S1"], sut.Create_CopiedSamples.Select(s => s.SenderRef));
    }

    /// <summary>
    /// Regression: an abandoned pending copy (Cancel/navigate away before submitting the
    /// pre-filled form) must not resurface on a later, unrelated Create Submission visit that
    /// doesn't carry the matching token — it must be discarded, not applied.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_PendingCopyStagedButNoMatchingToken_IsDiscardedAndFormUsesNormalDefaults()
    {
        var sut = CreateSut();
        sut.Mode = "create";
        sut.CopyToken = null; // ordinary visit — no token in the query string
        var pending = new CopyBatchModel.PendingCopy(10, [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1" }], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        await sut.OnGetAsync();

        Assert.False(sut.TempData.ContainsKey("CopyBatch_PendingCopy"));
        _batches.Verify(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies the "Copy submission" consuming path: once the pre-filled Create Submission form
    /// is actually submitted, the whole copy (batch header + staged samples/tissues) is delegated
    /// to <see cref="ISubmissionService.CreateBatchWithCopiedSamplesAsync"/> as a single operation
    /// — <see cref="IBatchService.AddAsync"/> is never called directly for this journey — and the
    /// user lands on CopyBatchSummary instead of the normal SampleSummary redirect.
    /// </summary>
    [Fact]
    public async Task OnPostCreateAsync_PendingCopyStaged_DelegatesWholeCopyToOneTransactionalCallAndRedirectsToCopyBatchSummary()
    {
        var stagedAnimal = new Animal { ID = 1, SenderRef = "S1", BatchSubmissionID = 50 };
        var otherAnimal = new Animal { ID = 2, SenderRef = "S2", BatchSubmissionID = 51 };
        var sourceSubmission = new BatchSubmission { ID = 50, BatchID = 10, AnimalID = 1, Order = 1 };
        var otherSubmission = new BatchSubmission { ID = 51, BatchID = 10, AnimalID = 2, Order = 2 };
        var tissue = new Tissue
        {
            ID = 1,
            OwnerID = 50,
            Owner = TissueOwner.Submission,
            TissueCode = "T1",
            NoPieces = 2,
            Comment = "original comment",
            ArchiveLocation = "Freezer 1",
            ArchivedDate = new DateTime(2024, 01, 15),
            ArchiveComment = "archive note",
        };

        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[sourceSubmission, otherSubmission]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[stagedAnimal, otherAnimal]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(10, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[tissue]);
        _submissions.Setup(s => s.CreateBatchWithCopiedSamplesAsync(
                It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string?>(), It.Is<IReadOnlyList<CopiedSamplePlan>>(p => p.Count == 1 && p[0].SourceAnimal.ID == 1 && p[0].Tissues.Count == 1 && p[0].Tissues[0].ArchiveLocation == "Freezer 1" && p[0].Tissues[0].ArchiveComment == "archive note"), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1", NewSenderRef = "S1-NEW" }], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        var result = await sut.OnPostCreateAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/CopyBatchSummary", redirect.PageName);
        Assert.Equal(100, redirect.RouteValues!["newBatchId"]);
        _batches.Verify(b => b.AddAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(sut.TempData.ContainsKey("CopyBatch_PendingCopy"));
    }

    [Fact]
    public async Task OnPostCreateAsync_PendingCopyStaged_OnlyIncludesRowsWithSelectedNewSenderRefs()
    {
        var stagedAnimal = new Animal { ID = 1, SenderRef = "S1", BatchSubmissionID = 50 };
        var otherAnimal = new Animal { ID = 2, SenderRef = "S2", BatchSubmissionID = 51 };
        var sourceSubmission = new BatchSubmission { ID = 50, BatchID = 10, AnimalID = 1, Order = 1 };
        var otherSubmission = new BatchSubmission { ID = 51, BatchID = 10, AnimalID = 2, Order = 2 };

        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[sourceSubmission, otherSubmission]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[stagedAnimal, otherAnimal]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(10, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(10, 51, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);

        IReadOnlyList<CopiedSamplePlan>? captured = null;
        _submissions.Setup(s => s.CreateBatchWithCopiedSamplesAsync(
                It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string?>(), It.IsAny<IReadOnlyList<CopiedSamplePlan>>(), 7, It.IsAny<CancellationToken>()))
            .Callback((Batch _, IReadOnlyList<string> _, IReadOnlyList<string> _, IReadOnlyList<string> _,
                       string? _, IReadOnlyList<CopiedSamplePlan> p, int _, CancellationToken _) => captured = p)
            .ReturnsAsync(100);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [
            new CopyBatchModel.AnimalRow { AnimalId = 1, SubmissionId = 50, SenderRef = "S1", NewSenderRef = "S1-NEW" },
            new CopyBatchModel.AnimalRow { AnimalId = 2, SubmissionId = 51, SenderRef = "S2", NewSenderRef = string.Empty },
        ], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        await sut.OnPostCreateAsync();

        Assert.NotNull(captured);
        Assert.Single(captured!);
        Assert.Equal(1, captured![0].SourceAnimal.ID);
    }

    [Fact]
    public async Task OnPostCreateAsync_PendingCopyStaged_CopiesTheSamplesBlocksAndBlockTissues()
    {
        var stagedAnimal = new Animal { ID = 1, SenderRef = "S1", BatchSubmissionID = 50 };
        var sourceSubmission = new BatchSubmission { ID = 50, BatchID = 10, AnimalID = 1, Order = 1 };
        var blockTissue = new Tissue { ID = 9, OwnerID = 70, TissueCode = "BT1", Owner = TissueOwner.Block };

        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[sourceSubmission]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[stagedAnimal]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(10, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[
                new Block { ID = 70, BatchID = 10, AnimalID = 1, BlockRef = "01", Order = 1, CustomerRef = "C1" }]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(10, 70, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[blockTissue]);

        CopiedSamplePlan? captured = null;
        _submissions.Setup(s => s.CreateBatchWithCopiedSamplesAsync(
                It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string?>(), It.IsAny<IReadOnlyList<CopiedSamplePlan>>(), 7, It.IsAny<CancellationToken>()))
            .Callback((Batch _, IReadOnlyList<string> _, IReadOnlyList<string> _, IReadOnlyList<string> _,
                       string? _, IReadOnlyList<CopiedSamplePlan> p, int _, CancellationToken _) => captured = p.Single())
            .ReturnsAsync(100);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1", NewSenderRef = "S1-NEW" }], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        await sut.OnPostCreateAsync();

        var block = Assert.Single(captured!.Blocks);
        Assert.Equal("01", block.BlockRef);
        Assert.Equal("C1", block.CustomerRef);
        Assert.Equal("BT1", Assert.Single(block.Tissues).TissueCode);
    }

    [Fact]
    public async Task OnPostCreateAsync_PendingCopyStaged_CarriesBlockTestSelectionsAndTheHistologyRefType()
    {
        var stagedAnimal = new Animal { ID = 1, SenderRef = "S1", BatchSubmissionID = 50, HistologyRef = "26/60001", HistoRefSet = true };
        var sourceSubmission = new BatchSubmission { ID = 50, BatchID = 10, AnimalID = 1, Order = 1 };

        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[sourceSubmission]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[stagedAnimal]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(10, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.GetTissuesByBlockAsync(10, 70, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Block>)[new Block { ID = 70, BatchID = 10, AnimalID = 1, BlockRef = "01" }]);
        _blockTests.Setup(t => t.GetAllSelectionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BlockTest>)[
                new BlockTest { BlockID = 70, TestType = BlockTestType.Histology, Code = HistologyCode.HAndE },
                new BlockTest { BlockID = 70, TestType = BlockTestType.Stain, Code = "ST1" }]);

        CopiedSamplePlan? captured = null;
        _submissions.Setup(s => s.CreateBatchWithCopiedSamplesAsync(
                It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string?>(), It.IsAny<IReadOnlyList<CopiedSamplePlan>>(), 7, It.IsAny<CancellationToken>()))
            .Callback((Batch _, IReadOnlyList<string> _, IReadOnlyList<string> _, IReadOnlyList<string> _,
                       string? _, IReadOnlyList<CopiedSamplePlan> p, int _, CancellationToken _) => captured = p.Single())
            .ReturnsAsync(100);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1", NewSenderRef = "S1-NEW" }], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        await sut.OnPostCreateAsync();

        // The type travels, not a pre-claimed ref — the draw happens inside the copy transaction.
        Assert.Equal(HistologyRefTypeCode.MouseProjects, captured!.HistologyRefType);
        var block = Assert.Single(captured.Blocks);
        Assert.Equal([HistologyCode.HAndE], block.HistologyCodes);
        Assert.Equal(["ST1"], block.StainCodes);
        Assert.Empty(block.AntibodyCodes);
    }

    /// <summary>
    /// Regression: an abandoned pending copy must not be applied to a later, unrelated Create
    /// Submission POST that lacks the matching token — it must fall through to the ordinary
    /// create-a-new-batch path (<see cref="IBatchService.AddAsync"/>), not the copy path.
    /// </summary>
    [Fact]
    public async Task OnPostCreateAsync_PendingCopyStagedButNoMatchingToken_FallsThroughToNormalCreate()
    {
        _batches.Setup(b => b.AddAsync(It.IsAny<Batch>(), 7, It.IsAny<CancellationToken>())).ReturnsAsync(100);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = null;
        var pending = new CopyBatchModel.PendingCopy(10, [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1", NewSenderRef = "S1-NEW" }], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        var result = await sut.OnPostCreateAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _batches.Verify(b => b.AddAsync(It.IsAny<Batch>(), 7, It.IsAny<CancellationToken>()), Times.Once);
        _submissions.Verify(s => s.CreateBatchWithCopiedSamplesAsync(
            It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<string?>(), It.IsAny<IReadOnlyList<CopiedSamplePlan>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostCreateAsync_PendingCopy_UsesTheStagedSubmissionIdForWetTissueRows()
    {
        // A wet-tissue animal owns more than one BatchSubmission, so the staged id picks which one.
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[
                new() { ID = 50, BatchID = 10, AnimalID = 1, Order = 1 },
                new() { ID = 60, BatchID = 10, AnimalID = 1, Order = 2 },
            ]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "S1", BatchSubmissionID = 0 }]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(10, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 10, TissueCode = "LIVER" }]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(10, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 20, TissueCode = "LUNG" }]);

        CopiedSamplePlan? captured = null;
        _submissions.Setup(s => s.CreateBatchWithCopiedSamplesAsync(
                It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string?>(), It.IsAny<IReadOnlyList<CopiedSamplePlan>>(), 7, It.IsAny<CancellationToken>()))
            .Callback((Batch _, IReadOnlyList<string> _, IReadOnlyList<string> _, IReadOnlyList<string> _,
                       string? _, IReadOnlyList<CopiedSamplePlan> p, int _, CancellationToken _) => captured = p.Single())
            .ReturnsAsync(100);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [
            new CopyBatchModel.AnimalRow { AnimalId = 1, SubmissionId = 60, SenderRef = "S1", NewSenderRef = "S1-NEW" }
        ], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        var result = await sut.OnPostCreateAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/CopyBatchSummary", redirect.PageName);
        Assert.NotNull(captured);
        Assert.Equal(60, captured!.SourceSubmission.ID);
        Assert.Equal("LUNG", captured.Tissues.Single().TissueCode);
        _submissions.Verify(s => s.GetTissuesBySubmissionAsync(10, 60, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// SubmissionId is a client-controlled hidden field, so a pair that points at another animal's
    /// submission must not be honoured — that would copy animal 2's tissues onto animal 1's blocks.
    /// </summary>
    [Fact]
    public async Task OnPostCreateAsync_StagedSubmissionBelongsToAnotherAnimal_FallsBackToTheAnimalsOwnSubmission()
    {
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[
                new() { ID = 50, BatchID = 10, AnimalID = 1, Order = 1 },
                new() { ID = 60, BatchID = 10, AnimalID = 2, Order = 2 },
            ]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "S1", BatchSubmissionID = 0 }]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(10, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 10, TissueCode = "LIVER" }]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(10, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 20, TissueCode = "LUNG" }]);

        CopiedSamplePlan? captured = null;
        _submissions.Setup(s => s.CreateBatchWithCopiedSamplesAsync(
                It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string?>(), It.IsAny<IReadOnlyList<CopiedSamplePlan>>(), 7, It.IsAny<CancellationToken>()))
            .Callback((Batch _, IReadOnlyList<string> _, IReadOnlyList<string> _, IReadOnlyList<string> _,
                       string? _, IReadOnlyList<CopiedSamplePlan> p, int _, CancellationToken _) => captured = p.Single())
            .ReturnsAsync(100);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [
            new CopyBatchModel.AnimalRow { AnimalId = 1, SubmissionId = 60, SenderRef = "S1", NewSenderRef = "S1-NEW" }
        ], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        await sut.OnPostCreateAsync();

        Assert.NotNull(captured);
        Assert.Equal(50, captured!.SourceSubmission.ID);
        Assert.Equal("LIVER", captured.Tissues.Single().TissueCode);
        _submissions.Verify(s => s.GetTissuesBySubmissionAsync(10, 60, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostCreateAsync_StagedSampleNoLongerInSourceBatch_FailsInsteadOfCopyingPartially()
    {
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 50, BatchID = 10, AnimalID = 1, Order = 1 }]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "S1", BatchSubmissionID = 50 }]);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [
            new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1", NewSenderRef = "S1-NEW" },
            new CopyBatchModel.AnimalRow { AnimalId = 999, SenderRef = "GONE", NewSenderRef = "GONE-NEW" }], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        var result = await sut.OnPostCreateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("GONE", sut.Errors?["Create_Save"]);
        _submissions.Verify(s => s.CreateBatchWithCopiedSamplesAsync(
            It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<string?>(), It.IsAny<IReadOnlyList<CopiedSamplePlan>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostCreateAsync_StagedSampleSubmissionCannotBeMatched_FailsInsteadOfCopyingPartially()
    {
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 50, BatchID = 10, AnimalID = 1, Order = 1 }]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        // BatchSubmissionID 77 matches no submission, and isn't 0 so the firstSubmId fallback can't apply.
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "S1", BatchSubmissionID = 77 }]);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1", NewSenderRef = "S1-NEW" }], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        var result = await sut.OnPostCreateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("submission record could not be found", sut.Errors?["Create_Save"]);
        _submissions.Verify(s => s.CreateBatchWithCopiedSamplesAsync(
            It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<string?>(), It.IsAny<IReadOnlyList<CopiedSamplePlan>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostCreateAsync_PendingCopyWithNoSamples_StillUsesTheCopyPath()
    {
        // An empty source submission still stages a valid pending copy; falling through to the
        // ordinary create would redirect to the wrong summary and strand CopyBatch_PendingCopy.
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.CreateBatchWithCopiedSamplesAsync(
                It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string?>(), It.Is<IReadOnlyList<CopiedSamplePlan>>(p => p.Count == 0), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        var result = await sut.OnPostCreateAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/CopyBatchSummary", redirect.PageName);
        Assert.False(sut.TempData.ContainsKey("CopyBatch_PendingCopy"));
        _batches.Verify(b => b.AddAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Regression: a failure anywhere in the transactional copy (e.g. a mid-copy sample/tissue
    /// error) must surface as a page error, not be silently swallowed after a batch has already
    /// been committed — <see cref="ISubmissionService.CreateBatchWithCopiedSamplesAsync"/> returns
    /// 0 when its internal transaction rolled back, and nothing should be left behind.
    /// </summary>
    [Fact]
    public async Task OnPostCreateAsync_PendingCopyStaged_TransactionFails_ShowsErrorWithoutRedirecting()
    {
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 50, BatchID = 10, AnimalID = 1, Order = 1 }]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1, SenderRef = "S1", BatchSubmissionID = 50 }]);
        _submissions.Setup(s => s.GetTissuesBySubmissionAsync(10, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[]);
        _submissions.Setup(s => s.CreateBatchWithCopiedSamplesAsync(
                It.IsAny<Batch>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string?>(), It.IsAny<IReadOnlyList<CopiedSamplePlan>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        sut.CopyToken = "tok-1";
        var pending = new CopyBatchModel.PendingCopy(10, [new CopyBatchModel.AnimalRow { AnimalId = 1, SenderRef = "S1", NewSenderRef = "S1-NEW" }], "tok-1");
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(pending);

        var result = await sut.OnPostCreateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to create the submission. Please try again.", sut.Errors?["Create_Save"]);
        _batches.Verify(b => b.AddAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void CopyBatchSummaryModel_OnPostSamplesAndEdit_SetSessionBatchAndRedirectToSampleOrEditPage()
    {
        var session = new Mock<ISessionService>();
        session.SetupProperty(s => s.BatchID);
        session.SetupProperty(s => s.IsViewSubmissionMode);
        session.SetupProperty(s => s.SampleSummaryReturnPage);
        session.SetupProperty(s => s.EditBatchReturnPage);
        session.SetupProperty(s => s.ReturnPageQuery);
        var batches = new Mock<IBatchService>();
        var submissions = new Mock<ISubmissionService>();
        var sut = new CopyBatchSummaryModel(session.Object, batches.Object, submissions.Object);

        var sampleResult = sut.OnPostSamplesAsync(42);
        var editResult = sut.OnPostEditAsync(42);

        var sampleRedirect = Assert.IsType<RedirectToPageResult>(sampleResult);
        Assert.Equal("/Submissions/SampleSummary", sampleRedirect.PageName);
        Assert.Equal(42, session.Object.BatchID);
        Assert.Equal(42, sampleRedirect.RouteValues!["batchId"]);

        var editRedirect = Assert.IsType<RedirectToPageResult>(editResult);
        Assert.Equal("/Batches/EditBatch", editRedirect.PageName);
        Assert.Equal("?newBatchId=42", session.Object.ReturnPageQuery);
    }

    [Fact]
    public async Task CopyBatchSummaryModel_OnGetAsync_NoRouteValue_FallsBackToSessionBatchId()
    {
        var session = new Mock<ISessionService>();
        session.SetupProperty(s => s.BatchID);
        session.Object.BatchID = 42;
        var batches = new Mock<IBatchService>();
        batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42 });
        var submissions = new Mock<ISubmissionService>();
        submissions.Setup(s => s.GetSubmissionsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 1 }]);
        submissions.Setup(s => s.GetAnimalsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 1 }]);

        var sut = new CopyBatchSummaryModel(session.Object, batches.Object, submissions.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

        var result = await sut.OnGetAsync(0);

        Assert.IsType<PageResult>(result);
        Assert.Equal(42, sut.NewBatchId);
        Assert.Equal(1, sut.SubmissionCount);
    }
}
