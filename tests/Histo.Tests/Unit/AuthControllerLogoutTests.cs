using Histo.Administration.Interfaces;
using Histo.Web.Controllers;
using ITfoxtec.Identity.Saml2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>The SLO endpoint must reject anything that is not a well-formed, signed SAML logout message.</summary>
public class AuthControllerLogoutTests
{
    private readonly Mock<ILogger<AuthController>> _logger = new();

    private AuthController CreateSut(DefaultHttpContext http)
    {
        var configuration = new ConfigurationBuilder().Build();
        return new AuthController(
            new Saml2Configuration(),
            Mock.Of<IUserService>(),
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            _logger.Object,
            configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
        };
    }

    private static DefaultHttpContext FormPost(params (string Key, string Value)[] fields)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = "POST";
        http.Request.ContentType = "application/x-www-form-urlencoded";
        http.Request.Form = new FormCollection(fields.ToDictionary(f => f.Key, f => new StringValues(f.Value)));
        return http;
    }

    private void AssertWarningLogged() =>
        _logger.Verify(l => l.Log(
            LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

    [Fact]
    public void SingleLogoutService_NotAFormPost_ReturnsBadRequest()
    {
        var result = CreateSut(new DefaultHttpContext()).SingleLogoutService(null, null);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public void SingleLogoutService_FormWithoutSamlMessage_ReturnsBadRequest()
    {
        var result = CreateSut(FormPost(("other", "x"))).SingleLogoutService(null, null);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public void SingleLogoutService_UnsignedOrMalformedSamlResponse_ReturnsBadRequestAndLogsWarning()
    {
        var result = CreateSut(FormPost(("SAMLResponse", "not-saml"))).SingleLogoutService("not-saml", null);

        Assert.IsType<BadRequestResult>(result);
        AssertWarningLogged();
    }

    [Fact]
    public void SingleLogoutService_UnsignedOrMalformedSamlRequest_ReturnsBadRequestAndLogsWarning()
    {
        var result = CreateSut(FormPost(("SAMLRequest", "not-saml"))).SingleLogoutService(null, "not-saml");

        Assert.IsType<BadRequestResult>(result);
        AssertWarningLogged();
    }
}
