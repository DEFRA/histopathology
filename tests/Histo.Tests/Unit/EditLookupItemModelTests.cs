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
/// Unit tests for <see cref="EditLookupItemModel"/> — the dedicated "edit an existing pick-list
/// item" page (split out from the previous combined Add/Edit form per GDS "one thing per page").
/// </summary>
public class EditLookupItemModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public EditLookupItemModelTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
    }

    private EditLookupItemModel CreateSut() =>
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
    public async Task OnGetAsync_CodeKeyedTable_LoadsExistingRowByCode()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 16, TableName = "Archive Location" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(16, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Code = "A", Name = "Store A", Active = true }]);
        var sut = CreateSut();
        sut.TableId = 16;
        sut.ItemCode = "A";

        await sut.OnGetAsync();

        Assert.Equal("Store A", sut.Description);
        Assert.Equal("A", sut.OriginalCode);
    }

    [Fact]
    public async Task OnGetAsync_ItemNotFound_RedirectsToLookupItems()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 16, TableName = "Archive Location" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(16, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[]);
        var sut = CreateSut();
        sut.TableId = 16;
        sut.ItemCode = "MISSING";

        var result = await sut.OnGetAsync();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal("/Admin/LookupItems", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_CodeKeyedTable_ChangingCodeToDuplicate_SetsError()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 16, TableName = "Archive Location" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(16, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[
                new LookupItem { ID = 1, Code = "A", Name = "Store A", Active = true },
                new LookupItem { ID = 2, Code = "B", Name = "Store B", Active = true },
            ]);
        var sut = CreateSut();
        sut.TableId = 16;
        sut.OriginalCode = "A";
        sut.Code = "B"; // now collides with the other existing row
        sut.Description = "Store A renamed";

        await sut.OnPostAsync();

        Assert.Equal("The code you have selected is already in use.", sut.Errors["Code"]);
    }

    [Fact]
    public async Task OnPostAsync_CodeKeyedTable_SameCode_SavesWithoutDuplicateError()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 16, TableName = "Archive Location" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(16, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Code = "A", Name = "Store A", Active = true }]);
        _lookups.Setup(l => l.UpdateLookupItemAsync(16, It.IsAny<LookupItem>(), 7, "A", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.TableId = 16;
        sut.OriginalCode = "A";
        sut.Code = "A";
        sut.Description = "Store A renamed";

        var result = await sut.OnPostAsync();

        Assert.Empty(sut.Errors);
        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToPageResult>(result);
        Assert.Equal("/Admin/LookupItems", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_IdKeyedTable_UpdatesById()
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 18, TableName = "Pathologists" }]);
        _lookups.Setup(l => l.GetLookupDataAsync(18, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 9, Name = "Dr Smith", Active = true }]);
        _lookups.Setup(l => l.UpdateLookupItemAsync(18, It.IsAny<LookupItem>(), 7, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.TableId = 18;
        sut.ItemId = 9;
        sut.Description = "Dr Smith Jr";

        await sut.OnPostAsync();

        _lookups.Verify(l => l.UpdateLookupItemAsync(18,
            It.Is<LookupItem>(i => i.ID == 9 && i.Name == "Dr Smith Jr"), 7, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
