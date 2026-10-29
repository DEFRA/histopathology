using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Histo.Administration.Interfaces;
using Histo.Administration.Models;
using Histo.Web.Auth;
using Histo.Web.Controllers;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Claims;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens.Saml2;
using Moq;
using System.ServiceModel.Security;

namespace Histo.Tests.Unit;

/// <summary>Sign-in redirect, ACS success/failure branches and SLO success for the SAML controller.</summary>
public sealed partial class AuthControllerFlowTests : IDisposable
{
    private const string SpIssuer = "https://sp.test/";
    private const string IdpIssuer = "https://idp.test/";
    private const string AcsUrl = "https://sp.test/Saml2/Acs";

    private readonly X509Certificate2 _cert = CreateCertificate();
    private readonly Mock<IUserService> _users = new();
    private readonly Mock<IAuthenticationService> _auth = new();
    private readonly Mock<ILogger<AuthController>> _logger = new();
    private readonly IDistributedCache _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    public void Dispose() => _cert.Dispose();

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=histo-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx), null);
    }

    private Saml2Configuration SpConfig()
    {
        var config = new Saml2Configuration
        {
            Issuer = SpIssuer,
            SingleSignOnDestination = new Uri("https://idp.test/sso"),
            SingleLogoutDestination = new Uri("https://idp.test/slo"),
            SigningCertificate = _cert,
            CertificateValidationMode = X509CertificateValidationMode.None,
            RevocationMode = X509RevocationMode.NoCheck,
        };
        config.AllowedAudienceUris.Add(SpIssuer);
        config.SignatureValidationCertificates.Add(_cert);
        return config;
    }

    private Saml2Configuration IdpConfig() => new()
    {
        Issuer = IdpIssuer,
        SigningCertificate = _cert,
        CertificateValidationMode = X509CertificateValidationMode.None,
        RevocationMode = X509RevocationMode.NoCheck,
    };

    private AuthController CreateSut(DefaultHttpContext http, bool forceAuthn = false)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Saml2:ForceAuthn"] = forceAuthn.ToString() })
            .Build();
        return new AuthController(SpConfig(), _users.Object, _cache, _logger.Object, configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            Url = new UrlHelper(new ActionContext(http, new RouteData(), new ActionDescriptor())),
        };
    }

    private DefaultHttpContext NewHttp()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_auth.Object);
        services.AddSingleton<IUrlHelperFactory>(new UrlHelperFactory());
        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        http.Request.Scheme = "https";
        http.Request.Host = new HostString("sp.test");
        return http;
    }

    private string SignedResponseForm(string? email, string? relayState, bool failed = false, string? emailClaimType = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "name-id-1"), new(ClaimTypes.GivenName, "Test") };
        if (email is not null) claims.Add(new Claim(emailClaimType ?? ClaimTypes.Email, email));

        var response = new Saml2AuthnResponse(IdpConfig())
        {
            Status = failed ? Saml2StatusCodes.Responder : Saml2StatusCodes.Success,
            Destination = new Uri(AcsUrl),
        };
        if (!failed)
        {
            response.NameId = new Saml2NameIdentifier("name-id-1", NameIdentifierFormats.Persistent);
            response.ClaimsIdentity = new ClaimsIdentity(claims);
            _ = response.CreateSecurityToken(SpIssuer, subjectConfirmationLifetime: 5, issuedTokenLifetime: 60);
        }

        var binding = new Saml2PostBinding { RelayState = relayState };
        binding.Bind(response);
        return SamlResponseField().Match(binding.PostContent).Groups[1].Value;
    }

    [GeneratedRegex("name=\"SAMLResponse\" value=\"([^\"]+)\"")]
    private static partial Regex SamlResponseField();

    private static DefaultHttpContext WithForm(DefaultHttpContext http, string samlResponse, string? relayState)
    {
        http.Request.Method = "POST";
        http.Request.ContentType = "application/x-www-form-urlencoded";
        var fields = new Dictionary<string, StringValues> { ["SAMLResponse"] = samlResponse };
        if (relayState is not null) fields["RelayState"] = relayState;
        http.Request.Form = new FormCollection(fields);
        return http;
    }

    private static User ActiveUser() => new()
    {
        UserID = 7,
        GroupName = "Admin",
        GroupCode = 2,
        AreaName = "Weybridge",
        AreaCode = 3,
    };

    private IEnumerable<ClaimsPrincipal> SignedInPrincipals() =>
        _auth.Invocations.Where(i => i.Method.Name == nameof(IAuthenticationService.SignInAsync))
            .Select(i => (ClaimsPrincipal)i.Arguments[2]);

    private static string? LocationOf(IActionResult result) => result switch
    {
        LocalRedirectResult r => r.Url,
        RedirectResult r => r.Url,
        _ => null,
    };

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("/Submissions/ViewSubmissions", true)]
    [InlineData("https://evil.example/x", false)]
    public void Login_RedirectsToIdpAndKeepsOnlyLocalReturnUrl(string? returnUrl, bool relayStateExpected)
    {
        var result = CreateSut(NewHttp()).Login(returnUrl);

        var location = Assert.IsType<RedirectResult>(result).Url;
        Assert.StartsWith("https://idp.test/sso", location, StringComparison.Ordinal);
        Assert.Equal(relayStateExpected, location.Contains("RelayState=", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Login_ForceAuthnConfig_ControlsRequestedReauthentication(bool forceAuthn)
    {
        var result = CreateSut(NewHttp(), forceAuthn).Login();

        Assert.StartsWith("https://idp.test/sso", Assert.IsType<RedirectResult>(result).Url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AssertionConsumerService_FailedStatus_RedirectsToAccessDeniedWithoutSignIn()
    {
        var http = WithForm(NewHttp(), SignedResponseForm(null, null, failed: true), null);

        var result = await CreateSut(http).AssertionConsumerService();

        Assert.Equal("/AccessDenied", LocationOf(result));
        Assert.Empty(SignedInPrincipals());
    }

    [Theory]
    [InlineData(null, "/Index")]
    [InlineData("/Batches/BatchDetails", "/Batches/BatchDetails")]
    [InlineData("https://evil.example/", "/Index")]
    public async Task AssertionConsumerService_KnownUser_SignsInWithAppClaimsAndRedirects(string? relayState, string expected)
    {
        _users.Setup(u => u.ResolveUserByEmailAsync("a@b.gov.uk", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveUser());
        var http = WithForm(NewHttp(), SignedResponseForm("a@b.gov.uk", relayState), relayState);

        var result = await CreateSut(http).AssertionConsumerService();

        Assert.Equal(expected, LocationOf(result));
        var principal = Assert.Single(SignedInPrincipals());
        Assert.Equal("Admin", principal.FindFirst(AppClaimTypes.GroupName)?.Value);
        Assert.Equal("7", principal.FindFirst(AppClaimTypes.UserDbId)?.Value);
        Assert.Equal("2", principal.FindFirst(AppClaimTypes.GroupId)?.Value);
        Assert.Equal("Weybridge", principal.FindFirst(AppClaimTypes.UserArea)?.Value);
        Assert.Equal("3", principal.FindFirst(AppClaimTypes.UserAreaId)?.Value);
    }

    [Fact]
    public async Task AssertionConsumerService_EmailInPreferredUsernameClaim_ResolvesUser()
    {
        _users.Setup(u => u.ResolveUserByEmailAsync("p@b.gov.uk", It.IsAny<CancellationToken>())).ReturnsAsync(ActiveUser());
        var http = WithForm(NewHttp(), SignedResponseForm("p@b.gov.uk", null, emailClaimType: "preferred_username"), null);

        await CreateSut(http).AssertionConsumerService();

        _users.Verify(u => u.ResolveUserByEmailAsync("p@b.gov.uk", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssertionConsumerService_UnknownUser_SignsInWithoutAppClaims()
    {
        _users.Setup(u => u.ResolveUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var http = WithForm(NewHttp(), SignedResponseForm("x@b.gov.uk", null), null);

        var result = await CreateSut(http).AssertionConsumerService();

        Assert.Equal("/Index", LocationOf(result));
        var principal = Assert.Single(SignedInPrincipals());
        Assert.Null(principal.FindFirst(AppClaimTypes.GroupName));
    }

    [Fact]
    public async Task AssertionConsumerService_NoEmailClaim_SignsInWithoutLookingUpUser()
    {
        var http = WithForm(NewHttp(), SignedResponseForm(null, null), null);

        await CreateSut(http).AssertionConsumerService();

        _users.Verify(u => u.ResolveUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Single(SignedInPrincipals());
    }

    [Fact]
    public void Logout_AuthenticatedUser_SignsOutAndRedirectsToIdpSlo()
    {
        var http = NewHttp();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(Saml2ClaimTypes.NameId, "name-id-1"), new Claim(Saml2ClaimTypes.SessionIndex, "s1")], "saml2"));

        var result = CreateSut(http).Logout();

        Assert.StartsWith("https://idp.test/slo", Assert.IsType<RedirectResult>(result).Url, StringComparison.Ordinal);
        _auth.Verify(a => a.SignOutAsync(http, It.IsAny<string?>(), It.IsAny<AuthenticationProperties?>()), Times.Once);
    }
}
