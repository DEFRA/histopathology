using Histo.Administration.Interfaces;
using Histo.Core.Domain;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Batches;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="CopyBatchModel"/> — the Copy Submission workflow.
///
/// Regression coverage for the fix that restores parity with legacy
/// <c>clsBatch.vb::CopyBatch()</c>, which always copies the source batch's
/// Histology/Antibody/Special Stain test-type selections onto the new batch
/// alongside the header. Prior to the fix, <c>OnPostAsync</c> only copied the
/// batch header, silently dropping the test-type template on every copy.
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
    }

    private CopyBatchModel CreateSut() =>
        new(_session.Object, _batches.Object, _submissions.Object, _lookups.Object)
        {
            PageContext = new PageContext { ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
        };

    private static Batch MakeSourceBatch() => new()
    {
        ID = 10,
        Status = BatchStatus.Submitted,
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
    public async Task OnPostAsync_CopiesBatchTestSelections_OntoNewBatch()
    {
        var sourceBatch = MakeSourceBatch();
        var sourceSelections = new BatchTestSelections
        {
            Histology = [new BatchTestSelectionRow { ID = 1, BatchID = 10, Code = "2" }],
            Antibodies = [new BatchTestSelectionRow { ID = 2, BatchID = 10, Code = "AB1" }],
            Stains = [new BatchTestSelectionRow { ID = 3, BatchID = 10, Code = "ST1" }],
        };

        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(sourceBatch);
        _batches.Setup(b => b.CopyBatchHeaderAsync(It.IsAny<Batch>(), 42, It.IsAny<CancellationToken>())).ReturnsAsync(99);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(sourceSelections);
        _batches.Setup(b => b.SaveBatchTestSelectionsAsync(
                99,
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                42,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetBlockAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);

        var sut = CreateSut();
        sut.SourceBatchId = 10;

        var result = await sut.OnPostAsync();

        _batches.Verify(b => b.GetBatchTestSelectionsAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _batches.Verify(b => b.SaveBatchTestSelectionsAsync(
            99,
            It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "2"),
            It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "AB1"),
            It.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "ST1"),
            42,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_SourceBatchNotFound_DoesNotAttemptCopy()
    {
        _batches.Setup(b => b.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);

        var sut = CreateSut();
        sut.SourceBatchId = 10;

        await sut.OnPostAsync();

        Assert.Equal("The submission to copy could not be found.", sut.Error);
        _batches.Verify(b => b.CopyBatchHeaderAsync(It.IsAny<Batch>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _batches.Verify(b => b.GetBatchTestSelectionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
