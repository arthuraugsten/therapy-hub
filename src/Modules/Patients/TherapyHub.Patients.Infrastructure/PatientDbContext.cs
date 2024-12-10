using Microsoft.EntityFrameworkCore;
using TherapyHub.Patients.Domain;

namespace TherapyHub.Patients.Infrastructure;

public sealed class PatientDbContext(DbContextOptions<PatientDbContext> options) : DbContext(options)
{
    public const string MigrationHistoryTableName = "__EFMigrationsHistory";
    public const string SchemaName = "Patients";

    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(InfrastructureAssemblyInfo.Assembly);
    }
}
