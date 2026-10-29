using System.Data.Common;
using Histo.Infrastructure;
using Histo.Submissions.Interfaces;
using Histo.Submissions.Models;
using Histo.TestSupport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

/// <summary>
/// Hosts the real Histo.Web pipeline with Entra ID replaced by a header-driven identity and the
/// database replaced by an in-memory batch lookup plus a connection factory that always throws.
/// </summary>
public sealed class HistoWebFactory : WebApplicationFactory<Program>
{
    static HistoWebFactory() => TestIdentity.UseHostSettings("Server=unused;Database=unused");

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

    private sealed class NoDatabase : IDbConnectionFactory
    {
        public DbConnection CreateConnection() =>
            throw new InvalidOperationException("Integration tests must not reach a database.");
    }
}
