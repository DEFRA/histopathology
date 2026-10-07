using Histo.Administration.Interfaces;
using Histo.Administration.Models;
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
    }

    private BatchDetailsModel CreateSut() =>
        new(_session.Object, _batches.Object, _lookups.Object, _users.Object, _submissions.Object)
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
        var tissue = new Tissue { ID = 1, OwnerID = 50, TissueCode = "T1" };

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
                It.IsAny<string?>(), It.Is<IReadOnlyList<CopiedSamplePlan>>(p => p.Count == 1 && p[0].SourceAnimal.ID == 1), 7, It.IsAny<CancellationToken>()))
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
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
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
}
