using Histo.Administration.Models;

namespace Histo.Web.Pages.Batches;

public static class BatchTestListOptions
{
    public const string Other = "Other";

    public static IReadOnlyList<LookupItem> EnsureOtherOption(IEnumerable<LookupItem>? options)
    {
        var items = (options ?? []).ToList();

        if (items.Any(item =>
                string.Equals(item.Name, Other, StringComparison.OrdinalIgnoreCase)
             || string.Equals(item.Code, Other, StringComparison.OrdinalIgnoreCase)))
        {
            return items;
        }

        items.Add(new LookupItem
        {
            ID = 0,
            Name = Other,
            Code = Other,
            Active = true,
        });

        return items;
    }
}
