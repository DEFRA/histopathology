using Histo.AuditLog.Interfaces;
using Histo.AuditLog.Repositories;
using Histo.AuditLog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Histo.AuditLog;

/// <summary>
/// Self-contained DI registration for the AuditLog module.
/// Call <see cref="AddAuditLogModule"/> from Program.cs — do not register
/// individual types from this module anywhere outside this file.
/// </summary>
public static class AuditLogModule
{
    public static IServiceCollection AddAuditLogModule(this IServiceCollection services)
    {
        // Repositories — internal to this module; not part of the public API
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Services — public module contract consumed by Histo.Web
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }
}
