using Histo.Administration.Interfaces;
using Histo.Administration.Models;
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

/// <summary>Unit tests for <see cref="AddSubmissionModel"/> — the "Add sample" form.</summary>
public class AddSubmissionModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<ITempDataDictionary> _tempData = new();

    public AddSubmissionModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.BatchSubmissionID);
        _session.Setup(s => s.UserID).Returns(99);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(9);
        _lookups.Setup(l => l.GetLookupDataAsync(11, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new LookupItem { Code = "4", Name = "Wet Tissue" },
                new LookupItem { Code = "5", Name = "Wax Block" }
            ]);

        object? outValue = null;
        _tempData.Setup(t => t.TryGetValue(It.IsAny<string>(), out outValue)).Returns(false);
    }

    private AddSubmissionModel CreateSut() =>
        new(_session.Object, _submissions.Object, _batches.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = _tempData.Object,
        };

    [Fact]
    public async Task OnGetAsync_NoSenderRefProvided_LeavesSenderRefBlank()
    {
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        var sut = CreateSut();
        sut.BatchId = 1;

        await sut.OnGetAsync(null, null);

        Assert.Equal(string.Empty, sut.SenderRef);
    }

    [Fact]
    public async Task OnGetAsync_SenderRefQueryParam_PrefillsSenderRef()
    {
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        var sut = CreateSut();
        sut.BatchId = 1;

        await sut.OnGetAsync("S123", null);

        Assert.Equal("S123", sut.SenderRef);
    }

    [Fact]
    public async Task OnGetAsync_ExistingSubmission_ResolvesBatchSubmissionId()
    {
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 7, BatchID = 1 }]);
        var sut = CreateSut();
        sut.BatchId = 1;

        await sut.OnGetAsync(null, null);

        Assert.Equal(7, sut.BatchSubmissionId);
    }

    [Fact]
    public async Task OnPostAsync_NoBatchId_RedirectsToIndex()
    {
        var sut = CreateSut();
        sut.BatchId = null;

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_AddAnimalFails_ReturnsPageWithError()
    {
        _submissions.Setup(s => s.AddAnimalAsync(7, "S123", false, 99, It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var sut = CreateSut();
        sut.BatchId = 1;
        sut.BatchSubmissionId = 7;
        sut.SenderRef = "S123";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Could not add the sample. Please try again.", sut.ModelError);
    }

    [Fact]
    public async Task OnPostAsync_WetTissue_RoutesToSubmissionDetails()
    {
        _submissions.Setup(s => s.AddAnimalAsync(7, "S123", false, 99, It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(55);
        _submissions.Setup(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(9);
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("4");
        var sut = CreateSut();
        sut.BatchId = 1;
        sut.BatchSubmissionId = 7;
        sut.SenderRef = "S123";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetails", redirect.PageName);
        Assert.Equal(9, _session.Object.BatchSubmissionID);
    }

    [Fact]
    public async Task OnPostAsync_NotWetTissue_RoutesToSubmissionDetailsBlock()
    {
        _submissions.Setup(s => s.AddAnimalAsync(7, "S123", false, 99, It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(55);
        _batches.Setup(b => b.GetSubmittedAsCodeAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("5");
        var sut = CreateSut();
        sut.BatchId = 1;
        sut.BatchSubmissionId = 7;
        sut.SenderRef = "S123";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/SubmissionDetailsBlock", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_NoExistingSubmission_CreatesDefaultSubmission()
    {
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.AddSubmissionAsync(It.IsAny<BatchSubmission>(), 99, It.IsAny<CancellationToken>())).ReturnsAsync(9);
        _submissions.Setup(s => s.AddAnimalAsync(9, "S123", false, 99, It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(55);
        var sut = CreateSut();
        sut.BatchId = 1;
        sut.BatchSubmissionId = null;
        sut.SenderRef = "S123";

        var result = await sut.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(9, _session.Object.BatchSubmissionID);
    }
}
