using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.Archive;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="ArchiveTissuesModel"/>.</summary>
public class ArchiveTissuesModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ISubmissionService> _submissions = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();

    public ArchiveTissuesModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupProperty(s => s.IsViewSubmissionMode);
        _session.SetupProperty(s => s.ReturnPage, string.Empty);
        _session.Setup(s => s.UserID).Returns(99);
        _lookups.Setup(l => l.GetLookupDataAsync(16, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { Code = "A", Name = "Archive A" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(9, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[]);
    }

    private ArchiveTissuesModel CreateSut() =>
        new(_session.Object, _submissions.Object, _batches.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnGetAsync_WithBatchId_JoinsAnimalAndSubmission()
    {
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42 });
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 5, OwnerID = 11, Owner = TissueOwner.Submission, TissueCode = "LN" }]);
        _submissions.Setup(s => s.GetSubmissionsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<BatchSubmission>)[new BatchSubmission { ID = 11, AnimalID = 7 }]);
        _submissions.Setup(s => s.GetAnimalsByBatchAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Animal>)[new Animal { ID = 7, SenderRef = "S1", HistologyRef = "24/001" }]);
        var sut = CreateSut();
        sut.BatchId = 42;

        await sut.OnGetAsync();

        Assert.Equal(42, _session.Object.BatchID);
        Assert.Single(sut.Rows);
        Assert.Equal("S1", sut.Rows[0].SenderRef);
        Assert.Equal("/Batches/BatchesForArchiving", _session.Object.ReturnPage);
    }

    [Fact]
    public async Task OnPostUpdateAsync_NoSelection_ReturnsError()
    {
        _session.Object.BatchID = 42;
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[]);
        var sut = CreateSut();

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select at least one tissue to update.", sut.Error);
        _submissions.Verify(s => s.UpdateTissueAsync(It.IsAny<Tissue>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_SingleSelection_OverwritesAllFields()
    {
        _session.Object.BatchID = 42;
        var existing = new Tissue { ID = 5, OwnerID = 11, Owner = TissueOwner.Submission, TissueCode = "LN", ArchiveComment = "old" };
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[existing]);
        var sut = CreateSut();
        sut.SelectedIds = [5];
        sut.ArchiveLocationCode = "NEW";
        sut.Comment = null;

        await sut.OnPostUpdateAsync();

        _submissions.Verify(s => s.UpdateTissueAsync(
            It.Is<Tissue>(t => t.ID == 5 && t.ArchiveLocation == "NEW" && t.ArchiveComment == null), 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostUpdateAsync_ArchivedDateInFuture_ReturnsError()
    {
        _session.Object.BatchID = 42;
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42 });
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 5 }]);
        var sut = CreateSut();
        sut.SelectedIds = [5];
        sut.ArchivedDate = DateTime.Today.AddDays(1);

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Archive date must be today or earlier.", sut.Error);
        _submissions.Verify(s => s.UpdateTissueAsync(It.IsAny<Tissue>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostUpdateAsync_ViewMode_DoesNotUpdate()
    {
        _session.Object.BatchID = 42;
        _session.Object.IsViewSubmissionMode = true;
        _submissions.Setup(s => s.GetBatchSubmissionTissuesAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Tissue>)[new Tissue { ID = 5 }]);
        var sut = CreateSut();
        sut.SelectedIds = [5];
        sut.ArchiveLocationCode = "X";

        var result = await sut.OnPostUpdateAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _submissions.Verify(s => s.UpdateTissueAsync(It.IsAny<Tissue>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
