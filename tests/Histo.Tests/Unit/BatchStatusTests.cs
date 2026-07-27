using Histo.Core.Domain;

namespace Histo.Tests.Unit;

/// <summary>
/// Baseline unit tests for <see cref="BatchStatus"/> constants.
///
/// These tests document the exact string values used by legacy stored procedures
/// and DataRow filter expressions throughout the Histopathology application.
///
/// Legacy source: HistopathologyLib/clsBatch.vb, string constant declarations.
///
/// Why test constants? These string values flow directly into SQL stored procedure
/// parameters. A silent rename or type change (string → enum) during migration
/// would break every SP call site. The tests act as a trip-wire.
/// </summary>
public class BatchStatusTests
{
    [Fact]
    public void Submitted_HasExpectedValue()
        => Assert.Equal("1", BatchStatus.Submitted);

    [Fact]
    public void Received_HasExpectedValue()
        => Assert.Equal("2", BatchStatus.Received);

    [Fact]
    public void Rejected_HasExpectedValue()
        => Assert.Equal("3", BatchStatus.Rejected);

    [Fact]
    public void Completed_HasExpectedValue()
        => Assert.Equal("4", BatchStatus.Completed);

    [Fact]
    public void OnHold_HasExpectedValue()
        => Assert.Equal("5", BatchStatus.OnHold);

    [Fact]
    public void InProgress_HasExpectedValue()
        => Assert.Equal("6", BatchStatus.InProgress);

    [Fact]
    public void AllStatusValues_AreUnique()
    {
        var values = new[]
        {
            BatchStatus.Submitted,
            BatchStatus.Received,
            BatchStatus.Rejected,
            BatchStatus.Completed,
            BatchStatus.OnHold,
            BatchStatus.InProgress,
        };

        Assert.Equal(values.Length, values.Distinct().Count());
    }

    [Fact]
    public void AllStatusValues_AreNumericStrings()
    {
        var values = new[]
        {
            BatchStatus.Submitted,
            BatchStatus.Received,
            BatchStatus.Rejected,
            BatchStatus.Completed,
            BatchStatus.OnHold,
            BatchStatus.InProgress,
        };

        foreach (var value in values)
        {
            Assert.True(int.TryParse(value, out _),
                $"BatchStatus value '{value}' is not a numeric string — stored procedures expect numeric status codes.");
        }
    }
}
