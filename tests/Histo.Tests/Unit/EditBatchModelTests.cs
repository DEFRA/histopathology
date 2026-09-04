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

/// <summary>Unit tests for <see cref="EditBatchModel"/> — the "Edit submission" form.</summary>
public class EditBatchModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public EditBatchModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.BatchType);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
        _session.SetupProperty(s => s.IsViewSubmissionMode, true);
        _session.Setup(s => s.UserID).Returns(99);

        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
    }

    private EditBatchModel CreateSut() =>
        new(_session.Object, _batches.Object, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = Mock.Of<ITempDataDictionary>(),
        };

    private static Batch MakeBatch(string status = BatchStatus.Received, byte[]? rowStamp = null, bool useDefaultRowStamp = true) => new()
    {
        ID = 1,
        Status = status,
        RowStamp = rowStamp ?? (useDefaultRowStamp ? [0x01] : null),
        BatchDate = new DateTime(2026, 1, 1),
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
    public async Task OnGetAsync_BatchNotFound_RedirectsToIndex()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_ExistingBatch_PopulatesFieldsAndClearsViewMode()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(BatchStatus.Received, sut.Status);
        Assert.Equal("01/01/2026", sut.BatchDateStr);
        Assert.False(_session.Object.IsViewSubmissionMode);
    }

    [Fact]
    public async Task OnPostAsync_BatchHasNoRowStamp_RedirectsToIndex()
    {
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch(useDefaultRowStamp: false));
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_TransitionToReceivedDirectly_IsBlocked()
    {
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch(status: BatchStatus.Submitted));
        var sut = CreateSut();
        sut.Status = BatchStatus.Received;
        sut.OriginalStatus = BatchStatus.Submitted;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Mark a submission as Received using the Receive Submissions workflow.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_TransitionToInProgressWhileStillSubmitted_IsBlocked()
    {
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch(status: BatchStatus.Submitted));
        var sut = CreateSut();
        sut.Status = BatchStatus.InProgress;
        sut.OriginalStatus = BatchStatus.Submitted;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The submission cannot be set to In Progress while still Submitted. Receive it first.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_InvalidBatchDateFormat_ReturnsPageWithError()
    {
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        var sut = CreateSut();
        sut.Status = BatchStatus.Received;
        sut.OriginalStatus = BatchStatus.Received;
        sut.BatchDateStr = "not-a-date";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a valid date of submission in DD/MM/YYYY format.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_ValidUpdate_SavesAndRedirectsToReturnPage()
    {
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), 99, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Status = BatchStatus.Received;
        sut.OriginalStatus = BatchStatus.Received;
        sut.BatchDateStr = "02/01/2026";
        _session.Object.ReturnPage = "/Batches/BatchesForEditing";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesForEditing", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_UpdateThrows_ReturnsPageWithError()
    {
        _batches.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), 99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("concurrency"));
        var sut = CreateSut();
        sut.Status = BatchStatus.Received;
        sut.OriginalStatus = BatchStatus.Received;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to save the submission. Please try again.", sut.SaveError);
    }

    [Fact]
    public void ReturnPage_NoSessionValue_DefaultsToBatchesForEditing()
    {
        _session.Object.ReturnPage = string.Empty;
        var sut = CreateSut();

        Assert.Equal("/Batches/BatchesForEditing", sut.ReturnPage);
    }
}
