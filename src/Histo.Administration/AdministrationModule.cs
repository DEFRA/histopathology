using Histo.Administration.Interfaces;
using Histo.Administration.Repositories;
using Histo.Administration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Histo.Administration;

/// <summary>
/// Self-contained DI registration for the Administration module.
/// Call <see cref="AddAdministrationModule"/> from Program.cs — do not register
/// individual types from this module anywhere outside this file.
/// </summary>
public static class AdministrationModule
{
    public static IServiceCollection AddAdministrationModule(this IServiceCollection services)
    {
        // Repositories — internal to this module; not part of the public API
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();

        // Services — public module contract consumed by Histo.Web
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILookupService, LookupService>();

        return services;
    }
}
