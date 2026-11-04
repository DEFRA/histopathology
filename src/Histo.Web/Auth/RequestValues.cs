namespace Histo.Web.Auth;

public static class RequestValues
{
    /// <summary>First positive integer found for any of the keys in the query string, then the form.</summary>
    public static int? GetPositiveInt(HttpRequest request, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (TryParse(request.Query[key], out var fromQuery)) return fromQuery;
            if (request.HasFormContentType && TryParse(request.Form[key], out var fromForm)) return fromForm;
        }

        return null;
    }

    private static bool TryParse(Microsoft.Extensions.Primitives.StringValues values, out int value) =>
        int.TryParse(values.ToString(), out value) && value > 0;
}
