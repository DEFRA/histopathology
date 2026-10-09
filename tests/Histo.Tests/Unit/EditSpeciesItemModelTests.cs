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
/// Unit tests for <see cref="EditSpeciesItemModel"/> — the dedicated "edit an existing species" page.
/// </summary>
public class EditSpeciesItemModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public EditSpeciesItemModelTests() => _session.Setup(s => s.UserID).Returns(7);

    private EditSpeciesItemModel CreateSut() =>
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
    public async Task OnGetAsync_ExistingSpecies_LoadsFields()
    {
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[new SpeciesItem { SpeciesID = 3, Species = "Bovine", CommonName = "Cattle" }]);
        var sut = CreateSut();
        sut.SpeciesID = 3;

        await sut.OnGetAsync();

        Assert.Equal("Bovine", sut.Species);
        Assert.Equal("Cattle", sut.CommonName);
    }

    [Fact]
    public async Task OnGetAsync_SpeciesNotFound_RedirectsToSpeciesItems()
    {
        _lookups.Setup(l => l.GetSpeciesItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SpeciesItem>)[]);
        var sut = CreateSut();
        sut.SpeciesID = 99;

        var result = await sut.OnGetAsync();

        Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_MissingSpecies_SetsErrorAndDoesNotSave()
    {
        var sut = CreateSut();
        sut.SpeciesID = 3;
        sut.Species = "";

        await sut.OnPostAsync();

        Assert.Equal("Enter a species.", sut.Errors["Species"]);
        _lookups.Verify(l => l.UpdateSpeciesItemAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_Valid_UpdatesAndRedirects()
    {
        _lookups.Setup(l => l.UpdateSpeciesItemAsync(3, "Bovine", "Cattle", 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.SpeciesID = 3;
        sut.Species = "Bovine";
        sut.CommonName = "Cattle";

        var result = await sut.OnPostAsync();

        _lookups.Verify(l => l.UpdateSpeciesItemAsync(3, "Bovine", "Cattle", 7, It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_SaveFails_SetsSaveError()
    {
        _lookups.Setup(l => l.UpdateSpeciesItemAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = CreateSut();
        sut.SpeciesID = 3;
        sut.Species = "Bovine";

        await sut.OnPostAsync();

        Assert.Equal("Failed to save the species. Please try again.", sut.SaveError);
    }
}
