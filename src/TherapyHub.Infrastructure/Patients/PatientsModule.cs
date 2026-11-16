using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

using TherapyHub.Application.Patients;
using TherapyHub.BuildingBlocks.Infrastructure.Persistence;
using TherapyHub.Infrastructure.Patients.Persistence;

namespace TherapyHub.Infrastructure.Patients;

public static class PatientsModule
{
    /// <summary>Registers the Patients module's persistence: its DbContext, unit of work, repository, and queries.</summary>
    public static IServiceCollection AddPatientsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PatientsDbContext>(options => options.UseSqlServer(
            connectionString,
            static sql => sql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, PatientsDbContext.Schema)));

        services.AddScoped<IPatientsUnitOfWork>(static sp => sp.GetRequiredService<PatientsDbContext>());

        // Enumerable registration: hosts and tests migrate every module through GetServices<ModuleDbContext>().
        services.AddScoped<ModuleDbContext>(static sp => sp.GetRequiredService<PatientsDbContext>());

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientQueries, PatientQueries>();

        return services;
    }
}