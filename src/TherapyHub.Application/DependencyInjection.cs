using Microsoft.Extensions.DependencyInjection;

using TherapyHub.Application.Patients;
using TherapyHub.Application.Patients.PublicApi;
using TherapyHub.BuildingBlocks.Application;

namespace TherapyHub.Application;

public static class DependencyInjection
{
    /// <summary>Registers every module's handlers in this assembly and each module's public API.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddHandlersFromAssembly(typeof(DependencyInjection).Assembly);

        // Patients module: the only surface other modules may call.
        services.AddScoped<IPatientsApi, PatientsApi>();

        return services;
    }
}