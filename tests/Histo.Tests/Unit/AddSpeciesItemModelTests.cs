using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="AddSpeciesItemModel"/> — the dedicated "add a new species" page.
/// </summary>
public class AddSpeciesItemModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    private AddSpeciesItemModel CreateSut() =>
        new(_session.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
        };

    [Fact]
    public async Task OnPostAsync_MissingSpecies_SetsErrorAndDoesNotSave()
    {
        var sut = CreateSut();
        sut.Species = "";

        await sut.OnPostAsync();

        Assert.Equal("Enter a species.", sut.Errors["Species"]);
        _lookups.Verify(l => l.AddSpeciesItemAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_Valid_ComputesNextSpeciesIdAsMaxPlusOne()
    {
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[
                new SpeciesItem { SpeciesID = 3, Species = "Bovine" },
                new SpeciesItem { SpeciesID = 7, Species = "Ovine" },
            ]);
        _lookups.Setup(l => l.AddSpeciesItemAsync(8, "Porcine", "Pig", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Species = "Porcine";
        sut.CommonName = "Pig";

        var result = await sut.OnPostAsync();

        _lookups.Verify(l => l.AddSpeciesItemAsync(8, "Porcine", "Pig", It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_NoExistingSpecies_ComputesNextSpeciesIdAsOne()
    {
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[]);
        _lookups.Setup(l => l.AddSpeciesItemAsync(1, "Bovine", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Species = "Bovine";

        await sut.OnPostAsync();

        _lookups.Verify(l => l.AddSpeciesItemAsync(1, "Bovine", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_SaveFails_SetsSaveError()
    {
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[]);
        _lookups.Setup(l => l.AddSpeciesItemAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = CreateSut();
        sut.Species = "Bovine";

        await sut.OnPostAsync();

        Assert.Equal("Failed to save the species. Please try again.", sut.SaveError);
    }
}
