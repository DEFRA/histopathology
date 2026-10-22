using System.Security.Claims;
using Histo.Web.Auth;
using Histo.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>CDE-01 (security headers) and CDE-09 (earlier sessions must not survive a new sign-in).</summary>
public class SecurityHardeningTests
{
    private sealed class CapturingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => false;
        public void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));
        public void OnCompleted(Func<object, Task> callback, object state) { }

        public async Task FireOnStartingAsync()
        {
            foreach (var (callback, state) in _onStarting) await callback(state);
        }
    }

    private static async Task<IHeaderDictionary> RunHeadersMiddlewareAsync(bool sendHsts)
    {
        var feature = new CapturingResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(feature);

        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider()).UseSecurityHeaders(sendHsts);
        await app.Build()(context);
        await feature.FireOnStartingAsync();

        return feature.Headers;
    }

    [Fact]
    public async Task SecurityHeaders_AddsTheBaselineHeaders()
    {
        var headers = await RunHeadersMiddlewareAsync(sendHsts: true);

        Assert.Equal("nosniff", headers.XContentTypeOptions);
        Assert.Equal("DENY", headers.XFrameOptions);
        Assert.Contains("frame-ancestors 'none'", headers.ContentSecurityPolicy.ToString());
        Assert.Contains("max-age=", headers.StrictTransportSecurity.ToString());
    }

    [Fact]
    public async Task SecurityHeaders_HstsCanBeTurnedOffForLocalDevelopment()
    {
        var headers = await RunHeadersMiddlewareAsync(sendHsts: false);

        Assert.False(headers.ContainsKey("Strict-Transport-Security"));
        Assert.True(headers.ContainsKey("X-Content-Type-Options"));
    }

    private static MemoryDistributedCache NewCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    private static CookieValidatePrincipalContext ContextFor(string? userId, string? stamp, IDistributedCache cache)
    {
        var auth = new Mock<IAuthenticationService>();
        auth.Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string?>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        var services = new ServiceCollection()
            .AddSingleton(cache)
            .AddSingleton(auth.Object)
            .BuildServiceProvider();

        var identity = new ClaimsIdentity("test");
        if (userId is not null) identity.AddClaim(new Claim(AppClaimTypes.UserDbId, userId));
        if (stamp is not null) identity.AddClaim(new Claim(AppClaimTypes.SessionStamp, stamp));
        var principal = new ClaimsPrincipal(identity);

        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme);
        return new CookieValidatePrincipalContext(
            new DefaultHttpContext { RequestServices = services }, scheme, new CookieAuthenticationOptions(), ticket);
    }

    [Fact]
    public async Task SingleSession_CurrentStamp_StaysSignedIn()
    {
        var cache = NewCache();
        var stamp = await SingleSessionPolicy.IssueStampAsync(cache, "7");
        var context = ContextFor("7", stamp, cache);

        await SingleSessionPolicy.ValidateAsync(context);

        Assert.NotNull(context.Principal);
    }

    [Fact]
    public async Task SingleSession_AfterANewSignIn_TheEarlierCookieIsRejected()
    {
        var cache = NewCache();
        var firstStamp = await SingleSessionPolicy.IssueStampAsync(cache, "7");
        await SingleSessionPolicy.IssueStampAsync(cache, "7");
        var context = ContextFor("7", firstStamp, cache);

        await SingleSessionPolicy.ValidateAsync(context);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task SingleSession_OtherUsersAreNotAffected()
    {
        var cache = NewCache();
        var stampForUser7 = await SingleSessionPolicy.IssueStampAsync(cache, "7");
        await SingleSessionPolicy.IssueStampAsync(cache, "8");
        var context = ContextFor("7", stampForUser7, cache);

        await SingleSessionPolicy.ValidateAsync(context);

        Assert.NotNull(context.Principal);
    }

    [Theory]
    [InlineData("7", null)]
    [InlineData(null, "abc")]
    public async Task SingleSession_CookieWithoutStampOrUser_IsLeftAlone(string? userId, string? stamp)
    {
        var context = ContextFor(userId, stamp, NewCache());

        await SingleSessionPolicy.ValidateAsync(context);

        Assert.NotNull(context.Principal);
    }

    [Fact]
    public async Task SingleSession_NoStampInCache_DoesNotLockTheUserOut()
    {
        var context = ContextFor("7", "old-stamp", NewCache());

        await SingleSessionPolicy.ValidateAsync(context);

        Assert.NotNull(context.Principal);
    }
}
