using Histo.Administration.Models;
using Histo.Web.Pages.Batches;

namespace Histo.Tests.Unit;

public class BatchTestListOptionsTests
{
    [Fact]
    public void EnsureOtherOption_AddsOtherWhenMissing()
    {
        var options = new[]
        {
            new LookupItem { ID = 1, Name = "PrP", Code = "PrP" },
            new LookupItem { ID = 2, Name = "Congo Red", Code = "CR" }
        };

        var result = BatchTestListOptions.EnsureOtherOption(options);

        Assert.Contains(result, item => string.Equals(item.Name, "Other", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, item => string.Equals(item.Code, "Other", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnsureOtherOption_DoesNotDuplicateExistingOther()
    {
        var options = new[]
        {
            new LookupItem { ID = 3, Name = "Other", Code = "Other" },
            new LookupItem { ID = 4, Name = "PrP", Code = "PrP" }
        };

        var result = BatchTestListOptions.EnsureOtherOption(options);

        Assert.Single(result, item => string.Equals(item.Name, "Other", StringComparison.OrdinalIgnoreCase));
    }
}
