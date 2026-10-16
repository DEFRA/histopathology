using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace Histo.Web.Pages;

public static class ReturnPageRedirect
{
    /// <summary>
    /// <c>RedirectToPage</c> needs a bare page name, but a stored return page can carry its own query string
    /// (e.g. Search submissions' <c>?ReturnPage=...</c>). Splits it and merges that query with the saved sort/page query.
    /// </summary>
    public static RedirectToPageResult RedirectToReturnPage(this PageModel page, string returnPage, string? returnPageQuery)
    {
        var split = returnPage.IndexOf('?');
        var path = split < 0 ? returnPage : returnPage[..split];

        var values = new RouteValueDictionary();
        foreach (var query in new[] { split < 0 ? null : returnPage[split..], returnPageQuery })
        {
            if (string.IsNullOrEmpty(query)) continue;
            foreach (var pair in QueryHelpers.ParseQuery(query))
                values[pair.Key] = pair.Value.ToString();
        }

        return page.RedirectToPage(path, values);
    }
}
