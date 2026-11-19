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

/// <summary>Unit tests for <see cref="PickListUserAreaModel"/>.</summary>
public class PickListUserAreaModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    public PickListUserAreaModelTests()
    {
        _session.SetupGet(s => s.UserID).Returns(9);
        _session.SetupGet(s => s.UserArea).Returns("2");
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)[new EditableLookup { ID = 18, TableName = "Pathologists" }]);
        _lookups.Setup(l => l.GetUserAreaDataAsync(18, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<LookupItem>)
            [
                new LookupItem { ID = 1, Name = "Zed", Active = false },
                new LookupItem { ID = 2, Name = "Amy", Active = true },
            ]);
    }

    private PickListUserAreaModel CreateSut()
    {
        var http = new DefaultHttpContext();
        return new PickListUserAreaModel(_session.Object, _lookups.Object)
        {
            TableId = 18,
            PageContext = new PageContext
            {
                HttpContext = http,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>()),
        };
    }

    [Fact]
    public async Task OnGetAsync_NoItemSelected_LoadsAreaItemsAndDefaultsToActive()
    {
        var sut = CreateSut();

        await sut.OnGetAsync();

        Assert.Equal("Pick List", sut.ViewData["Title"]);
        Assert.Equal("Pick List", sut.ViewData["PageTitle"]);
        Assert.Equal("Pathologists", sut.TableName);
        Assert.True(sut.Active);
        Assert.Equal(["Amy", "Zed"], sut.GetPagedEntries().Select(i => i.Name).ToArray());
        _lookups.Verify(l => l.GetUserAreaDataAsync(18, "2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_ItemSelected_PrefillsDescriptionAndActive()
    {
        var sut = CreateSut();
        sut.ItemId = 1;
        sut.UserArea = "4";

        await sut.OnGetAsync();

        Assert.Equal("Zed", sut.Description);
        Assert.False(sut.Active);
        _lookups.Verify(l => l.GetUserAreaDataAsync(18, "4", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedEntries_SortByActiveDescending_OrdersActiveFirst()
    {
        var sut = CreateSut();
        sut.SortColumn = "Active";
        sut.SortDesc = true;

        await sut.OnGetAsync();

        Assert.Equal([2, 1], sut.GetPagedEntries().Select(i => i.ID).ToArray());
    }

    [Fact]
    public async Task OnPostAsync_BlankDescription_ReturnsPageWithError()
    {
        var sut = CreateSut();
        sut.Description = "  ";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Pick List", sut.ViewData["Title"]);
        Assert.Equal("Pick List", sut.ViewData["PageTitle"]);
        Assert.Equal(["Enter a description."], sut.Errors);
        _lookups.Verify(l => l.CreateLookupItemAsync(It.IsAny<int>(), It.IsAny<LookupItem>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_NewItem_CreatesInSessionAreaAndRedirectsWithStatusMessage()
    {
        _lookups.Setup(l => l.CreateLookupItemAsync(18, It.IsAny<LookupItem>(), 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.Description = " Dr New ";

        var result = await sut.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/PickListUserArea", redirect.PageName);
        Assert.Equal("'Dr New' was added.", sut.TempData["StatusMessage"]);
        _lookups.Verify(l => l.CreateLookupItemAsync(18,
            It.Is<LookupItem>(i => i.Name == "Dr New" && i.Area == "2" && i.Active), 9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ExistingItem_UpdatesAndRedirectsWithStatusMessage()
    {
        _lookups.Setup(l => l.UpdateLookupItemAsync(18, It.IsAny<LookupItem>(), 9, It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();
        sut.ItemId = 1;
        sut.Description = "Renamed";
        sut.Active = false;

        var result = await sut.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("'Renamed' was updated.", sut.TempData["StatusMessage"]);
        _lookups.Verify(l => l.UpdateLookupItemAsync(18,
            It.Is<LookupItem>(i => i.ID == 1 && i.Name == "Renamed" && !i.Active), 9, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_SaveFails_ReturnsPageWithError()
    {
        _lookups.Setup(l => l.CreateLookupItemAsync(18, It.IsAny<LookupItem>(), 9, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();
        sut.Description = "Dr New";

        var result = await sut.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(["Failed to save the pick list item. Please try again."], sut.Errors);
    }
}
