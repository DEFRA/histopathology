using Histo.Core.Domain;

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
