using System.Security.Claims;
using Histo.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Histo.TestSupport;

/// <summary>Who the fake auth signs the request in as. A null <see cref="Group"/> is a signed-in user with no tblUser record.</summary>
public sealed record TestUser(string? Group, int AreaId = 0, int UserId = 1)
{
    public static TestUser Customer(int areaId, int userId = 1) => new("Customer", areaId, userId);
    public static TestUser HistoUser(int userId = 1) => new("Histopathology User", 5, userId);
    public static TestUser MaintenanceUser(int userId = 1) => new("Maintenance", 5, userId);

    public static readonly TestUser Histo = HistoUser();
    public static readonly TestUser Maintenance = MaintenanceUser();
    public static readonly TestUser UnknownUser = new((string?)null);
}

public static class TestIdentity
{
    public const string GroupHeader = "X-Test-Group";
    public const string AreaHeader = "X-Test-Area";
    public const string UserHeader = "X-Test-User";
    public const string SignedInHeader = "X-Test-SignedIn";

    /// <summary>Program.cs reads configuration eagerly, so it must come from the environment, not test config callbacks.</summary>
    public static void UseHostSettings(string connectionString)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__HistologyDb", connectionString);
        Environment.SetEnvironmentVariable("Saml2__IdPMetadataUrl", "");
    }

    public static HttpClient CreateClientAs(this WebApplicationFactory<Program> factory, TestUser? user)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        if (user is null) return client;

        client.DefaultRequestHeaders.Add(SignedInHeader, "true");
        if (user.Group is not null)
        {
            client.DefaultRequestHeaders.Add(GroupHeader, user.Group);
            client.DefaultRequestHeaders.Add(AreaHeader, user.AreaId.ToString());
            client.DefaultRequestHeaders.Add(UserHeader, user.UserId.ToString());
        }

        return client;
    }
}

public sealed class PassThroughClaims : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) => Task.FromResult(principal);
}

/// <summary>Sets context.User from the X-Test-* headers ahead of the real authentication middleware.</summary>
public sealed class HeaderIdentityStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, pipeline) =>
        {
            if (context.Request.Headers.ContainsKey(TestIdentity.SignedInHeader))
            {
                var identity = new ClaimsIdentity("test");
                if (context.Request.Headers.TryGetValue(TestIdentity.GroupHeader, out var group))
                {
                    var area = context.Request.Headers[TestIdentity.AreaHeader].ToString();
                    identity.AddClaim(new Claim(AppClaimTypes.GroupName, group.ToString()));
                    identity.AddClaim(new Claim(AppClaimTypes.UserDbId, context.Request.Headers[TestIdentity.UserHeader].ToString()));
                    identity.AddClaim(new Claim(AppClaimTypes.GroupId, GroupCode(group.ToString())));
                    identity.AddClaim(new Claim(AppClaimTypes.UserAreaId, area));
                    identity.AddClaim(new Claim(AppClaimTypes.UserArea, "Area " + area));
                    identity.AddClaim(new Claim(AppClaimTypes.UserName, "Test User"));
                }

                context.User = new ClaimsPrincipal(identity);
            }

            await pipeline();
        });
        next(app);
    };

    private static string GroupCode(string group) => group switch
    {
        "Customer" => "1",
        "Histopathology User" => "2",
        _ => "3",
    };
}
