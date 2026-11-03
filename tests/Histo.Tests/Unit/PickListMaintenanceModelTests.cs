using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Pages.Admin;
using Histo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Unit tests for <see cref="PickListMaintenanceModel"/>.</summary>
public class PickListMaintenanceModelTests
{
    private readonly Mock<ISessionService> _session = new();
    private readonly Mock<ILookupService> _lookups = new();

    private PickListMaintenanceModel CreateSut() =>
        new(_session.Object, _lookups.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };

    private async Task<PickListMaintenanceModel> LoadAsync(bool sortDesc)
    {
        _lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EditableLookup>)
            [
                new EditableLookup { ID = 2, TableName = "Species" },
                new EditableLookup { ID = 3, TableName = "User Area" },
                new EditableLookup { ID = 1, TableName = "Archive Location" },
            ]);
        var sut = CreateSut();
        sut.SortDesc = sortDesc;

        await sut.OnGetAsync();
        return sut;
    }

    [Fact]
    public async Task OnGetAsync_ExcludesUserAreaTableAndSortsByNameAscending()
    {
        var sut = await LoadAsync(sortDesc: false);

        Assert.Equal("Pick List Maintenance", sut.ViewData["Title"]);
        Assert.Equal("Pick List Maintenance", sut.ViewData["PageTitle"]);
        Assert.Equal(2, sut.TotalCount);
        Assert.Equal(["Archive Location", "Species"], sut.GetPagedEntries().Select(l => l.TableName).ToArray());
    }

    [Fact]
    public async Task GetPagedEntries_SortDescending_ReversesOrder()
    {
        var sut = await LoadAsync(sortDesc: true);

        Assert.Equal(["Species", "Archive Location"], sut.GetPagedEntries().Select(l => l.TableName).ToArray());
    }
}
