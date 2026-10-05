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
}
