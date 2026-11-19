using System.Reflection;
using Histo.Web.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Histo.Tests.Unit;

public class AuthControllerTests
{
    private static AuthController CreateSut()
    {
        var http = new DefaultHttpContext();
        var sut = new AuthController { ControllerContext = new ControllerContext { HttpContext = http } };
        sut.Url = new UrlHelper(new ActionContext(http, new RouteData(), new ActionDescriptor()));
        return sut;
    }

    [Theory]
    [InlineData("/Batches/BatchDetails?id=5", "/Batches/BatchDetails?id=5")]
    [InlineData("https://evil.example/", "/Index")]
    [InlineData("//evil.example", "/Index")]
    [InlineData(null, "/Index")]
    [InlineData("", "/Index")]
    public void Login_ChallengesOidc_WithOnlyLocalReturnUrls(string? returnUrl, string expectedRedirect)
    {
        var result = Assert.IsType<ChallengeResult>(CreateSut().Login(returnUrl));

        Assert.Equal([OpenIdConnectDefaults.AuthenticationScheme], result.AuthenticationSchemes);
        Assert.Equal(expectedRedirect, result.Properties!.RedirectUri);
    }

    [Fact]
    public void Logout_SignsOutOfCookieAndEntraId()
    {
        var result = Assert.IsType<SignOutResult>(CreateSut().Logout());

        Assert.Equal(
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme],
            result.AuthenticationSchemes);
        Assert.Equal("/Index", result.Properties!.RedirectUri);
    }

    [Fact]
    public void Logout_RequiresSignedInUserAndAntiforgeryPost()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Logout))!;

        Assert.NotNull(method.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
    }
}
