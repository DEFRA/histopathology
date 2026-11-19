using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Histo.Web.Security;

/// <summary>Stamps the per-request Content-Security-Policy nonce on every script element.</summary>
[HtmlTargetElement("script")]
public sealed class CspNonceTagHelper(IHttpContextAccessor httpContextAccessor) : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (output.Attributes.ContainsName("nonce")) return;

        var nonce = httpContextAccessor.HttpContext?.GetCspNonce();
        if (!string.IsNullOrEmpty(nonce))
            output.Attributes.SetAttribute("nonce", nonce);
    }
}
