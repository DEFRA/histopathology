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
/// Unit tests for <see cref="AddLookupItemModel"/> — the dedicated "add a new pick-list item"
/// page (split out from the previous combined Add/Edit form per GDS "one thing per page").
/// </summary>
public class AddLookupItemModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public AddLookupItemModelTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.UserArea).Returns("PATH");
    }

    private AddLookupItemModel CreateSut() =>
        new(_session.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            },
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
        };

    private void SetupCodeKeyedTable()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 16, TableName = "Archive Location" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(16, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Code = "A", Name = "Store A", Active = true }]);
    }

    [Fact]
    public async Task OnPostAsync_CodeKeyedTable_MissingCode_SetsError()
    {
        SetupCodeKeyedTable();
        var sut = CreateSut();
        sut.TableId = 16;
        sut.Description = "New location";

        var result = await sut.OnPostAsync();

        Assert.Equal("Enter a code.", sut.Errors["Code"]);
        _lookups.Verify(l => l.CreateLookupItemAsync(It.IsAny<int>(), It.IsAny<LookupItem>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_CodeKeyedTable_DuplicateCode_SetsError()
    {
        SetupCodeKeyedTable();
        var sut = CreateSut();
        sut.TableId = 16;
        sut.Description = "Duplicate";
        sut.Code = "a"; // case-insensitive duplicate of existing "A"

        await sut.OnPostAsync();

        Assert.Equal("The code you have selected is already in use.", sut.Errors["Code"]);
    }

    [Fact]
    public async Task OnPostAsync_CodeKeyedTable_Valid_CreatesAndRedirectsToLookupItems()
    {
        SetupCodeKeyedTable();
        _lookups.Setup(l => l.CreateLookupItemAsync(16, It.IsAny<LookupItem>(), 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.TableId = 16;
        sut.Description = "New location";
        sut.Code = "B";

        var result = await sut.OnPostAsync();

        _lookups.Verify(l => l.CreateLookupItemAsync(16,
            It.Is<LookupItem>(i => i.Code == "B" && i.Name == "New location" && i.Area == null), 7, It.IsAny<CancellationToken>()), Times.Once);
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal("/Admin/LookupItems", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_AreaScopedTable_NoCodeRequired_PassesAreaNotCode()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 18, TableName = "Pathologists" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        _lookups.Setup(l => l.GetUserAreasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 3, Name = "Pathology" }]);
        _lookups.Setup(l => l.CreateLookupItemAsync(18, It.IsAny<LookupItem>(), 7, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.TableId = 18;
        sut.Description = "Dr Smith";
        sut.Area = "3";

        await sut.OnPostAsync();

        Assert.Empty(sut.Errors);
        _lookups.Verify(l => l.CreateLookupItemAsync(18,
            It.Is<LookupItem>(i => i.Area == "3" && i.Code == null && i.Name == "Dr Smith"), 7, It.IsAny<CancellationToken>()), Times.Once);
    }
}
