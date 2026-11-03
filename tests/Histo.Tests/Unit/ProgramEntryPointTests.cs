namespace Histo.Tests.Unit;

public class ProgramEntryPointTests
{
    private sealed class Probe : Program;

    [Fact]
    public void ProgramCanBeSubclassedForTestHosts() => Assert.NotNull(new Probe());
}
