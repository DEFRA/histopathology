using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Search;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="SearchSubmissionsModel"/>.</summary>
public class SearchSubmissionsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IUserService> _users = new();

    public SearchSubmissionsModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
        _session.SetupProperty(s => s.IsViewSubmissionMode);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
    }

    private SearchSubmissionsModel CreateSut() =>
        new(_session.Object, _batches.Object, _users.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    [Fact]
    public async Task OnPostAsync_PopulatesResultsAndSetsSearched()
    {
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[new BatchSearchResult { ID = 1 }]);
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

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
        Assert.Equal("/Search/SearchSubmissions", _session.Object.ReturnPage);
        Assert.True(_session.Object.IsViewSubmissionMode);
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

    // ── Action-button availability matrix (mirrors grdSearchResults_SelectedIndexChanged) ──

    [Theory]
    [InlineData(BatchStatus.Submitted, true, false, false, false, false)]
    [InlineData(BatchStatus.Rejected, false, false, true, false, true)]
    [InlineData(BatchStatus.Received, true, true, true, true, true)]
    [InlineData(BatchStatus.OnHold, true, true, true, true, true)]
    [InlineData(BatchStatus.InProgress, true, true, true, true, true)]
    [InlineData(BatchStatus.Completed, true, false, true, true, true)]
    public async Task ActionButtons_ReflectStatusGatingMatrix(
        string status, bool canPrint, bool canEdit, bool canView, bool canQualityData, bool canReceipt)
    {
        _batches.Setup(b => b.SearchAsync(It.IsAny<BatchSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSearchResult>)[new BatchSearchResult { ID = 7, Status = status }]);
        var sut = CreateSut();
        sut.SelectedBatchId = 7;

        await sut.OnPostSelectAsync();

        Assert.Equal(canPrint, sut.CanPrintSubmission);
        Assert.Equal(canEdit, sut.CanEditSubmission);
        Assert.Equal(canView, sut.CanViewSubmission);
        Assert.Equal(canQualityData, sut.CanViewQualityData);
        Assert.Equal(canQualityData, sut.CanViewArchiveData);
        Assert.Equal(canReceipt, sut.CanViewReceipt);
    }

    [Fact]
    public void ActionButtons_NoSelection_AllDisabled()
    {
        var sut = CreateSut();

        Assert.False(sut.CanPrintSubmission);
        Assert.False(sut.CanEditSubmission);
        Assert.False(sut.CanViewSubmission);
        Assert.False(sut.CanViewQualityData);
        Assert.False(sut.CanViewReceipt);
    }
}
