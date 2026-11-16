using Azure.Identity;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using TherapyHub.Application.Abstractions.Data;
using TherapyHub.Application.Patients;
using TherapyHub.Infrastructure.DomainEvents;
using TherapyHub.Infrastructure.Patients;
using TherapyHub.Infrastructure.Persistence;

namespace TherapyHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddPersistence(configuration);
        services.AddEncryptionKeys(configuration, environment);

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        const string name = TherapyHubDbContext.ConnectionStringName;

        var connectionString = configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException(
                $"Connection string '{name}' is not configured. Set it with " +
                $"'dotnet user-secrets set ConnectionStrings:{name} \"<value>\" --project src/TherapyHub.Api'.");

        services.AddDbContext<TherapyHubDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TherapyHubDbContext>());
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientQueries, PatientQueries>();
    }

    /// <summary>
    /// Data Protection keys encrypt clinical content. Never store them in the database or the repo.
    /// Losing the keys means losing the clinical notes.
    /// </summary>
    private static void AddEncryptionKeys(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName(DataProtectionSettings.ApplicationName);

        if (environment.IsProduction())
        {
            var blobUri = Required(configuration, DataProtectionSettings.BlobUriKey);
            var keyVaultKeyUri = Required(configuration, DataProtectionSettings.KeyVaultKeyUriKey);
            var credential = new DefaultAzureCredential();

            dataProtection
                .PersistKeysToAzureBlobStorage(new Uri(blobUri), credential)
                .ProtectKeysWithAzureKeyVault(new Uri(keyVaultKeyUri), credential);
        }
        else
        {
            var keysPath = configuration[DataProtectionSettings.KeysPathKey] ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                DataProtectionSettings.ApplicationName,
                DataProtectionSettings.DefaultKeysFolder);

            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Configuration value '{key}' is required in production.");
}