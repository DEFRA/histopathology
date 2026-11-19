using Histo.Web.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Histo.Tests.Unit;

public class SecurityHeadersNonceTests
{
    private sealed class CapturingResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

        public override void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));

        public async Task FireOnStartingAsync()
        {
            foreach (var (callback, state) in _onStarting) await callback(state);
        }
    }

    private static async Task<(HttpContext Context, string Csp)> RunAsync()
    {
        var feature = new CapturingResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(feature);

        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()).UseSecurityHeaders(sendHsts: false);
        await app.Build()(context);
        await feature.FireOnStartingAsync();

        return (context, feature.Headers.ContentSecurityPolicy.ToString());
    }

    [Fact]
    public async Task ContentSecurityPolicy_AllowsOnlyNoncedInlineScripts()
    {
        var (context, csp) = await RunAsync();

        var nonce = context.GetCspNonce();
        Assert.False(string.IsNullOrEmpty(nonce));
        Assert.Contains($"script-src 'self' 'nonce-{nonce}'", csp);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp);
        Assert.Contains("style-src 'self';", csp);
    }

    [Fact]
    public async Task ContentSecurityPolicy_IssuesADifferentNonceForEveryRequest()
    {
        var (first, _) = await RunAsync();
        var (second, _) = await RunAsync();

        Assert.NotEqual(first.GetCspNonce(), second.GetCspNonce());
    }

    private static TagHelperOutput NewScriptOutput() =>
        new("script", [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

    private static TagHelperContext NewContext() =>
        new("script", [], new Dictionary<object, object>(), Guid.NewGuid().ToString());

    [Fact]
    public void CspNonceTagHelper_AddsTheRequestNonceToScriptElements()
    {
        var http = new DefaultHttpContext();
        http.Items[SecurityHeadersExtensions.CspNonceKey] = "abc123";
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);
        var output = NewScriptOutput();

        new CspNonceTagHelper(accessor.Object).Process(NewContext(), output);

        Assert.Equal("abc123", output.Attributes["nonce"].Value);
    }

    [Fact]
    public void CspNonceTagHelper_LeavesAnExistingNonceAlone()
    {
        var http = new DefaultHttpContext();
        http.Items[SecurityHeadersExtensions.CspNonceKey] = "abc123";
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(http);
        var output = NewScriptOutput();
        output.Attributes.SetAttribute("nonce", "custom");

        new CspNonceTagHelper(accessor.Object).Process(NewContext(), output);

        Assert.Equal("custom", output.Attributes["nonce"].Value);
    }

    [Fact]
    public void CspNonceTagHelper_WithoutARequestNonce_AddsNothing()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());
        var output = NewScriptOutput();

        new CspNonceTagHelper(accessor.Object).Process(NewContext(), output);

        Assert.False(output.Attributes.ContainsName("nonce"));
    }

    [Theory]
    [InlineData(true, "GET")]
    [InlineData(false, "POST")]
    public async Task ForceGetWhenReExecuted_OnlyRewritesTheMethodOfReExecutedRequests(bool reExecuted, string expectedMethod)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        if (reExecuted)
            context.Features.Set<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature());
        var nextCalled = false;

        await SecurityHeadersExtensions.ForceGetWhenReExecuted(context, _ => { nextCalled = true; return Task.CompletedTask; });

        Assert.Equal(expectedMethod, context.Request.Method);
        Assert.True(nextCalled);
    }
}
