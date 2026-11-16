using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using TherapyHub.BuildingBlocks.Infrastructure;
using TherapyHub.Infrastructure.Patients;

namespace TherapyHub.Infrastructure;

/// <summary>Composition root for Infrastructure: shared building blocks, then each module.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        const string name = DatabaseSettings.ConnectionStringName;

        var connectionString = configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException(
                $"Connection string '{name}' is not configured. Set it with " +
                $"'dotnet user-secrets set ConnectionStrings:{name} \"<value>\" --project src/TherapyHub.Api'.");

        services.AddBuildingBlocksInfrastructure(configuration, environment);
        services.AddPatientsModule(connectionString);

        return services;
    }
}