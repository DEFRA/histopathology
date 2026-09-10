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
/// Unit tests for <see cref="BatchDetailsModel"/> (view mode) — read-only batch summary
/// and its status-gated action availability, which mirrors legacy <c>EnableDisableControls</c>.
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
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.BatchType);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
        _session.SetupProperty(s => s.IsViewSubmissionMode);
        _session.Setup(s => s.IsHistoUser).Returns(true);

        _batches.Setup(b => b.GetBatchTestSelectionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchTestSelections());
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Animal>)[]);
    }

    private BatchDetailsModel CreateSut() =>
        new(_session.Object, _batches.Object, _lookups.Object, _users.Object, _submissions.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = Mock.Of<ITempDataDictionary>(),
        };

    private static Batch MakeBatch(string status, int userAreaCode = 5) => new()
    {
        ID = 1,
        Status = status,
        UserAreaCode = userAreaCode,
    };

    [Fact]
    public async Task OnGetAsync_NoBatchIdInSession_RedirectsToIndex()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_HappyPath_LoadsBatchAndSyncsSessionBatchType()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch(BatchStatus.Received));
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(sut.Batch);
        Assert.Equal(BatchTypeConstants.Tse, _session.Object.BatchType);
    }

    [Fact]
    public async Task OnGetAsync_AreaRestrictedUserFromDifferentArea_ReturnsForbid()
    {
        _session.Object.BatchID = 1;
        _session.Setup(s => s.IsHistoUser).Returns(false);
        _session.Setup(s => s.UserAreaID).Returns(99);
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch(BatchStatus.Received, userAreaCode: 5));
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<ForbidResult>(result);
    }

    [Theory]
    [InlineData(BatchStatus.Submitted, true)]
    [InlineData(BatchStatus.Rejected, true)]
    [InlineData(BatchStatus.Received, false)]
    [InlineData(BatchStatus.InProgress, false)]
    [InlineData(BatchStatus.Completed, false)]
    public async Task CanModifySamples_ReflectsStatus(string status, bool expected)
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch(status));
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(expected, sut.CanModifySamples);
    }

    [Fact]
    public async Task CanDateReturned_OnlyTrueForCompleted()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch(BatchStatus.Completed));
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.True(sut.CanDateReturned);
    }

    [Fact]
    public void BackLinkPage_NoReturnPageSet_DefaultsToIndex()
    {
        _session.Object.ReturnPage = string.Empty;
        var sut = CreateSut();

        Assert.Equal("/Index", sut.BackLinkPage);
    }

    [Fact]
    public void BackLinkPage_ReturnPageSet_HonoursIt()
    {
        _session.Object.ReturnPage = "/Submissions/ViewSubmissions";
        var sut = CreateSut();

        Assert.Equal("/Submissions/ViewSubmissions", sut.BackLinkPage);
    }
}
