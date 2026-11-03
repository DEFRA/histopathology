using Histo.Administration.Interfaces;
using Histo.Core.Domain;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Create-form drafting, validation, access and display-flag branches of the submission details page.</summary>
public class BatchDetailsHandlerTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<IBlockTestService> _blockTests = new();

    public BatchDetailsHandlerTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.UserAreaID).Returns(5);
        _session.Setup(s => s.IsHistoUser).Returns(true);
        _session.SetupProperty(s => s.BatchID);
        _lookups.Setup(l => l.GetProjectsByAreaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetContactsByAreaAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Histo.Administration.Models.User>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new BatchTestSelections());
    }

    private BatchDetailsModel CreateSut(string? mode = null)
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("/Batches/BatchDetails?mode=create");
        url.SetupGet(u => u.ActionContext).Returns(new ActionContext(new Microsoft.AspNetCore.Http.DefaultHttpContext(), new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));
        return new BatchDetailsModel(_session.Object, _batches.Object, _lookups.Object, _users.Object, _submissions.Object, _blocks.Object, _blockTests.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            Url = url.Object,
            Mode = mode,
        };
    }

    [Theory]
    [InlineData("Histopath", true)]
    [InlineData("Customer", false)]
    public void CanEditSubmittedArea_OnlyForHistopathArea(string area, bool expected)
    {
        _session.Setup(s => s.UserArea).Returns(area);

        Assert.Equal(expected, CreateSut().CanEditSubmittedArea);
    }

    [Theory]
    [InlineData(new[] { HistologyCode.IhcPrp }, true, false)]
    [InlineData(new[] { HistologyCode.IhcOther }, true, false)]
    [InlineData(new[] { HistologyCode.SpecialStain }, false, true)]
    [InlineData(new[] { HistologyCode.EO }, false, false)]
    public void CreateShowFlags_FollowSelectedHistologyCodes(string[] codes, bool antibodies, bool stains)
    {
        var sut = CreateSut();
        sut.Create_SelectedHistologyCodes = [.. codes];

        Assert.Equal(antibodies, sut.Create_ShowAntibodies);
        Assert.Equal(stains, sut.Create_ShowStains);
    }

    [Fact]
    public void StatusGates_NoBatchLoaded_AreAllClosed()
    {
        var sut = CreateSut();

        Assert.False(sut.CanModifySamples);
        Assert.False(sut.CanDateReturned);
    }

    [Theory]
    [InlineData(null, "/Index")]
    [InlineData("  ", "/Index")]
    [InlineData("/Batches/ViewBatches", "/Batches/ViewBatches")]
    public void BackLinkPage_UsesSessionReturnPageOrIndex(string? returnPage, string expected)
    {
        _session.Setup(s => s.ReturnPage).Returns(returnPage ?? string.Empty);

        Assert.Equal(expected, CreateSut().BackLinkPage);
    }

    [Theory]
    [InlineData("submittedBy", "/Admin/UserMaintenance")]
    [InlineData("project", "/Admin/LookupItems")]
    [InlineData("pathologist", "/Admin/LookupItems")]
    [InlineData("other", "/Batches/BatchDetails")]
    public void OnPostManagePickList_RedirectsToTheMatchingMaintenancePage(string field, string expectedPage)
    {
        var sut = CreateSut("create");

        var result = sut.OnPostManagePickList(field);

        Assert.Equal(expectedPage, Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.True(sut.TempData.ContainsKey("BatchDetails_CreateDraft"));
    }

    [Fact]
    public async Task OnGet_CreateModeAfterPickListDetour_RestoresDraftValues()
    {
        var first = CreateSut("create");
        first.Create_ProjectContractCode = "P9";
        first.Create_Comments = "kept";
        first.Create_SelectedHistologyCodes = [HistologyCode.EO];
        first.OnPostManagePickList("project");
        var second = CreateSut("create");
        second.TempData["BatchDetails_CreateDraft"] = first.TempData["BatchDetails_CreateDraft"];

        await second.OnGetAsync();

        Assert.Equal("P9", second.Create_ProjectContractCode);
        Assert.Equal("kept", second.Create_Comments);
        Assert.Equal([HistologyCode.EO], second.Create_SelectedHistologyCodes);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public async Task OnGet_CreateModeWithUnusableDraft_FallsBackToDefaults(string draft)
    {
        var sut = CreateSut("create");
        sut.TempData["BatchDetails_CreateDraft"] = draft;

        await sut.OnGetAsync();

        Assert.Equal(7, sut.Create_OtherSubmittedBy);
        Assert.Equal("5", sut.Create_OtherSubmittedArea);
        Assert.Equal(DateTime.Today.ToString("yyyy-MM-dd"), sut.Create_BatchDateStr);
    }

    [Fact]
    public async Task OnGet_CreateModeWithMismatchedCopyToken_DiscardsPendingCopy()
    {
        var sut = CreateSut("create");
        sut.CopyToken = "other";
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(new CopyBatchModel.PendingCopy(3, [], "token"));

        await sut.OnGetAsync();

        Assert.False(sut.TempData.ContainsKey("CopyBatch_PendingCopy"));
        Assert.Empty(sut.Create_CopiedSamples);
    }

    [Fact]
    public async Task OnGet_CreateModeWithMalformedPendingCopy_DiscardsIt()
    {
        var sut = CreateSut("create");
        sut.CopyToken = "token";
        sut.TempData["CopyBatch_PendingCopy"] = "{bad";

        await sut.OnGetAsync();

        Assert.False(sut.TempData.ContainsKey("CopyBatch_PendingCopy"));
    }

    [Fact]
    public async Task OnGet_CreateModeWithMatchingCopyToken_PrefillsFromSourceBatch()
    {
        var sut = CreateSut("create");
        sut.CopyToken = "token";
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(
            new CopyBatchModel.PendingCopy(3, [new CopyBatchModel.AnimalRow { AnimalId = 1, NewSenderRef = "S2" }], "token"));
        _batches.Setup(b => b.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch
        {
            ID = 3, ProjectContractCode = "P1", ContactName = "C1", Species = "2", Fixation = "F1", SafeToHandle = true,
            OtherSubmittedBy = 11, OtherSubmittedArea = "8", Comments = "src", IsPreCassetted = true,
        });
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(new BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { Code = HistologyCode.EO }],
            Antibodies = [new BatchTestSelectionRow { Code = "A1" }],
            Stains = [new BatchTestSelectionRow { Code = "S1" }],
        });
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync("2");
        _lookups.Setup(l => l.GetLookupDataAsync(11, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 0, Code = "2", Name = "Wax Block" }]);

        await sut.OnGetAsync();

        Assert.Equal("P1", sut.Create_ProjectContractCode);
        Assert.Equal(11, sut.Create_OtherSubmittedBy);
        Assert.Equal("8", sut.Create_OtherSubmittedArea);
        Assert.Equal([HistologyCode.EO], sut.Create_SelectedHistologyCodes);
        Assert.Equal(["A1"], sut.Create_SelectedAntibodyCodes);
        Assert.Equal(["S1"], sut.Create_SelectedStainCodes);
        Assert.Equal("Wax Block", sut.Create_SubmittedAsName);
        Assert.Equal("2", sut.TempData["CreateSubmittedAsCode"]);
        Assert.Single(sut.Create_CopiedSamples);
    }

    [Fact]
    public async Task OnGet_CreateModeWithMatchingTokenButMissingSourceBatch_LeavesFormBlank()
    {
        var sut = CreateSut("create");
        sut.CopyToken = "token";
        sut.TempData["CopyBatch_PendingCopy"] = System.Text.Json.JsonSerializer.Serialize(new CopyBatchModel.PendingCopy(3, [], "token"));

        await sut.OnGetAsync();

        Assert.Null(sut.Create_ProjectContractCode);
    }

    [Fact]
    public async Task OnGet_CreateModeSelectedProjectMissingFromAreaList_AddsItFromFullLookup()
    {
        var sut = CreateSut("create");
        sut.TempData["BatchDetails_CreateDraft"] = System.Text.Json.JsonSerializer.Serialize(new
        {
            ProjectContractCode = "4", ContactName = "6", SpeciesId = (string?)null, BatchDateStr = (string?)null, Fixation = (string?)null,
            SafeToHandle = (bool?)null, OtherSubmittedBy = (int?)null, OtherSubmittedArea = (string?)null, Comments = (string?)null,
            Histology = Array.Empty<string>(), Antibodies = Array.Empty<string>(), Stains = Array.Empty<string>(),
        });
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 4, Code = "4", Name = "Project" }, new LookupItem { ID = 6, Code = "6", Name = "Contact" }]);

        await sut.OnGetAsync();

        Assert.Contains(sut.Create_Projects, p => p.ID == 4);
        Assert.Contains(sut.Create_Contacts, c => c.ID == 6);
    }

    [Fact]
    public async Task OnGet_ViewModeWithoutAnyBatchId_RedirectsToIndex()
    {
        var result = await CreateSut().OnGetAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGet_ViewModeForAnotherAreasBatch_ReturnsForbid()
    {
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(1);
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, UserAreaCode = 9 });
        var sut = CreateSut();
        sut.BatchId = 5;

        var result = await sut.OnGetAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnGet_ViewModeBatchLoadFails_ShowsLoadError()
    {
        _batches.SetupSequence(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 5 })
            .ThrowsAsync(new InvalidOperationException("db"));
        var sut = CreateSut();
        sut.BatchId = 5;

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(sut.LoadError);
    }

    [Fact]
    public async Task OnGet_ViewModeSessionBatchFallback_LoadsBatchAndStatusGates()
    {
        _session.Object.BatchID = 5;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, Status = BatchStatus.Completed });
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(5, sut.BatchId);
        Assert.True(sut.CanDateReturned);
        Assert.False(sut.CanModifySamples);
    }

    [Fact]
    public async Task OnPostCreate_EveryRequiredFieldMissing_ReportsEachError()
    {
        var sut = CreateSut("create");

        var result = await sut.OnPostCreateAsync();

        Assert.IsType<PageResult>(result);
        foreach (var key in new[] { "Create_ProjectContractCode", "Create_ContactName", "Create_SpeciesId", "Create_BatchDateStr",
                     "Create_OtherSubmittedBy", "Create_OtherSubmittedArea", "Create_Histology", "Create_SafeToHandle" })
            Assert.True(sut.Errors.ContainsKey(key), key);
        _batches.Verify(b => b.AddAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostCreate_UnparseableDate_ReportsInvalidDate()
    {
        var sut = FilledSut();
        sut.Create_BatchDateStr = "31-31-2026";

        await sut.OnPostCreateAsync();

        Assert.Equal("Enter a valid submission date.", sut.Errors["Create_BatchDateStr"]);
    }

    [Fact]
    public async Task OnPostCreate_ArchiveCombinedWithOtherType_IsRejected()
    {
        var sut = FilledSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.Archive, HistologyCode.EO];

        await sut.OnPostCreateAsync();

        Assert.Contains("Archive cannot be combined", sut.Errors["Create_Histology"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostCreate_IhcAndStainWithoutDetailSelections_RequiresAntibodiesAndStains()
    {
        var sut = FilledSut();
        sut.Create_SelectedHistologyCodes = [HistologyCode.IhcPrp, HistologyCode.SpecialStain];

        await sut.OnPostCreateAsync();

        Assert.True(sut.Errors.ContainsKey("Create_Antibodies"));
        Assert.True(sut.Errors.ContainsKey("Create_Stains"));
    }

    private BatchDetailsModel FilledSut()
    {
        var sut = CreateSut("create");
        sut.Create_ProjectContractCode = "PC1";
        sut.Create_ContactName = "Dr Smith";
        sut.Create_SpeciesId = "1";
        sut.Create_BatchDateStr = "2026-10-01";
        sut.Create_SafeToHandle = true;
        sut.Create_OtherSubmittedBy = 7;
        sut.Create_OtherSubmittedArea = "5";
        sut.Create_SelectedHistologyCodes = [HistologyCode.EO];
        return sut;
    }

    [Theory]
    [InlineData(BatchTypeConstants.NonTse, HistologyCode.IhcPrp, false)]
    [InlineData(BatchTypeConstants.NonTse, HistologyCode.IhcOther, true)]
    [InlineData(BatchTypeConstants.Tse, HistologyCode.IhcOther, false)]
    [InlineData(BatchTypeConstants.Tse, HistologyCode.IhcPrp, true)]
    public async Task OnGet_CreateMode_HistologyOptionsExcludeCodesNotValidForTheBatchType(int batchType, string code, bool expectedListed)
    {
        _session.Setup(s => s.BatchType).Returns(batchType);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[
            new LookupItem { ID = 1, Code = HistologyCode.IhcPrp, Name = "PrP" },
            new LookupItem { ID = 2, Code = HistologyCode.IhcOther, Name = "Other" },
            new LookupItem { ID = 3, Code = HistologyCode.HeBse, Name = "BSE" },
        ]);
        var sut = CreateSut("create");

        await sut.OnGetAsync();

        Assert.Equal(expectedListed, sut.Create_HistologyOptions.Any(o => o.Code == code));
    }
}
