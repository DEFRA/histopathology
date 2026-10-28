using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Add/Edit pick-list item pages: initial load, save failure and retired-area handling.</summary>
public class LookupItemFormCoverageTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public LookupItemFormCoverageTests()
    {
        _session.Setup(s => s.UserID).Returns(7);
        _session.Setup(s => s.UserArea).Returns("PATH");
    }

    private static PageContext Ctx() => new()
    {
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        HttpContext = new DefaultHttpContext(),
    };

    private static TempDataDictionary Temp() => new(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

    private AddLookupItemModel Add() => new(_session.Object, _lookups.Object) { PageContext = Ctx(), TempData = Temp() };
    private EditLookupItemModel Edit() => new(_session.Object, _lookups.Object) { PageContext = Ctx(), TempData = Temp() };

    private void SetupTable(int id, string name, params LookupItem[] items)
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = id, TableName = name }]);
        _lookups.Setup(l => l.GetLookupDataAsync(id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)items);
        _lookups.Setup(l => l.GetUserAreasAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Name = "Active area" }]);
    }

    [Fact]
    public async Task AddOnGetAsync_SetsTitleDefaultsActiveAndLoadsTable()
    {
        SetupTable(16, "Archive Location", new LookupItem { ID = 1, Code = "A", Name = "Store A", Active = true });
        var sut = Add();
        sut.TableId = 16;
        sut.Active = false;

        await sut.OnGetAsync();

        Assert.Equal("Add pick list item", sut.ViewData["Title"]);
        Assert.Equal("Add pick list item", sut.ViewData["PageTitle"]);
        Assert.True(sut.Active);
        Assert.Equal("Archive Location", sut.TableName);
        Assert.True(sut.TableHasCodes);
    }

    [Fact]
    public async Task AddOnPostAsync_CreateFails_ShowsSaveErrorWithoutRedirect()
    {
        SetupTable(16, "Archive Location");
        _lookups.Setup(l => l.CreateLookupItemAsync(16, It.IsAny<LookupItem>(), 7, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = Add();
        sut.TableId = 16;
        sut.Code = "B";
        sut.Description = "Store B";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Failed to save the pick list item. Please try again.", sut.SaveError);
        Assert.Empty(sut.Errors);
    }

    [Fact]
    public async Task EditOnPostAsync_UpdateFails_ShowsSaveErrorWithoutRedirect()
    {
        SetupTable(16, "Archive Location", new LookupItem { ID = 1, Code = "A", Name = "Store A", Active = true });
        _lookups.Setup(l => l.UpdateLookupItemAsync(16, It.IsAny<LookupItem>(), 7, "A", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = Edit();
        sut.TableId = 16;
        sut.OriginalCode = "A";
        sut.Code = "A";
        sut.Description = "Store A2";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.NotNull(sut.SaveError);
    }

    [Fact]
    public async Task EditOnGetAsync_ItemInRetiredArea_AddsRetiredAreaToDropdown()
    {
        SetupTable(18, "Contacts", new LookupItem { ID = 5, Name = "Dr X", Active = true, Area = "4" });
        _lookups.Setup(l => l.GetUserAreasAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)[new LookupItem { ID = 1, Name = "Active area" }, new LookupItem { ID = 4, Name = "Retired area" }]);
        var sut = Edit();
        sut.TableId = 18;
        sut.ItemId = 5;

        var result = await sut.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(["Active area", "Retired area"], sut.UserAreas.Select(a => a.Name));
    }

    [Fact]
    public async Task EditOnGetAsync_ItemInActiveArea_DoesNotLoadInactiveAreas()
    {
        SetupTable(18, "Contacts", new LookupItem { ID = 5, Name = "Dr X", Active = true, Area = "1" });
        var sut = Edit();
        sut.TableId = 18;
        sut.ItemId = 5;

        await sut.OnGetAsync();

        Assert.Single(sut.UserAreas);
        _lookups.Verify(l => l.GetUserAreasAsync(true, It.IsAny<CancellationToken>()), Times.Never);
    }
}
