using Microsoft.EntityFrameworkCore;

using TherapyHub.Application.Patients;
using TherapyHub.BuildingBlocks.Infrastructure.DomainEvents;
using TherapyHub.BuildingBlocks.Infrastructure.Persistence;
using TherapyHub.Domain.Patients;
using TherapyHub.Infrastructure.Patients.Persistence.Configurations;

namespace TherapyHub.Infrastructure.Patients.Persistence;

/// <summary>The Patients module's DbContext. Owns schema <see cref="Schema"/>, including its migrations history table.</summary>
internal sealed class PatientsDbContext : ModuleDbContext, IPatientsUnitOfWork
{
    public const string Schema = "patients";

    public PatientsDbContext(DbContextOptions<PatientsDbContext> options, IDomainEventDispatcher domainEventDispatcher)
        : base(options, domainEventDispatcher)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Only this module's configurations; other modules' entities live in their own contexts and schemas.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PatientsDbContext).Assembly,
            static type => type.Namespace == typeof(PatientConfiguration).Namespace);
    }
}