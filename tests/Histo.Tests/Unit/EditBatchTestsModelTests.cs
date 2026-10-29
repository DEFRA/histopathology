using Histo.Administration.Interfaces;
using Histo.Administration.Models;
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

/// <summary>Unit tests for <see cref="EditBatchTestsModel"/>.</summary>
public class EditBatchTestsModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();

    public EditBatchTestsModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.SetupGet(s => s.UserID).Returns(9);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)
            [
                new LookupItem { ID = 1, Code = HistologyCode.HAndE, Name = "H&E" },
                new LookupItem { ID = 2, Code = HistologyCode.IhcPrp, Name = "IHC PrP" },
                new LookupItem { ID = 3, Code = HistologyCode.HeBse, Name = "H&E BSE" },
                new LookupItem { ID = 4, Code = HistologyCode.IhcOther, Name = "IHC Other" },
            ]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Code = "X", Name = "Named" }, new LookupItem { ID = 2, Code = "Y", Name = " " }]);
        _batches.Setup(b => b.GetBatchTestSelectionsAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchTestSelections
            {
                Histology = [new BatchTestSelectionRow { Code = HistologyCode.IhcPrp }],
                Antibodies = [new BatchTestSelectionRow { Code = "X" }],
                Stains = [new BatchTestSelectionRow { Code = "S1" }],
            });
    }

    private EditBatchTestsModel CreateSut() =>
        new(_session.Object, _batches.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    private void ExistingBatch(int batchType) =>
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 5, BatchType = batchType });

    [Fact]
    public async Task OnGetAsync_NoBatchInSession_RedirectsToIndexAndSetsTitles()
    {
        _session.Object.BatchID = 0;
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("Edit test types", sut.ViewData["Title"]);
        Assert.Equal("Edit test types", sut.ViewData["PageTitle"]);
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
    public async Task OnGetAsync_TseBatch_PreselectsSavedTestsAndHidesIhcOtherOption()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchTypeConstants.Tse);
        var sut = CreateSut();

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal([HistologyCode.IhcPrp], sut.SelectedHistologyCodes);
        Assert.Equal(["X"], sut.SelectedAntibodyCodes);
        Assert.Equal(["S1"], sut.SelectedStainCodes);
        Assert.DoesNotContain(sut.HistologyOptions, o => o.Code == HistologyCode.IhcOther);
        Assert.Equal(3, sut.HistologyOptions.Count);
        Assert.Single(sut.AntibodyOptions);
        Assert.Single(sut.StainOptions);
        Assert.True(sut.ShowAntibodies);
        Assert.False(sut.ShowStains);
    }

    [Fact]
    public async Task OnGetAsync_NonTseBatch_HidesIhcPrpAndHeBseOptionsAndUsesNonTseAntibodies()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchTypeConstants.NonTse);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal([HistologyCode.HAndE, HistologyCode.IhcOther], sut.HistologyOptions.Select(o => o.Code).ToArray());
        _lookups.Verify(l => l.GetLookupDataAsync(5, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_BatchNotFound_RedirectsToIndexAndSetsTitles()
    {
        _session.Object.BatchID = 5;
        _batches.Setup(b => b.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
        Assert.Equal("Edit test types", sut.ViewData["Title"]);
        Assert.Equal("Edit test types", sut.ViewData["PageTitle"]);
    }

    [Fact]
    public async Task OnPostAsync_NoHistologySelected_ReturnsPageWithError()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchTypeConstants.Tse);
        var sut = CreateSut();

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Select at least one histology type.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_ArchiveCombinedWithOtherType_ReturnsPageWithError()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchTypeConstants.Tse);
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.Archive, HistologyCode.HAndE];

        await sut.OnPostAsync();

        Assert.Equal("Archive cannot be combined with other histology types.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_SpecialStainWithoutStain_ReturnsPageWithError()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchTypeConstants.Tse);
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.SpecialStain];

        await sut.OnPostAsync();

        Assert.StartsWith("Special Stain is selected", sut.SaveError);
        Assert.True(sut.ShowStains);
    }

    [Fact]
    public async Task OnPostAsync_IhcWithoutAntibody_ReturnsPageWithError()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchTypeConstants.Tse);
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.IhcPrp];

        await sut.OnPostAsync();

        Assert.StartsWith("IHC is selected", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_SaveFails_ReturnsPageWithError()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchTypeConstants.Tse);
        _batches.Setup(b => b.SaveBatchTestSelectionsAsync(5, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(), 9, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to save test types. Please try again.", sut.SaveError);
    }

    [Fact]
    public async Task OnPostAsync_ValidSelection_SavesWithoutUnrelatedAntibodiesOrStainsAndRedirects()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchTypeConstants.Tse);
        _batches.Setup(b => b.SaveBatchTestSelectionsAsync(5, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(), 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.HAndE];
        sut.SelectedAntibodyCodes = ["X"];
        sut.SelectedStainCodes = ["S1"];

        var result = await sut.OnPostAsync();

        Assert.Equal("/Batches/BatchDetails", Assert.IsType<RedirectToPageResult>(result).PageName);
        _batches.Verify(b => b.SaveBatchTestSelectionsAsync(5,
            It.Is<IReadOnlyList<string>>(h => h.SequenceEqual(new[] { HistologyCode.HAndE })),
            It.Is<IReadOnlyList<string>>(a => a.Count == 0),
            It.Is<IReadOnlyList<string>>(s => s.Count == 0),
            9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_IhcAndSpecialStainSelected_KeepsAntibodiesAndStains()
    {
        _session.Object.BatchID = 5;
        ExistingBatch(BatchTypeConstants.Tse);
        _batches.Setup(b => b.SaveBatchTestSelectionsAsync(5, It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<IReadOnlyList<string>>(), 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.SelectedHistologyCodes = [HistologyCode.IhcPrp, HistologyCode.SpecialStain];
        sut.SelectedAntibodyCodes = ["X"];
        sut.SelectedStainCodes = ["S1"];

        await sut.OnPostAsync();

        _batches.Verify(b => b.SaveBatchTestSelectionsAsync(5,
            It.IsAny<IReadOnlyList<string>>(),
            It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(AntibodyCodes)),
            It.Is<IReadOnlyList<string>>(s => s.SequenceEqual(StainCodes)),
            9, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static readonly string[] AntibodyCodes = ["X"];
    private static readonly string[] StainCodes = ["S1"];
}
