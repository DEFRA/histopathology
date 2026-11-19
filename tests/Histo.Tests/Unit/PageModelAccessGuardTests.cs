using Histo.Web.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Tests.Unit;

/// <summary>
/// Guards deny-by-default: a PageModel that skips <see cref="HistoPageModel"/> also skips the
/// tblUser (Gate 2) check and the batch-area guard, so any such page must be listed here on purpose.
/// </summary>
public class PageModelAccessGuardTests
{
    private static readonly HashSet<string> IntentionallyOutsideHistoPageModel =
    [
        "AccessDeniedModel",
        "SessionErrorModel",
        "ServiceProblemModel",
        "ErrorModel",
    ];

    private static IEnumerable<Type> PageModels() =>
        typeof(HistoPageModel).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && typeof(PageModel).IsAssignableFrom(t));

    [Fact]
    public void EveryPageModelDerivesFromHistoPageModelOrIsListedAsPublic()
    {
        var unguarded = PageModels()
            .Where(t => !typeof(HistoPageModel).IsAssignableFrom(t))
            .Select(t => t.Name)
            .Where(n => !IntentionallyOutsideHistoPageModel.Contains(n))
            .OrderBy(n => n)
            .ToList();

        Assert.True(unguarded.Count == 0, "PageModels bypassing HistoPageModel: " + string.Join(", ", unguarded));
    }

    [Fact]
    public void ThePublicPageAllowListOnlyNamesRealPageModels()
    {
        var names = PageModels().Select(t => t.Name).ToHashSet();

        Assert.All(IntentionallyOutsideHistoPageModel, n => Assert.Contains(n, names));
    }
}
