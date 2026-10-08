using Histo.Administration.Interfaces;
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
/// Unit tests for <see cref="CopyBatchModel"/> — the Copy Submission workflow.
///
/// Redesigned (2026-10-07): Finish no longer writes anything to the database. It stages every
/// sample in the source submission (matching legacy's always-copy-everything behaviour — no
/// per-sample selection) and redirects to the Create Submission form (<see cref="BatchDetailsModel"/>,
/// mode=create), which performs the actual copy once the user submits that form — see
/// <c>BatchDetailsModelTests</c> for the consuming side.
/// </summary>
public class CopyBatchModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<ILookupService> _lookups = new();

    public CopyBatchModelTests()
    {
        _session.Setup(s => s.UserID).Returns(42);
        _session.SetupProperty(s => s.BatchType);
    }

    private CopyBatchModel CreateSut() =>
        new(_session.Object, _batches.Object, _submissions.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
        };

    private static Batch MakeSourceBatch(string status = BatchStatus.Submitted) => new()
    {
        ID = 10,
        Status = status,
        Comments = "source comments",
        SubmittedByUserID = 1,
        UserAreaCode = 1,
        IsPreCassetted = false,
        BatchType = BatchTypeConstants.Tse,
        ProjectContractCode = "PROJ1",
        ContactName = "A Contact",
        Species = "Mouse",
        BatchDate = new DateTime(2026, 1, 1),
        Fixation = "Formalin",
        SafeToHandle = true,
        OtherSubmittedBy = null,
        OtherSubmittedArea = null,
    };

    [Fact]
    public async Task OnPostAsync_NotConfirmed_ShowsConfirmPanelWithoutStaging()
    {
        var sourceBatch = MakeSourceBatch();
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(sourceBatch);

        var sut = CreateSut();
        sut.SourceBatchId = 10;
        sut.Confirm = false;
        sut.Animals = [new CopyBatchModel.AnimalRow { AnimalId = 7, SenderRef = "S1" }];

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(sut.ShowConfirmPanel);
        Assert.False(sut.TempData.ContainsKey("CopyBatch_PendingCopy"));
    }

    [Fact]
    public async Task OnPostAsync_Confirmed_StagesEverySampleAndRedirectsToCreateSubmission()
    {
        var sourceBatch = MakeSourceBatch();
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(sourceBatch);

        var sut = CreateSut();
        sut.SourceBatchId = 10;
        sut.Confirm = true;
        sut.Animals =
        [
            new CopyBatchModel.AnimalRow { AnimalId = 7, SenderRef = "S1", NewSenderRef = "S1-NEW" },
            new CopyBatchModel.AnimalRow { AnimalId = 8, SenderRef = "S2", NewSenderRef = "S2-NEW" },
        ];

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Batches/BatchDetails", redirect.PageName);
        Assert.Equal("create", redirect.RouteValues!["mode"]);

        var json = Assert.IsType<string>(sut.TempData["CopyBatch_PendingCopy"]);
        var pending = System.Text.Json.JsonSerializer.Deserialize<CopyBatchModel.PendingCopy>(json);
        Assert.NotNull(pending);
        Assert.Equal(10, pending!.SourceBatchId);
        Assert.Equal(2, pending.Samples.Count);
        Assert.False(string.IsNullOrEmpty(pending.Token));
        Assert.Equal(pending.Token, redirect.RouteValues["copyToken"]);
    }

    /// <summary>
    /// Regression: BatchDetailsModel's create-mode form reads Session.BatchType (not the source
    /// batch directly) to pick the TSE/Non-TSE antibody lookup table and to stamp the new batch's
    /// own BatchType. View/Search Submissions only ever set Session.BatchID, so copying a Non-TSE
    /// submission previously left whatever BatchType was already in session (e.g. a stale TSE
    /// value from an earlier, unrelated visit), rendering the wrong options and creating the copy
    /// with the wrong type.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_Confirmed_SetsSessionBatchTypeFromSourceBatch()
    {
        var sourceBatch = MakeSourceBatch();
        var nonTseBatch = new Batch
        {
            ID = sourceBatch.ID,
            Status = sourceBatch.Status,
            BatchType = BatchTypeConstants.NonTse,
            ProjectContractCode = sourceBatch.ProjectContractCode,
            ContactName = sourceBatch.ContactName,
            Species = sourceBatch.Species,
            BatchDate = sourceBatch.BatchDate,
            Fixation = sourceBatch.Fixation,
            SafeToHandle = sourceBatch.SafeToHandle,
        };
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(nonTseBatch);
        _session.Object.BatchType = BatchTypeConstants.Tse; // stale value from an earlier visit

        var sut = CreateSut();
        sut.SourceBatchId = 10;
        sut.Confirm = true;
        sut.Animals = [new CopyBatchModel.AnimalRow { AnimalId = 7, SenderRef = "S1" }];

        await sut.OnPostAsync();

        Assert.Equal(BatchTypeConstants.NonTse, _session.Object.BatchType);
    }

    [Fact]
    public async Task OnPostAsync_SourceBatchNotFound_DoesNotStage()
    {
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);

        var sut = CreateSut();
        sut.SourceBatchId = 10;

        await sut.OnPostAsync();

        Assert.Equal("The submission to copy could not be found.", sut.Error);
        Assert.False(sut.TempData.ContainsKey("CopyBatch_PendingCopy"));
    }

    [Fact]
    public void DisplayRows_GroupsRangeCopiesIntoLegacyStyleDisplay()
    {
        var sut = CreateSut();
        sut.Animals =
        [
            new CopyBatchModel.AnimalRow { AnimalId = 7, SubmissionId = 11, SenderRef = "MC000001", NewSenderRef = "MC000002" },
            new CopyBatchModel.AnimalRow { AnimalId = 7, SubmissionId = 11, SenderRef = "MC000001", NewSenderRef = "MC000003" },
            new CopyBatchModel.AnimalRow { AnimalId = 7, SubmissionId = 11, SenderRef = "MC000001", NewSenderRef = "MC000004" },
            new CopyBatchModel.AnimalRow { AnimalId = 8, SubmissionId = 12, SenderRef = "MC000010", NewSenderRef = "MC000011" },
        ];

        var rows = sut.DisplayRows;

        Assert.Equal(2, rows.Count);
        Assert.Equal("MC000001", rows[0].SenderRef);
        Assert.Equal("MC000002 - MC000004", rows[0].DisplayNewSenderRef);
        Assert.Equal("MC000010", rows[1].SenderRef);
        Assert.Equal("MC000011", rows[1].DisplayNewSenderRef);
        Assert.Equal([0, 1, 2], rows[0].RowIndexes);
    }

    [Fact]
    public void OnPostPick_RedirectsToAddSubmissionSoUserCanChooseSenderOrMouseRange()
    {
        var sut = CreateSut();
        sut.SourceBatchId = 10;
        sut.Animals =
        [
            new CopyBatchModel.AnimalRow { AnimalId = 7, SenderRef = "S1", NewSenderRef = string.Empty },
            new CopyBatchModel.AnimalRow { AnimalId = 8, SenderRef = "S2", NewSenderRef = string.Empty }
        ];

        var result = sut.OnPostPick(1);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Submissions/AddSubmission", redirect.PageName);
        Assert.Equal("/Batches/CopyBatch?sourceBatchId=10", redirect.RouteValues!["returnPage"]);
        Assert.Equal(10, redirect.RouteValues["sourceBatchId"]);
        Assert.Equal(8, redirect.RouteValues["sourceAnimalId"]);
    }
}
