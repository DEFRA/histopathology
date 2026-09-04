using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Submissions;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="ViewSubmissionsModel"/>.</summary>
public class ViewSubmissionsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<ILookupService> _lookups = new();

    public ViewSubmissionsModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
        _session.SetupProperty(s => s.IsViewSubmissionMode);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private ViewSubmissionsModel CreateSut() =>
        new(_session.Object, _batches.Object, _users.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnPostSearchAsync_PopulatesResultsAndSetsSearched()
    {
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[new BatchSearchResult { ID = 1 }]);
        var sut = CreateSut();

        var result = await sut.OnPostSearchAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.Searched);
        Assert.Single(sut.Results);
    }

    [Fact]
    public async Task OnPostSelectAsync_PositiveBatchId_SetsSessionState()
    {
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[]);
        var sut = CreateSut();
        sut.SelectedBatchId = 7;

        await sut.OnPostSelectAsync();

        Assert.Equal(7, _session.Object.BatchID);
        Assert.Equal("/Submissions/ViewSubmissions", _session.Object.ReturnPage);
        Assert.True(_session.Object.IsViewSubmissionMode);
    }

    [Fact]
    public async Task OnPostSelectAsync_ZeroBatchId_DoesNotSetSessionState()
    {
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[]);
        var sut = CreateSut();
        sut.SelectedBatchId = 0;

        await sut.OnPostSelectAsync();

        Assert.Null(_session.Object.BatchID);
        Assert.False(_session.Object.IsViewSubmissionMode);
    }

    [Fact]
    public async Task OnPostExportCsvAsync_ReturnsFileResult()
    {
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[]);
        var sut = CreateSut();

        var result = await sut.OnPostExportCsvAsync();

        Assert.IsAssignableFrom<FileResult>(result);
    }

    // ── Action-button availability matrix (mirrors grdviewResults_SelectedIndexChanged) ────

    [Theory]
    [InlineData(BatchStatus.Submitted, true, false)]
    [InlineData(BatchStatus.Rejected, true, false)]
    [InlineData(BatchStatus.Received, false, false)]
    [InlineData(BatchStatus.OnHold, false, false)]
    [InlineData(BatchStatus.InProgress, false, false)]
    [InlineData(BatchStatus.Completed, false, true)]
    public async Task ActionButtons_ReflectStatusGatingMatrix(string status, bool canEdit, bool canDateReturned)
    {
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[new BatchSearchResult { ID = 7, Status = status }]);
        var sut = CreateSut();
        sut.SelectedBatchId = 7;

        await sut.OnPostSelectAsync();

        Assert.Equal(canEdit, sut.CanEditSubmission);
        Assert.Equal(canDateReturned, sut.CanDateReturned);
        Assert.True(sut.CanViewSubmission);
        Assert.True(sut.CanCopySubmission);
    }

    [Fact]
    public void ActionButtons_NoSelection_AllDisabled()
    {
        var sut = CreateSut();

        Assert.False(sut.CanEditSubmission);
        Assert.False(sut.CanViewSubmission);
        Assert.False(sut.CanCopySubmission);
        Assert.False(sut.CanDateReturned);
    }
}
