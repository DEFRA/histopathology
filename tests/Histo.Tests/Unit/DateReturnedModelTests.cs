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

/// <summary>Unit tests for <see cref="DateReturnedModel"/> — the "Date returned" workflow.</summary>
public class DateReturnedModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();

    public DateReturnedModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.Setup(s => s.UserID).Returns(99);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private DateReturnedModel CreateSut() =>
        new(_session.Object, _batches.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    private static Batch MakeBatch(string status = BatchStatus.Completed, byte[]? rowStamp = null) => new()
    {
        ID = 1,
        Status = status,
        RowStamp = rowStamp ?? [0x01],
    };

    [Fact]
    public async Task OnGetAsync_NoBatchIdInSession_RedirectsToViewSubmissions()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/ViewSubmissions", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_BatchNotFound_RedirectsToViewSubmissions()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/ViewSubmissions", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_BatchNotCompleted_ReturnsPageWithError()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch(status: BatchStatus.InProgress));
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The date returned can only be recorded for a completed submission.", sut.Error);
    }

    [Fact]
    public async Task OnGetAsync_CompletedBatch_PrefillsExistingDate()
    {
        _session.Object.BatchID = 1;
        var batch = MakeBatch();
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Null(sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_BatchNotFound_ReturnsPageWithError()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Submission not found.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_MissingDate_ReturnsPageWithError()
    {
        _session.Object.BatchID = 1;
        _batches.Setup(b => b.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeBatch());
        var sut = CreateSut();
        sut.CustomerReceivedDate = null;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Enter the date returned.", sut.Error);
    }
}
