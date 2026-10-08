using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
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
/// Unit tests for <see cref="EditBatchModel"/> — "Edit submission", covering header field
/// pre-population, the pick-list-detour draft round-trip, batch-level test-type validation,
/// and the save/redirect flow.
/// </summary>
public class EditBatchModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public EditBatchModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.IsViewSubmissionMode);
        _session.SetupProperty(s => s.SampleSummaryReturnPage);
        _session.SetupProperty(s => s.BatchType);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
        _session.SetupProperty(s => s.EditBatchReturnPage);
        _session.SetupProperty(s => s.ReturnPageQuery);
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.UserArea).Returns("Histopath");

        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchTestSelections());
    }

    private EditBatchModel CreateSut()
    {
        // OnPostManagePickList builds a return URL via Url.Page(...) before redirecting, which
        // reads IUrlHelper.ActionContext internally — a bare mock's null ActionContext throws.
        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.ActionContext).Returns(actionContext);
        url.Setup(u => u.RouteUrl(It.IsAny<Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext>())).Returns("/Batches/EditBatch");

        return new(_session.Object, _batches.Object, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            Url = url.Object,
        };
    }

    private static Batch SampleBatch(string status = BatchStatus.Submitted) => new()
    {
        ID = 10,
        Status = status,
        BatchDate = new DateTime(2026, 1, 15),
        BatchType = BatchTypeConstants.Tse,
        RowStamp = [1, 2, 3],
        SafeToHandle = true,
    };

    // ── OnGetAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_NoBatchIdInSession_RedirectsToIndex()
    {
        var sut = CreateSut();
        _session.Object.BatchID = 0;

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        _batches.Verify(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_GetByIdThrows_SetsSaveErrorAndReturnsPage()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db down"));
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("db down", sut.SaveError);
    }

    [Fact]
    public async Task OnGetAsync_BatchNotFound_RedirectsToIndex()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_BatchFound_PrePopulatesFieldsAndClearsViewSubmissionMode()
    {
        _session.Object.BatchID = 10;
        _session.Object.IsViewSubmissionMode = true;
        var batch = SampleBatch();
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("15/01/2026", sut.BatchDateStr);
        Assert.False(_session.Object.IsViewSubmissionMode);
        Assert.Equal("/Batches/EditBatch", _session.Object.SampleSummaryReturnPage);
    }

    [Fact]
    public async Task OnGetAsync_NoDraftPending_LoadsTestSelectionsFromBatch()
    {
        _session.Object.BatchID = 10;
        var batch = SampleBatch();
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchTestSelections
            {
                Histology = [new BatchTestSelectionRow { Code = HistologyCode.HAndE }],
            });
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal([HistologyCode.HAndE], sut.SelectedHistologyCodes);
    }

    // ── OnPostManagePickList ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("submittedBy", "/Admin/UserMaintenance")]
    [InlineData("project", "/Admin/LookupItems")]
    [InlineData("pathologist", "/Admin/LookupItems")]
    [InlineData("somethingElse", "/Batches/EditBatch")]
    public void OnPostManagePickList_RoutesToExpectedPage(string field, string expectedPage)
    {
        var sut = CreateSut();
        sut.ProjectContractCode = "PC1";

        var result = sut.OnPostManagePickList(field);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(expectedPage, redirect.PageName);
        // A draft is staged regardless of which field triggered the detour.
        Assert.NotNull(sut.TempData["EditBatch_Draft"]);
    }

    [Fact]
    public async Task OnGetAsync_PendingDraftExists_RestoresFieldsInsteadOfBatchValues()
    {
        _session.Object.BatchID = 10;
        var batch = SampleBatch();
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        var sut = CreateSut();
        sut.ProjectContractCode = "DRAFT-PC";
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.OnPostManagePickList("project"); // stages the draft into TempData

        await sut.OnGetAsync();

        Assert.Equal("DRAFT-PC", sut.ProjectContractCode);
        // The batch-level selection lookup must not override a restored draft.
        _batches.Verify(b => b.GetBatchTestSelectionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_CorruptDraftJson_IsIgnoredAndFallsBackToBatchValues()
    {
        _session.Object.BatchID = 10;
        var batch = SampleBatch();
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        var sut = CreateSut();
        sut.TempData["EditBatch_Draft"] = "{not-valid-json";

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(batch.ProjectContractCode, sut.ProjectContractCode);
    }

    // ── OnPostAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task OnPostAsync_BatchHasNoRowStamp_RedirectsToIndex()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 10, RowStamp = null });
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_GetByIdThrows_SetsGenericSaveError()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db down"));
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to load the submission. Please go back and try again.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_NoHistologySelected_SetsSaveErrorAndReturnsPage()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(SampleBatch());
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [];

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select at least one histology type.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_ArchiveCombinedWithOtherHistologyTypes_SetsSaveError()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(SampleBatch());
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.Archive, HistologyCode.HAndE];

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Archive cannot be combined with other histology types.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_SpecialStainSelectedWithNoStainCodes_SetsSaveError()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(SampleBatch());
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.SpecialStain];
        sut.SelectedStainCodes = [];

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Special Stain is selected — you must also select at least one special stain.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_IhcSelectedWithNoAntibodyCodes_SetsSaveError()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(SampleBatch());
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.IhcPrp];
        sut.SelectedAntibodyCodes = [];

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("IHC is selected — you must also select at least one antibody.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_SafeToHandleNotAnswered_SetsSaveError()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(SampleBatch());
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select whether the submission is adequately fixed.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_InvalidBatchDateFormat_SetsSaveError()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(SampleBatch());
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = true;
        sut.BatchDateStr = "not-a-date";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a valid date of submission in DD/MM/YYYY format.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_RejectedBatch_RevertsStatusToSubmittedOnSave()
    {
        _session.Object.BatchID = 10;
        _session.Object.ReturnPage = "/Batches/BatchesForEditing";
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(SampleBatch(BatchStatus.Rejected));
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _batches.Setup(b => b.SaveBatchTestSelectionsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = true;
        sut.BatchDateStr = "15/01/2026";

        await sut.OnPostAsync();

        _batches.Verify(b => b.UpdateAsync(It.Is<Batch>(x => x.Status == BatchStatus.Submitted), 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_UpdateAsyncThrows_SetsSaveError()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(SampleBatch());
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("concurrency conflict"));
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = true;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to save the submission. Please try again.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_SaveTestSelectionsFails_SetsSaveErrorAfterBatchAlreadySaved()
    {
        _session.Object.BatchID = 10;
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(SampleBatch());
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _batches.Setup(b => b.SaveBatchTestSelectionsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = true;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Submission saved, but failed to save test types. Please try again.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_HappyPath_RedirectsToReturnPageWithParsedQuery()
    {
        _session.Object.BatchID = 10;
        _session.Object.ReturnPage = "/Batches/BatchesForEditing";
        _session.Object.ReturnPageQuery = "?SortColumn=Species&PageNumber=2";
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(SampleBatch());
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _batches.Setup(b => b.SaveBatchTestSelectionsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SafeToHandle = true;

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesForEditing", redirect.PageName);
        Assert.Equal("Species", redirect.RouteValues!["SortColumn"]);
        Assert.Equal("2", redirect.RouteValues!["PageNumber"]);
    }

    // ── Computed properties ──────────────────────────────────────────────────────

    [Fact]
    public void ReturnPage_EditBatchReturnPageSet_TakesPrecedence()
    {
        _session.Object.EditBatchReturnPage = "/Batches/EditSubmissionStatus";
        _session.Object.ReturnPage = "/Batches/BatchesForEditing";
        var sut = CreateSut();

        Assert.Equal("/Batches/EditSubmissionStatus", sut.ReturnPage);
    }

    [Fact]
    public void ReturnPage_OnlyGeneralReturnPageSet_UsesIt()
    {
        _session.Object.ReturnPage = "/Batches/BatchesReceived";
        var sut = CreateSut();

        Assert.Equal("/Batches/BatchesReceived", sut.ReturnPage);
    }

    [Fact]
    public void ReturnPage_NeitherSet_FallsBackToBatchesForEditing()
    {
        var sut = CreateSut();

        Assert.Equal("/Batches/BatchesForEditing", sut.ReturnPage);
    }

    [Fact]
    public void CanEditSubmittedArea_UserAreaIsHistopath_ReturnsTrue()
    {
        _session.Setup(s => s.UserArea).Returns("Histopath");
        var sut = CreateSut();

        Assert.True(sut.CanEditSubmittedArea);
    }

    [Fact]
    public void CanEditSubmittedArea_UserAreaIsNotHistopath_ReturnsFalse()
    {
        _session.Setup(s => s.UserArea).Returns("Neuropath");
        var sut = CreateSut();

        Assert.False(sut.CanEditSubmittedArea);
    }

    [Fact]
    public void ShowAntibodies_IhcOtherSelected_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.IhcOther];

        Assert.True(sut.ShowAntibodies);
    }

    [Fact]
    public void ShowStains_SpecialStainSelected_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.SpecialStain];

        Assert.True(sut.ShowStains);
    }
}
