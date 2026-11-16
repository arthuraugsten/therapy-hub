using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TherapyHub.Api.IntegrationTests.Infrastructure;
using TherapyHub.BuildingBlocks.Infrastructure.Persistence;

namespace TherapyHub.Api.IntegrationTests.Patients;

/// <summary>Checks where the Patients module's tables live in the migrated database (schema per module).</summary>
public sealed class PatientsSchemaTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Migrations_PatientsTable_IsInPatientsSchema()
    {
        var schemas = await TableSchemasAsync("Patients");

        Assert.Equal(["patients"], schemas);
    }

    [Fact]
    public async Task Migrations_MigrationsHistoryTable_IsInPatientsSchema()
    {
        var schemas = await TableSchemasAsync("__EFMigrationsHistory");

        Assert.Equal(["patients"], schemas);
    }

    [Fact]
    public async Task Migrations_AllModules_CreateNoTablesInDbo()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetServices<ModuleDbContext>().First();

        var dboTables = await dbContext.Database
            .SqlQuery<string>($"SELECT TABLE_NAME AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'")
            .ToListAsync();

        Assert.Empty(dboTables);
    }

    [Fact]
    public async Task TestConnection_Database_IsDedicatedTestDatabaseNotMaster()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetServices<ModuleDbContext>().First();

        var databaseName = await dbContext.Database
            .SqlQuery<string>($"SELECT DB_NAME() AS Value")
            .SingleAsync();

        Assert.NotEqual("master", databaseName, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(ApiFactory.DatabaseName, databaseName);
    }

    private async Task<List<string>> TableSchemasAsync(string tableName)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetServices<ModuleDbContext>().First();

        return await dbContext.Database
            .SqlQuery<string>(
                $"SELECT TABLE_SCHEMA AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {tableName}")
            .ToListAsync();
    }
}