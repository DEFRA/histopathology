using System.Security.Claims;
using Histo.Web.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Histo.Tests.Unit;

public class TelemetryHelperTests
{
    private readonly Mock<ILogger<TelemetryHelper>> _logger = new();
    private readonly Mock<IHttpContextAccessor> _accessor = new();

    private TelemetryHelper CreateSut() => new(_logger.Object, _accessor.Object);

    private (LogLevel Level, Exception? Exception, string Message) CaptureLog()
    {
        var captured = _logger.Invocations.Single(i => i.Method.Name == nameof(ILogger.Log));
        return ((LogLevel)captured.Arguments[0], (Exception?)captured.Arguments[3], captured.Arguments[2]?.ToString() ?? "");
    }

    [Fact]
    public void TrackException_WithRequestContext_LogsErrorWithUserPathAndExtraProperties()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Jo Bloggs")], "test"));
        context.Request.Path = "/Batches/Summary";
        _accessor.SetupGet(a => a.HttpContext).Returns(context);
        var boom = new InvalidOperationException("boom");

        CreateSut().TrackException(boom, "SaveBatch", new Dictionary<string, string> { ["BatchId"] = "5", ["Step"] = "2" });

        var (level, exception, message) = CaptureLog();
        Assert.Equal(LogLevel.Error, level);
        Assert.Same(boom, exception);
        Assert.Equal("Exception in SaveBatch for user Jo Bloggs at /Batches/Summary. BatchId=5, Step=2", message);
    }

    [Fact]
    public void TrackException_WithoutHttpContext_LogsAnonymousUserAndNoExtraProperties()
    {
        _accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);

        CreateSut().TrackException(new InvalidOperationException("boom"), "SaveBatch");

        var (_, _, message) = CaptureLog();
        Assert.Equal("Exception in SaveBatch for user anonymous at . ", message);
    }

    [Fact]
    public void TrackEvent_WithProperties_LogsInformationCarryingTheCustomEventNameAndProperties()
    {
        CreateSut().TrackEvent("SubmissionCreated", new Dictionary<string, string> { ["BatchId"] = "5" });

        var invocation = _logger.Invocations.Single(i => i.Method.Name == nameof(ILogger.Log));
        var state = Assert.IsType<IEnumerable<KeyValuePair<string, object?>>>(invocation.Arguments[2], exactMatch: false).ToList();
        Assert.Equal(LogLevel.Information, (LogLevel)invocation.Arguments[0]);
        Assert.Contains(new KeyValuePair<string, object?>("microsoft.custom_event.name", "SubmissionCreated"), state);
        Assert.Contains(new KeyValuePair<string, object?>("BatchId", "5"), state);
    }

    [Fact]
    public void TrackEvent_WithoutProperties_LogsOnlyTheCustomEventName()
    {
        CreateSut().TrackEvent("SamplesDispatched");

        var invocation = _logger.Invocations.Single(i => i.Method.Name == nameof(ILogger.Log));
        var state = Assert.IsType<IEnumerable<KeyValuePair<string, object?>>>(invocation.Arguments[2], exactMatch: false).ToList();
        var only = Assert.Single(state);
        Assert.Equal("SamplesDispatched", only.Value);
    }
}
