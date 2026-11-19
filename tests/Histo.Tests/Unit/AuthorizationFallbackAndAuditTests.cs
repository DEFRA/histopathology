using System.Security.Claims;
using Histo.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Histo.Tests.Unit;

public class AuthorizationFallbackAndAuditTests
{
    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public List<object?> States { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            States.Add(state);
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    private static (DefaultHttpContext Context, CapturingLogger Logger) ContextWithLogger(ClaimsPrincipal user)
    {
        var logger = new CapturingLogger();
        var factory = new Mock<ILoggerFactory>();
        factory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger);
        var context = new DefaultHttpContext
        {
            User = user,
            RequestServices = new ServiceCollection().AddSingleton(factory.Object).BuildServiceProvider(),
        };
        context.Request.Path = "/Admin/Users";
        context.Request.Method = "GET";
        return (context, logger);
    }

    private static ClaimsPrincipal SignedIn(string group, string? userId = null)
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(AppClaimTypes.GroupName, group));
        identity.AddClaim(new Claim(ClaimTypes.Email, "someone@example.test"));
        if (userId is not null) identity.AddClaim(new Claim(AppClaimTypes.UserDbId, userId));
        return new ClaimsPrincipal(identity);
    }

    private static async Task<AuthorizationResult> EvaluateFallbackAsync(ClaimsPrincipal user)
    {
        var services = new ServiceCollection().AddLogging().AddAuthorization().BuildServiceProvider();
        var auth = services.GetRequiredService<IAuthorizationService>();
        return await auth.AuthorizeAsync(user, null, AuthorizationPolicies.FallbackRequireSignedIn());
    }

    private static Task RunFilterAsync(IAsyncPageFilter filter, HttpContext httpContext)
    {
        var pageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()));
        var context = new PageHandlerExecutingContext(
            pageContext, [], new HandlerMethodDescriptor(), new Dictionary<string, object?>(), handlerInstance: new object());
        return filter.OnPageHandlerExecutionAsync(context, () =>
            Task.FromResult(new PageHandlerExecutedContext(pageContext, [], new HandlerMethodDescriptor(), new object())));
    }

    [Fact]
    public async Task FallbackPolicy_RejectsAnonymousUser()
    {
        var result = await EvaluateFallbackAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task FallbackPolicy_AllowsSignedInUser()
    {
        var result = await EvaluateFallbackAsync(SignedIn("Customer"));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void LogDenied_WritesWarningWithOpaqueUserIdAndNoClaimValues()
    {
        var (context, logger) = ContextWithLogger(SignedIn("Customer", "42"));

        AuthorizationAudit.LogDenied(context, "batch outside user area", 3049);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("userId=42", entry.Message);
        Assert.Contains("resourceId=3049", entry.Message);
        Assert.Contains("path=/Admin/Users", entry.Message);
        Assert.DoesNotContain("someone@example.test", entry.Message);
    }

    [Fact]
    public void LogDenied_EmitsAnApplicationInsightsCustomEventWithoutClaimValues()
    {
        var (context, logger) = ContextWithLogger(SignedIn("Customer", "42"));

        AuthorizationAudit.LogDenied(context, "batch outside user area", 3049);

        var state = Assert.IsType<IReadOnlyList<KeyValuePair<string, object?>>>(Assert.Single(logger.States), exactMatch: false);
        var values = state.ToDictionary(p => p.Key, p => p.Value);
        Assert.Equal(AuthorizationAudit.EventName, values["microsoft.custom_event.name"]);
        Assert.Equal("42", values["UserId"]);
        Assert.Equal(3049, values["ResourceId"]);
        Assert.DoesNotContain(values.Values, v => v?.ToString()?.Contains("someone@example.test") == true);
    }

    [Fact]
    public void LogDenied_AnonymousUser_IsLoggedAsAnonymous()
    {
        var (context, logger) = ContextWithLogger(new ClaimsPrincipal(new ClaimsIdentity()));

        AuthorizationAudit.LogDenied(context, "x");

        Assert.Contains("userId=anonymous", Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public void LogDenied_WithoutContextOrServices_DoesNotThrow()
    {
        var thrown = Record.Exception(() =>
        {
            AuthorizationAudit.LogDenied(null, "x");
            AuthorizationAudit.LogDenied(new DefaultHttpContext(), "x");
        });

        Assert.Null(thrown);
    }

    [Fact]
    public async Task MaintenanceOnlyFilter_LogsDenialForNonMaintenanceUser()
    {
        var (context, logger) = ContextWithLogger(SignedIn("Customer", "7"));

        await RunFilterAsync(new MaintenanceOnlyPageFilter(), context);

        Assert.Contains("Maintenance required", Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task MaintenanceOnlyFilter_DoesNotLogForMaintenanceUser()
    {
        var (context, logger) = ContextWithLogger(SignedIn("Maintenance", "7"));

        await RunFilterAsync(new MaintenanceOnlyPageFilter(), context);

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task HistoOrMaintenanceFilter_LogsDenialForCustomer()
    {
        var (context, logger) = ContextWithLogger(SignedIn("Customer", "7"));

        await RunFilterAsync(new HistoOrMaintenancePageFilter(), context);

        Assert.Contains("Histopathology User or Maintenance required", Assert.Single(logger.Entries).Message);
    }
}
