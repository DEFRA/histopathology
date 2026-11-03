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

/// <summary>Unit tests for <see cref="DateReturnedModel"/>.</summary>
public class DateReturnedModelTests
{
    private static readonly byte[] RowStamp = [1];

    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();

    public DateReturnedModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.ReturnPage);
        _session.SetupGet(s => s.UserID).Returns(9);
        _lookups.Setup(l => l.GetLookupDataAsync(19, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 7, Name = "Project Seven" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 3, Name = "Dr Three" }]);
    }

    private DateReturnedModel CreateSut() =>
        new(_session.Object, _batches.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    private void ExistingBatch(string status = BatchStatus.Completed, DateTime? returned = null) =>
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 5, Status = status, ProjectContractCode = "7", ContactName = "3", CustomerReceivedDate = returned, RowStamp = RowStamp });

    [Fact]
    public void BackLinkPage_ReadOnlyOrNoSessionReturnPage_FallsBackToBatchDetails()
    {
        _session.Object.ReturnPage = "/Somewhere";
        var sut = CreateSut();
        sut.ReadOnly = true;

        Assert.Equal("/Batches/BatchDetails", sut.BackLinkPage);

        sut.ReadOnly = false;
        Assert.Equal("/Somewhere", sut.BackLinkPage);
    }

    [Fact]
    public async Task OnGetAsync_NoBatchInSession_RedirectsToViewSubmissionsAndSetsTitles()
    {
        _session.Object.BatchID = null;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.Equal("/Submissions/ViewSubmissions", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("Date returned", sut.ViewData["Title"]);
        Assert.Equal("Date returned", sut.ViewData["PageTitle"]);
    }

    [Fact]
    public async Task OnGetAsync_BatchNotFound_RedirectsToViewSubmissions()
    {
        _session.Object.BatchID = 5;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.Equal("/Submissions/ViewSubmissions", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task OnGetAsync_BatchNotCompleted_ReturnsPageWithError()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchStatus.InProgress);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("The date returned can only be recorded for a completed submission.", sut.Error);
        Assert.Null(sut.CustomerReceivedDate);
    }

    [Fact]
    public async Task OnGetAsync_CompletedBatch_ResolvesNamesAndPrefillsDate()
    {
        _session.Object.BatchID = 5;
        var returned = new DateTime(2025, 2, 3);
        ExistingBatch(returned: returned);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Project Seven", sut.ProjectName);
        Assert.Equal("Dr Three", sut.PathologistName);
        Assert.Equal(returned, sut.CustomerReceivedDate);
    }

    [Fact]
    public async Task OnPostAsync_ReadOnly_RedirectsToBackLinkWithoutSaving()
    {
        var sut = CreateSut();
        sut.ReadOnly = true;

        var result = await sut.OnPostAsync();

        Assert.Equal("/Batches/BatchDetails", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("Date returned", sut.ViewData["Title"]);
        Assert.Equal("Date returned", sut.ViewData["PageTitle"]);
        _batches.Verify(b => b.SetCustomerReceivedDateAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_BatchMissingRowStamp_ReturnsPageWithNotFoundError()
    {
        _session.Object.BatchID = 5;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5 });
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Submission not found.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_NoDateEntered_ReturnsPageWithError()
    {
        _session.Object.BatchID = 5;
        ExistingBatch();
        var sut = CreateSut();

        await sut.OnPostAsync();

        Assert.Equal("Enter the date returned.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_FutureDate_ReturnsPageWithError()
    {
        _session.Object.BatchID = 5;
        ExistingBatch();
        var sut = CreateSut();
        sut.CustomerReceivedDate = DateTime.Today.AddDays(1);

        await sut.OnPostAsync();

        Assert.Equal("The date returned cannot be in the future.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_SaveFails_ReturnsPageWithConcurrencyError()
    {
        _session.Object.BatchID = 5;
        ExistingBatch();
        _batches.Setup(b => b.SetCustomerReceivedDateAsync(5, It.IsAny<DateTime?>(), RowStamp, 9, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();
        sut.CustomerReceivedDate = DateTime.Today;

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.StartsWith("Could not save the date returned.", sut.Error);
    }

    [Fact]
    public async Task OnPostAsync_ValidDate_SavesAndRedirectsToViewSubmissions()
    {
        _session.Object.BatchID = 5;
        ExistingBatch();
        _batches.Setup(b => b.SetCustomerReceivedDateAsync(5, DateTime.Today, RowStamp, 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.CustomerReceivedDate = DateTime.Today;

        var result = await sut.OnPostAsync();

        Assert.Equal("/Submissions/ViewSubmissions", Assert.IsType<RedirectToPageResult>(result).PageName);
        _batches.Verify(b => b.SetCustomerReceivedDateAsync(5, DateTime.Today, RowStamp, 9, It.IsAny<CancellationToken>()), Times.Once);
    }
}
