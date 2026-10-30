using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Histo.E2ETests;

[CollectionDefinition(Name)]
public sealed class E2eCollection : ICollectionFixture<E2eHost>
{
    public const string Name = "E2E host";
}

/// <summary>The real app and the real data layer over a copy of a deployed database; only sign-in is faked.</summary>
public sealed class E2eHost : IAsyncLifetime
{
    private sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IClaimsTransformation>();
                services.AddScoped<IClaimsTransformation, PassThroughClaims>();
                services.AddSingleton<IStartupFilter, HeaderIdentityStartupFilter>();
            });
        }
    }

    private Factory? _factory;

    public static bool Enabled => E2eDatabase.Enabled;
    public E2eDatabase Database { get; private set; } = null!;
    public WebApplicationFactory<Program> App => _factory!;

    public int CustomerArea { get; private set; }
    public int OtherArea { get; private set; }
    public int CustomerUserId { get; private set; }
    public int OwnBatchId { get; private set; }
    public int OtherAreaBatchId { get; private set; }

    public async ValueTask InitializeAsync()
    {
        if (!Enabled) return;

        Database = await E2eDatabase.CreateAsync();

        // Areas with the most batches in the copy: one for the Customer, a different one for "someone else".
        CustomerArea = 3;
        OtherArea = 5;
        CustomerUserId = await Database.ScalarAsync<int>("SELECT TOP 1 ID FROM [User] WHERE Active = 1 AND UserArea = @a ORDER BY ID", ("@a", CustomerArea));
        OwnBatchId = await Database.ScalarAsync<int>("SELECT MAX(ID) FROM Batch WHERE SubmittedArea = @a", ("@a", CustomerArea.ToString()));
        OtherAreaBatchId = await Database.ScalarAsync<int>("SELECT MAX(ID) FROM Batch WHERE SubmittedArea = @a", ("@a", OtherArea.ToString()));
        Assert.True(CustomerUserId > 0 && OwnBatchId > 0 && OtherAreaBatchId > 0, "The source database has no suitable users or batches in the chosen areas.");

        TestIdentity.UseHostSettings(E2eDatabase.ConnectionString);
        _factory = new Factory();
    }

    public async ValueTask DisposeAsync()
    {
        _factory?.Dispose();
        if (Enabled && Database is not null) await Database.DisposeAsync();
    }
}
