using Histo.Submissions.Interfaces;
using Histo.Submissions.Repositories;
using Histo.Submissions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Histo.Submissions;

/// <summary>
/// Self-contained DI registration for the Submissions module.
/// Call <see cref="AddSubmissionsModule"/> from Program.cs — do not register
/// individual types from this module anywhere outside this file.
/// </summary>
public static class SubmissionsModule
{
    public static IServiceCollection AddSubmissionsModule(this IServiceCollection services)
    {
        // Repositories — internal to this module; not part of the public API
        services.AddScoped<IBatchRepository, BatchRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();

        // Services — public module contract consumed by Histo.Web
        services.AddScoped<IBatchService, BatchService>();
        services.AddScoped<ISubmissionService, SubmissionService>();

        return services;
    }
}
