using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.MsSql;

using TherapyHub.Infrastructure;
using TherapyHub.Infrastructure.Persistence;

namespace TherapyHub.Api.IntegrationTests.Infrastructure;

/// <summary>Runs the API against a real SQL Server in Docker (Testcontainers). Docker must be running.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestingEnvironment = "Testing";

    // Keep in sync with the image in compose.yaml.
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";
    private const string TempKeysFolder = "therapy-hub-tests";
    private const string ConnectionStringKey = "ConnectionStrings:" + TherapyHubDbContext.ConnectionStringName;

    private readonly MsSqlContainer _database = new MsSqlBuilder(SqlServerImage).Build();
    private readonly string _keysPath = Path.Combine(Path.GetTempPath(), TempKeysFolder, Guid.NewGuid().ToString());

    public HttpClient CreateClientFor(Guid psychologistId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.PsychologistHeader, psychologistId.ToString());
        return client;
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TherapyHubDbContext>().Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestingEnvironment);
        builder.UseSetting(ConnectionStringKey, _database.GetConnectionString());
        builder.UseSetting(DataProtectionSettings.KeysPathKey, _keysPath);

        builder.ConfigureTestServices(services =>
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));
    }
}