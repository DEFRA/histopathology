using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Histo.Web.Pages;

/// <summary>Renders GOV.UK summary list rows for use inside a caller-owned &lt;dl class="govuk-summary-list"&gt;.</summary>
public static class SummaryList
{
    public static IHtmlContent Row(string key, string? value)
    {
        var row = new TagBuilder("div");
        row.AddCssClass("govuk-summary-list__row");

        var dt = new TagBuilder("dt");
        dt.AddCssClass("govuk-summary-list__key");
        dt.InnerHtml.Append(key);

        var dd = new TagBuilder("dd");
        dd.AddCssClass("govuk-summary-list__value");
        dd.InnerHtml.Append(value ?? string.Empty);

        row.InnerHtml.AppendHtml(dt);
        row.InnerHtml.AppendHtml(dd);
        return row;
    }
}
