using Histo.Histology.Interfaces;
using Histo.Histology.Repositories;
using Histo.Histology.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Histo.Histology;

/// <summary>
/// Self-contained DI registration for the Histology module.
/// Call <see cref="AddHistologyModule"/> from Program.cs — do not register
/// individual types from this module anywhere outside this file.
/// </summary>
public static class HistologyModule
{
    public static IServiceCollection AddHistologyModule(this IServiceCollection services)
    {
        // Repositories — internal to this module; not part of the public API
        services.AddScoped<IBlockRepository, BlockRepository>();
        services.AddScoped<IHistologyRepository, HistologyRepository>();
        services.AddScoped<IBlockTestRepository, BlockTestRepository>();

        // Services — public module contract consumed by Histo.Web
        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<IHistologyRefService, HistologyRefService>();
        services.AddScoped<IBlockTestService, BlockTestService>();

        return services;
    }
}
