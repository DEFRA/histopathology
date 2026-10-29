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

/// <summary>The ACS endpoint must not sign anyone in from a SAML response that fails validation.</summary>
public class AuthControllerAcsTests
{
    private static AuthController CreateSut(DefaultHttpContext http) =>
        new(
            new Saml2Configuration(),
            Mock.Of<IUserService>(),
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            Mock.Of<ILogger<AuthController>>(),
            new ConfigurationBuilder().Build())
        {
            ControllerContext = new ControllerContext { HttpContext = http },
        };

    [Fact]
    public async Task AssertionConsumerService_MalformedSamlResponse_ThrowsWithoutSigningIn()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = "POST";
        http.Request.ContentType = "application/x-www-form-urlencoded";
        http.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["SAMLResponse"] = "not-saml" });
        var sut = CreateSut(http);

        await Assert.ThrowsAnyAsync<Exception>(() => sut.AssertionConsumerService());

        Assert.False(http.User.Identity?.IsAuthenticated ?? false);
    }
}
