using Azure.Identity;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using TherapyHub.BuildingBlocks.Infrastructure.DomainEvents;

namespace TherapyHub.BuildingBlocks.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers what every module's Infrastructure needs: the clock, Data Protection, and the domain event dispatcher.</summary>
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddEncryptionKeys(configuration, environment);

        return services;
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