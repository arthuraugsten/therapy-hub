using Microsoft.EntityFrameworkCore;

using TherapyHub.Application.Abstractions.Data;
using TherapyHub.Domain.Abstractions;
using TherapyHub.Domain.Patients;
using TherapyHub.Infrastructure.DomainEvents;

namespace TherapyHub.Infrastructure.Persistence;

public sealed class TherapyHubDbContext(
    DbContextOptions<TherapyHubDbContext> options,
    IDomainEventDispatcher domainEventDispatcher) : DbContext(options), IUnitOfWork
{
    /// <summary>Name under "ConnectionStrings" in configuration.</summary>
    public const string ConnectionStringName = "TherapyHub";

    public DbSet<Patient> Patients => Set<Patient>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<IAggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();
        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatched after commit: handlers see persisted state. Use an outbox if handlers must be guaranteed to run.
        await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TherapyHubDbContext).Assembly);
}