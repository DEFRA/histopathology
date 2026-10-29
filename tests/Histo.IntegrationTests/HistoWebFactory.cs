using System.Data.Common;
using System.Security.Claims;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Histo.IntegrationTests;

/// <summary>One host for the whole run: Program.cs sets Serilog's static logger, so concurrent hosts race on it.</summary>
[CollectionDefinition(Name)]
public sealed class HostCollection : ICollectionFixture<HistoWebFactory>
{
    public const string Name = "Histo host";
}

/// <summary>Who the fake auth handler signs the request in as. A null <see cref="Group"/> is a signed-in user with no tblUser record.</summary>
public sealed record TestUser(string? Group, int AreaId = 0, int UserId = 1)
{
    public static TestUser Customer(int areaId) => new("Customer", areaId);
    public static readonly TestUser Histo = new("Histopathology User", 5);
    public static readonly TestUser Maintenance = new("Maintenance", 5);
    public static readonly TestUser UnknownUser = new((string?)null);
}

/// <summary>
/// Hosts the real Histo.Web pipeline with Entra ID replaced by a header-driven identity and the
/// database replaced by an in-memory batch lookup plus a connection factory that always throws.
/// </summary>
public sealed class HistoWebFactory : WebApplicationFactory<Program>
{
    public const string GroupHeader = "X-Test-Group";
    public const string AreaHeader = "X-Test-Area";
    public const string UserHeader = "X-Test-User";
    public const string SignedInHeader = "X-Test-SignedIn";

    static HistoWebFactory()
    {
        // Program.cs reads these eagerly, before ConfigureAppConfiguration callbacks apply.
        Environment.SetEnvironmentVariable("ConnectionStrings__HistologyDb", "Server=unused;Database=unused");
        Environment.SetEnvironmentVariable("Saml2__IdPMetadataUrl", "");
    }

    public Mock<IBatchService> Batches { get; } = new();

    public void AddBatch(int batchId, int areaCode) =>
        Batches.Setup(b => b.GetByIdAsync(batchId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Batch { ID = batchId, UserAreaCode = areaCode });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory>(new NoDatabase());

            services.RemoveAll<IClaimsTransformation>();
            services.AddScoped<IClaimsTransformation, PassThroughClaims>();

            services.RemoveAll<IBatchService>();
            services.AddSingleton(Batches.Object);

            StubRemainingModuleServices(services);

            services.AddSingleton<IStartupFilter, HeaderIdentityStartupFilter>();
        });
    }

    private static readonly Type[] RealServices =
        [typeof(IDbConnectionFactory), typeof(IBatchService), typeof(Histo.Web.Services.ISessionService), typeof(IAppLogger)];

    // Every other module interface becomes an empty loose mock, so allowed requests run the full
    // page and view pipeline without a database.
    private static void StubRemainingModuleServices(IServiceCollection services)
    {
        var types = services
            .Select(d => d.ServiceType)
            .Where(t => t is { IsInterface: true, IsGenericTypeDefinition: false }
                        && t.Assembly.GetName().Name!.StartsWith("Histo.", StringComparison.Ordinal)
                        && !RealServices.Contains(t))
            .Distinct()
            .ToList();

        foreach (var type in types)
        {
            var mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type), MockBehavior.Loose)!;
            mock.DefaultValue = DefaultValue.Mock;
            services.RemoveAll(type);
            services.AddSingleton(type, mock.Object);
        }
    }

    public HttpClient CreateClientAs(TestUser? user)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
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

    private sealed class NoDatabase : IDbConnectionFactory
    {
        public DbConnection CreateConnection() =>
            throw new InvalidOperationException("Integration tests must not reach a database.");
    }

    private sealed class PassThroughClaims : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) => Task.FromResult(principal);
    }

    private sealed class HeaderIdentityStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, pipeline) =>
            {
                if (context.Request.Headers.ContainsKey(SignedInHeader))
                {
                    var identity = new ClaimsIdentity("test");
                    if (context.Request.Headers.TryGetValue(GroupHeader, out var group))
                    {
                        var area = context.Request.Headers[AreaHeader].ToString();
                        identity.AddClaim(new Claim(AppClaimTypes.GroupName, group.ToString()));
                        identity.AddClaim(new Claim(AppClaimTypes.UserDbId, context.Request.Headers[UserHeader].ToString()));
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
}
