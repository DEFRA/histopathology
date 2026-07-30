using Histo.Submissions.Models;

namespace Histo.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="BatchConcurrencyException"/> and
/// <see cref="AnimalConcurrencyException"/> — verifying the default messages
/// match the legacy VB.NET strings users would see.
/// </summary>
public class DomainExceptionTests
{
    [Fact]
    public void BatchConcurrencyException_DefaultMessage_IsExpected()
    {
        var ex = new BatchConcurrencyException();
        Assert.Equal("Another user has modified this batch record.", ex.Message);
    }

    [Fact]
    public void AnimalConcurrencyException_DefaultMessage_IsExpected()
    {
        var ex = new AnimalConcurrencyException();
        Assert.Equal("Another user has modified this sample record.", ex.Message);
    }
}
