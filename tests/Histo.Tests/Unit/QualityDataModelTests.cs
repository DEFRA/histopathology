using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Histology.Interfaces;
using Histo.Histology.Models;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Pages.QC;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="QualityDataModel"/>.</summary>
public class QualityDataModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<IBlockTestService> _tests = new();
    private readonly Mock<IBatchService> _batches = new();
    private readonly Mock<ILookupService> _lookups = new();
    private readonly Mock<IUserService> _users = new();

    public QualityDataModelTests()
    {
        _session.SetupProperty(s => s.BatchID);
        _session.Object.BatchID = 42;
        _users.Setup(u => u.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<User>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
    }

    private QualityDataModel CreateSut() =>
        new(_session.Object, _tests.Object, _batches.Object, _lookups.Object, _users.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
        };

    [Fact]
    public async Task OnGetAsync_ResolvesSpeciesNameFromLookup_NotRawId()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(new Batch { ID = 42, Species = "3" });
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 3, Name = "Bovine" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal("Bovine", sut.SpeciesName);
    }

    [Fact]
    public async Task OnGetAsync_ResolvesProjectAndContact_WithIncludeInactiveTrue()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Batch { ID = 42, ProjectContractCode = "7", ContactName = "9" });
        _lookups.Setup(l => l.GetSpeciesLookupAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetLookupDataAsync(19, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 7, Name = "Deactivated Project" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 9, Name = "Dr Retired" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal("Deactivated Project", sut.ProjectName);
        Assert.Equal("Dr Retired", sut.PathologistName);
        _lookups.Verify(l => l.GetLookupDataAsync(19, true, It.IsAny<CancellationToken>()), Times.Once);
        _lookups.Verify(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_TestNamesFilter_UsesResolvedDisplayName_NotRawCode()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[
            new BlockTest { ID = 1, TestType = "Histology", Code = "2", HistologyRef = "24/001", BlockRef = "01" },
        ]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 2, Code = "2", Name = "H&E" }]);
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Contains("H&E", sut.TestNames);
        Assert.DoesNotContain("2", sut.TestNames);
    }

    [Fact]
    public async Task OnGetAsync_FilterTestByResolvedName_FiltersGridRows()
    {
        _tests.Setup(t => t.GetByBatchAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<BlockTest>)[
            new BlockTest { ID = 1, TestType = "Histology", Code = "2", HistologyRef = "24/001", BlockRef = "01" },
            new BlockTest { ID = 2, TestType = "Histology", Code = "3", HistologyRef = "24/002", BlockRef = "02" },
        ]);
        _batches.Setup(b => b.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Batch?)null);
        _lookups.Setup(l => l.GetHistologyTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { ID = 2, Code = "2", Name = "H&E" },
                new LookupItem { ID = 3, Code = "3", Name = "Special Stain" },
            ]);
        var sut = CreateSut();
        sut.FilterTest = "H&E";

        await sut.OnGetAsync();

        Assert.Single(sut.Tests);
        Assert.Equal(1, sut.Tests[0].ID);
    }
}
