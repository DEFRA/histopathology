using Histo.Administration;
using Histo.Administration.Interfaces;
using Histo.AuditLog;
using Histo.AuditLog.Interfaces;
using Histo.Histology;
using Histo.Histology.Interfaces;
using Histo.Infrastructure;
using Histo.QualityControl;
using Histo.QualityControl.Interfaces;
using Histo.Submissions;
using Histo.Submissions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Histo.Tests.Unit;

/// <summary>Every module's DI extension must register its repositories and services as resolvable scoped types.</summary>
public class ModuleRegistrationTests
{
    private static ServiceProvider Build(Func<IServiceCollection, IServiceCollection> addModule)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IDbConnectionFactory>());
        services.AddSingleton(Mock.Of<IAppLogger>());
        services.AddSingleton(Mock.Of<Histo.Administration.Interfaces.ILookupService>());
        Assert.Same(services, addModule(services));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [Fact]
    public void AddHistologyModule_RegistersRepositoriesAndServices()
    {
        using var provider = Build(s => s.AddHistologyModule());
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBlockRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHistologyRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBlockTestRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBlockService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHistologyRefService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBlockTestService>());
    }

    [Fact]
    public void AddAdministrationModule_RegistersRepositoriesAndServices()
    {
        using var provider = Build(s => s.AddAdministrationModule());
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ILookupRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserService>());
    }

    [Fact]
    public void AddAuditLogModule_RegistersRepositoryAndService()
    {
        using var provider = Build(s => s.AddAuditLogModule());
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAuditLogRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAuditLogService>());
    }

    [Fact]
    public void AddQualityControlModule_RegistersRepositoryAndService()
    {
        using var provider = Build(s => s.AddQualityControlModule());
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IQCNoteRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IQCNoteService>());
    }

    [Fact]
    public void AddSubmissionsModule_RegistersRepositoriesAndServices()
    {
        using var provider = Build(s => s.AddSubmissionsModule());
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBatchRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISubmissionRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBatchService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISubmissionService>());
    }
}
