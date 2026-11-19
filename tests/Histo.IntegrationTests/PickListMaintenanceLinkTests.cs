using System.Text.RegularExpressions;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Histo.IntegrationTests;

[Collection(HostCollection.Name)]
public class PickListMaintenanceLinkTests(HistoWebFactory factory)
{
    [Fact]
    public async Task EditItemsLinks_CarryTheTableId_AndSpeciesUsesItsOwnPage()
    {
        var lookups = Mock.Get(factory.Services.GetRequiredService<ILookupService>());
        lookups.Setup(l => l.ListEditableLookupsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(
               [
                   new EditableLookup { ID = 10, TableName = "Fixatives" },
                   new EditableLookup { ID = 19, TableName = "Projects" },
                   new EditableLookup { ID = 3, TableName = "Species" },
               ]);

        using var client = factory.CreateClientAs(TestUser.Maintenance);
        var html = await client.GetStringAsync("/Admin/PickListMaintenance", TestContext.Current.CancellationToken);

        var hrefs = Regex.Matches(html, "href=\"(/Admin/(?:LookupItems|SpeciesItems)[^\"]*)\"")
                         .Select(m => m.Groups[1].Value)
                         .ToList();

        Assert.Contains("/Admin/LookupItems/10", hrefs);
        Assert.Contains("/Admin/LookupItems/19", hrefs);
        Assert.Contains("/Admin/SpeciesItems", hrefs);
        Assert.DoesNotContain("/Admin/LookupItems", hrefs);
    }
}
