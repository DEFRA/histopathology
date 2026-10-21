using Histo.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Histo.Tests.Unit;

public class AppLoggerTests
{
    private sealed class CapturingLogger(LogLevel minimum = LogLevel.Trace) : ILogger
    {
        public List<(LogLevel Level, Exception? Exception, string Message, Dictionary<string, object?> State)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minimum;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var properties = ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(p => p.Key, p => p.Value);
            Entries.Add((logLevel, exception, formatter(state, exception), properties));
        }
    }

    [Fact]
    public void LogInfo_FillsPlaceholdersAndKeepsNamedPropertiesAndOriginalFormat()
    {
        var inner = new CapturingLogger();

        new AppLogger(inner).LogInfo("Batch {BatchId} saved by {UserId}", 5, "jo");

        var entry = Assert.Single(inner.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("Batch 5 saved by jo", entry.Message);
        Assert.Equal(5, entry.State["BatchId"]);
        Assert.Equal("jo", entry.State["UserId"]);
        Assert.Equal("Batch {BatchId} saved by {UserId}", entry.State["{OriginalFormat}"]);
    }

    [Fact]
    public void LogWarning_WritesAtWarningLevel()
    {
        var inner = new CapturingLogger();

        new AppLogger(inner).LogWarning("Careful");

        Assert.Equal(LogLevel.Warning, Assert.Single(inner.Entries).Level);
    }

    [Fact]
    public void LogError_PassesTheExceptionThrough()
    {
        var inner = new CapturingLogger();
        var boom = new InvalidOperationException("boom");

        new AppLogger(inner).LogError("Failed for {Id}", boom, 7);

        var entry = Assert.Single(inner.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(boom, entry.Exception);
        Assert.Equal("Failed for 7", entry.Message);
    }

    [Fact]
    public void Log_StripsDestructuringPrefixesAndAppliesFormatSpecifiers()
    {
        var inner = new CapturingLogger();

        new AppLogger(inner).LogInfo("Total {@Order} is {Amount:000}", "o1", 7);

        var entry = Assert.Single(inner.Entries);
        Assert.Equal("Total o1 is 007", entry.Message);
        Assert.Equal("o1", entry.State["Order"]);
    }

    [Fact]
    public void Log_EscapedBracesStayLiteral_AndAreNotTreatedAsPlaceholders()
    {
        var inner = new CapturingLogger();

        new AppLogger(inner).LogInfo("Literal {{braces}} and {Value}", 3);

        var entry = Assert.Single(inner.Entries);
        Assert.Equal("Literal {braces} and 3", entry.Message);
        Assert.DoesNotContain("braces", entry.State.Keys);
    }

    [Fact]
    public void Log_FewerArgumentsThanPlaceholders_FallsBackToTheTemplateInsteadOfThrowing()
    {
        var inner = new CapturingLogger();

        new AppLogger(inner).LogInfo("Needs {A} and {B}", 1);

        var entry = Assert.Single(inner.Entries);
        Assert.Equal("Needs {A} and {B}", entry.Message);
        Assert.Null(entry.State["B"]);
    }

    [Fact]
    public void Log_LevelDisabled_WritesNothing()
    {
        var inner = new CapturingLogger(LogLevel.Error);

        new AppLogger(inner).LogInfo("quiet");

        Assert.Empty(inner.Entries);
    }
}
