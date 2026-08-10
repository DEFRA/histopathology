using Histo.QualityControl.Interfaces;
using Histo.QualityControl.Repositories;
using Histo.QualityControl.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Histo.QualityControl;

/// <summary>
/// Self-contained DI registration for the QualityControl module.
/// Call <see cref="AddQualityControlModule"/> from Program.cs — do not register
/// individual types from this module anywhere outside this file.
/// </summary>
public static class QualityControlModule
{
    public static IServiceCollection AddQualityControlModule(this IServiceCollection services)
    {
        // Repositories — internal to this module; not part of the public API
        services.AddScoped<IQCNoteRepository, QCNoteRepository>();

        // Services — public module contract consumed by Histo.Web
        services.AddScoped<IQCNoteService, QCNoteService>();

        return services;
    }
}
