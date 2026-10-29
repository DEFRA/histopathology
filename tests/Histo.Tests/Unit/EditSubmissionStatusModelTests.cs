using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="EditSubmissionStatusModel"/>.</summary>
public class EditSubmissionStatusModelTests
{
    private static readonly byte[] RowStamp = [1, 2, 3];

    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public EditSubmissionStatusModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.ReturnPage);
        _session.SetupProperty(s => s.ReturnPageQuery);
        _session.SetupProperty(s => s.EditBatchReturnPage);
        _session.SetupGet(s => s.UserID).Returns(9);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
    }

    private EditSubmissionStatusModel CreateSut() =>
        new(_session.Object, _batches.Object, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    private void ExistingBatch(string status, DateTime? completed = null, byte[]? rowStamp = null) =>
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 5, Status = status, StatusComments = "note", CompletedDate = completed, RowStamp = rowStamp ?? RowStamp });

    [Fact]
    public void ReturnUrl_NoSessionReturnPage_FallsBackToBatchesForEditing()
    {
        _session.Object.ReturnPage = string.Empty;
        _session.Object.ReturnPageQuery = "?x=1";
        var sut = CreateSut();

        Assert.Equal("/Batches/BatchesForEditing", sut.ReturnPage);
        Assert.Equal("/Batches/BatchesForEditing?x=1", sut.ReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_InvalidBatchInSession_RedirectsToIndexAndSetsTitles()
    {
        _session.Object.BatchID = 0;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("Edit submission status", sut.ViewData["Title"]);
        Assert.Equal("Edit submission status", sut.ViewData["PageTitle"]);
    }

    [Fact]
    public async Task OnGetAsync_BatchNotFound_RedirectsToIndex()
    {
        _session.Object.BatchID = 5;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_BatchFound_PopulatesStatusFieldsAndReturnsPage()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchStatus.OnHold);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(BatchStatus.OnHold, sut.Status);
        Assert.Equal(BatchStatus.OnHold, sut.OriginalStatus);
        Assert.Equal("note", sut.StatusComments);
    }

    [Fact]
    public async Task OnPostSaveAsync_BatchMissingRowStamp_RedirectsToIndexAndSetsTitles()
    {
        _session.Object.BatchID = 5;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        var sut = CreateSut();

        var result = await sut.OnPostSaveAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("Edit submission status", sut.ViewData["Title"]);
        Assert.Equal("Edit submission status", sut.ViewData["PageTitle"]);
    }

    [Fact]
    public async Task OnPostSaveAsync_MarkingReceivedFromAnotherStatus_IsRejected()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchStatus.OnHold);
        var sut = CreateSut();
        sut.Status = BatchStatus.Received;
        sut.OriginalStatus = BatchStatus.OnHold;

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Mark a submission as Received using the Receive Submissions workflow.", sut.SaveError);
        _batches.Verify(b => b.UpdateAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostSaveAsync_InProgressWhileStillSubmitted_IsRejected()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchStatus.Submitted);
        var sut = CreateSut();
        sut.Status = BatchStatus.InProgress;
        sut.OriginalStatus = BatchStatus.Submitted;

        await sut.OnPostSaveAsync();

        Assert.Equal("The submission cannot be set to In Progress while still Submitted. Receive it first.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostSaveAsync_NewlyCompleted_StampsTodayAndRedirectsToReturnPage()
    {
        _session.Object.BatchID = 5;
        _session.Object.ReturnPage = "/Batches/BatchesForEditing";
        ExistingBatch(BatchStatus.InProgress);
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Status = BatchStatus.Completed;
        sut.OriginalStatus = BatchStatus.InProgress;
        sut.StatusComments = "done";

        var result = await sut.OnPostSaveAsync();

        Assert.Equal("/Batches/BatchesForEditing", Assert.IsType<RedirectToPageResult>(result).PageName);
        _batches.Verify(b => b.UpdateAsync(
            It.Is<Batch>(u => u.Status == BatchStatus.Completed && u.CompletedDate == DateTime.Today && u.StatusComments == "done" && u.RowStamp == RowStamp),
            9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostSaveAsync_StaysCompleted_KeepsExistingCompletedDate()
    {
        var completed = new DateTime(2024, 3, 1);
        _session.Object.BatchID = 5;
        ExistingBatch(BatchStatus.Completed, completed);
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Status = BatchStatus.Completed;
        sut.OriginalStatus = BatchStatus.Completed;

        await sut.OnPostSaveAsync();

        _batches.Verify(b => b.UpdateAsync(It.Is<Batch>(u => u.CompletedDate == completed), 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostSaveAsync_MovedAwayFromCompleted_ClearsCompletedDate()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchStatus.Completed, new DateTime(2024, 3, 1));
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Status = BatchStatus.OnHold;
        sut.OriginalStatus = BatchStatus.Completed;

        await sut.OnPostSaveAsync();

        _batches.Verify(b => b.UpdateAsync(It.Is<Batch>(u => u.CompletedDate == null && u.Status == BatchStatus.OnHold), 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostSaveAsync_UpdateThrows_ReturnsPageWithSaveError()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchStatus.InProgress);
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        var sut = CreateSut();
        sut.Status = BatchStatus.OnHold;
        sut.OriginalStatus = BatchStatus.InProgress;

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to save the submission status. Please try again.", sut.SaveError);
    }

    [Fact]
    public void OnPostContinueAsync_RecordsReturnPageAndRedirectsToEditBatch()
    {
        var sut = CreateSut();

        var result = sut.OnPostContinueAsync();

        Assert.Equal("/Batches/EditBatch", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("/Batches/EditSubmissionStatus", _session.Object.EditBatchReturnPage);
    }
}
