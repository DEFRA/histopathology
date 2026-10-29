using Histo.Core.Domain;

namespace Histo.Tests.Unit;

public class BatchStatusCoverageTests
{
    [Theory]
    [InlineData(BatchStatus.Submitted, "Not received")]
    [InlineData(BatchStatus.Received, "Received")]
    [InlineData(BatchStatus.Rejected, "Rejected")]
    [InlineData(BatchStatus.Completed, "Completed")]
    [InlineData(BatchStatus.OnHold, "On hold")]
    [InlineData(BatchStatus.InProgress, "In progress")]
    [InlineData("99", "99")]
    public void DisplayName_MapsEveryStatusCode(string code, string expected) =>
        Assert.Equal(expected, BatchStatus.DisplayName(code));

    [Theory]
    [InlineData("Not received", "Not received")]
    [InlineData("  NOT STARTED ", "Not received")]
    [InlineData("submitted", "Not received")]
    [InlineData("received", "Received")]
    [InlineData("Rejected", "Rejected")]
    [InlineData("COMPLETED", "Completed")]
    [InlineData("on hold", "On hold")]
    [InlineData("OnHold", "On hold")]
    [InlineData("In Progress", "In progress")]
    [InlineData("inprogress", "In progress")]
    [InlineData("Something Else", "Something Else")]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, "")]
    public void NormalizeDisplayText_MapsAliasesAndPassesUnknownThrough(string? raw, string expected) =>
        Assert.Equal(expected, BatchStatus.NormalizeDisplayText(raw));

    [Theory]
    [InlineData(BatchStatus.Submitted, "grey")]
    [InlineData(BatchStatus.Received, "purple")]
    [InlineData(BatchStatus.InProgress, "teal")]
    [InlineData(BatchStatus.OnHold, "orange")]
    [InlineData(BatchStatus.Completed, "green")]
    [InlineData(BatchStatus.Rejected, "red")]
    [InlineData("99", "grey")]
    public void TagClass_MapsEveryStatusCode(string code, string expected) =>
        Assert.Equal(expected, BatchStatus.TagClass(code));

    [Theory]
    [InlineData("Not received", "grey")]
    [InlineData("received", "purple")]
    [InlineData("in progress", "teal")]
    [InlineData("on hold", "orange")]
    [InlineData("completed", "green")]
    [InlineData("rejected", "red")]
    [InlineData("unknown text", "grey")]
    [InlineData(null, "grey")]
    public void TagClassForText_MapsFreeTextStatus(string? text, string expected) =>
        Assert.Equal(expected, BatchStatus.TagClassForText(text));
}
