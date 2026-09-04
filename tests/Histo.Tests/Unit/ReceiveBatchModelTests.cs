using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Core.Domain;
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
/// Unit tests for <see cref="ReceiveBatchModel"/> — the Receive Submission workflow
/// (status transitions and receipt-field validation).
/// </summary>
public class ReceiveBatchModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<IBlockService> _blocks = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public ReceiveBatchModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
        _session.SetupProperty(s => s.BatchType);
        _session.Setup(s => s.UserID).Returns(99);

        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _blocks.Setup(b => b.GetByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Block>)[]);
        _batches.Setup(b => b.GetPostFixationCodesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<string>)[]);
        _batches.Setup(b => b.SavePostFixationCodesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private ReceiveBatchModel CreateSut() =>
        new(_session.Object, _batches.Object, _blocks.Object, _lookups.Object, _users.Object)
        {
            // PageModel.ViewData is only populated by the MVC pipeline — SetTitle() writes to
            // it, so it must be seeded here when constructing the model outside that pipeline.
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    private static Batch MakeBatch(string status = BatchStatus.Submitted, byte[]? rowStamp = null, DateTime? batchDate = null) => new()
    {
        ID = 1,
        Status = status,
        BatchDate = batchDate ?? new DateTime(2026, 1, 1),
        RowStamp = rowStamp ?? [0x01],
    };

    // ── OnGetAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_NoBatchIdInSession_RedirectsToBatchesNotReceived()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesNotReceived", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_BatchNotFound_RedirectsToBatchesNotReceived()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesNotReceived", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_ExistingBatch_PopulatesFieldsFromBatch()
    {
        _session.Object.BatchID = 1;
        var batch = MakeBatch(status: BatchStatus.Received);
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal(BatchStatus.Received, sut.Status);
        Assert.True(sut.IsReadOnly);
    }

    [Fact]
    public async Task OnGetAsync_SubmittedBatch_IsNotReadOnly()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.False(sut.IsReadOnly);
    }

    // ── OnPostSaveAsync — validation matrix for Received/Rejected ───────────

    [Theory]
    [InlineData(BatchStatus.Received)]
    [InlineData(BatchStatus.Rejected)]
    public async Task OnPostSaveAsync_MissingReceivedBy_ReturnsPageWithError(string status)
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        var sut = CreateSut();
        sut.Status = status;
        sut.TimeReceived = "1";
        sut.DateReceived = DateTime.Today;
        sut.ReceivedByUserId = null;
        sut.Reason = "Some reason";

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select who received or rejected the submission.", sut.Error);
    }

    [Theory]
    [InlineData(BatchStatus.Received)]
    [InlineData(BatchStatus.Rejected)]
    public async Task OnPostSaveAsync_MissingTimeReceived_ReturnsPageWithError(string status)
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        var sut = CreateSut();
        sut.Status = status;
        sut.ReceivedByUserId = 5;
        sut.DateReceived = DateTime.Today;
        sut.TimeReceived = null;
        sut.Reason = "Some reason";

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select the time the submission was received or rejected.", sut.Error);
    }

    [Fact]
    public async Task OnPostSaveAsync_RejectedWithoutReason_ReturnsPageWithError()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        var sut = CreateSut();
        sut.Status = BatchStatus.Rejected;
        sut.ReceivedByUserId = 5;
        sut.TimeReceived = "1";
        sut.DateReceived = DateTime.Today;
        sut.Reason = null;

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter a reason for rejecting the submission.", sut.Error);
    }

    [Fact]
    public async Task OnPostSaveAsync_DateReceivedBeforeSubmissionDate_ReturnsPageWithError()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeBatch(batchDate: DateTime.Today));
        var sut = CreateSut();
        sut.Status = BatchStatus.Received;
        sut.ReceivedByUserId = 5;
        sut.TimeReceived = "1";
        sut.DateReceived = DateTime.Today.AddDays(-1);

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains("must be the same as or later than the submission date", sut.Error);
    }

    [Fact]
    public async Task OnPostSaveAsync_DateReceivedAfterToday_ReturnsPageWithError()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        var sut = CreateSut();
        sut.Status = BatchStatus.Received;
        sut.ReceivedByUserId = 5;
        sut.TimeReceived = "1";
        sut.DateReceived = DateTime.Today.AddDays(1);

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The date received must be today or earlier.", sut.Error);
    }

    [Theory]
    [InlineData(BatchStatus.Received)]
    [InlineData(BatchStatus.Rejected)]
    public async Task OnPostSaveAsync_ValidReceiptDetails_SavesAndRedirectsToBatchesNotReceived(string status)
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Status = status;
        sut.ReceivedByUserId = 5;
        sut.TimeReceived = "1";
        sut.DateReceived = DateTime.Today;
        sut.Reason = "Rejected for cause";

        var result = await sut.OnPostSaveAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesNotReceived", redirect.PageName);
    }

    [Fact]
    public async Task OnPostSaveAsync_BatchNotFound_SetsErrorAndReturnsPage()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Submission not found.", sut.Error);
    }

    [Fact]
    public async Task OnPostSaveAsync_UpdateThrows_SetsErrorAndReturnsPage()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        _batches.Setup(b => b.UpdateAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("concurrency"));
        var sut = CreateSut();
        sut.Status = BatchStatus.Submitted;

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Could not save the receipt details. It may have been modified by another user.", sut.Error);
    }

    [Fact]
    public async Task OnPostSaveAsync_BatchAlreadyActioned_Redirects()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch(status: BatchStatus.Received));
        var sut = CreateSut();

        var result = await sut.OnPostSaveAsync();

        Assert.IsType<RedirectToPageResult>(result);
    }

    // ── Cancel navigation ────────────────────────────────────────────────────

    [Fact]
    public void OnPostCancel_NoReturnPageSet_RedirectsToBatchesNotReceived()
    {
        _session.Object.ReturnPage = string.Empty;
        var sut = CreateSut();

        var result = sut.OnPostCancel();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchesNotReceived", redirect.PageName);
    }

    [Fact]
    public void OnPostCancel_ReturnPageSet_HonoursIt()
    {
        _session.Object.ReturnPage = "/Search/SearchSubmissions";
        var sut = CreateSut();

        var result = sut.OnPostCancel();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Search/SearchSubmissions", redirect.PageName);
    }
}
