using Histo.Core.Domain;
using Histo.Web.Pages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Histo.Tests.Unit;

public class BlockTestSelectionRulesTests
{
    private static BlockTestAdditions Compute(string[] h, string[] a, string[] s, string[] addH, string[] addA, string[] addS) =>
        BlockTestSelectionRules.ComputeAdditions(h, a, s, addH, addA, addS);

    [Fact]
    public void NonExclusiveHistology_IsAddedToABlockThatLacksIt()
    {
        var result = Compute(["2"], [], [], ["3"], [], ["S1"]);

        Assert.Equal(new[] { "3" }, result.Histology);
        Assert.Equal(new[] { "S1" }, result.Stains); // Special Stain (3) is now present on the block
    }

    [Theory]
    [InlineData("1")] // EO
    [InlineData("7")] // Archive
    public void BlockWithEoOrArchive_GetsNothing(string exclusive)
    {
        var result = Compute([exclusive], [], [], ["2", "3"], ["A1"], ["S1"]);

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void AntibodiesAndStains_NeedTheirTriggerHistology()
    {
        Assert.True(Compute(["2"], [], [], [], ["A1"], ["S1"]).IsEmpty);
        Assert.Equal(new[] { "A2" }, Compute(["4"], ["A1"], [], [], ["A1", "A2"], []).Antibodies);
    }

    [Fact]
    public void ExistingCodes_AreNeverRepeated_AndComparisonIgnoresCase()
    {
        Assert.True(Compute(["2"], [], [], ["2"], [], []).IsEmpty);
        Assert.True(Compute(["4"], ["a1"], [], [], ["A1"], []).IsEmpty);
    }

    [Fact]
    public void BlockWithNoHistology_CanTakeAnExclusiveCodeOnlyWhenNothingElseIsAdded()
    {
        Assert.Equal(new[] { "1" }, Compute([], [], [], ["1"], [], []).Histology);
        Assert.Equal(new[] { "2" }, Compute([], [], [], ["1", "2"], [], []).Histology);
    }
}

public class DevAuthBypassTests
{
    private static HttpContext ContextFor(string environment, bool? flag)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(flag is null ? [] : new Dictionary<string, string?> { ["DevAuthBypass"] = flag.Value.ToString() })
            .Build();
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(environment);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .AddSingleton(env.Object)
            .BuildServiceProvider();
        return new DefaultHttpContext { RequestServices = services };
    }

    [Fact]
    public void FlagOnInDevelopment_IsEnabled() =>
        Assert.True(HistoPageModel.IsDevAuthBypassEnabled(ContextFor("Development", true)));

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Test")]
    public void FlagOnOutsideDevelopment_IsIgnored(string environment) =>
        Assert.False(HistoPageModel.IsDevAuthBypassEnabled(ContextFor(environment, true)));

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public void FlagOffOrMissing_IsDisabledEvenInDevelopment(bool? flag) =>
        Assert.False(HistoPageModel.IsDevAuthBypassEnabled(ContextFor("Development", flag)));
}
