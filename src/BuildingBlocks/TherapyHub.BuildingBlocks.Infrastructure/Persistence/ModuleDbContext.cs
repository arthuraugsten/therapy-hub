using Microsoft.EntityFrameworkCore;

using TherapyHub.BuildingBlocks.Application.Abstractions.Data;
using TherapyHub.BuildingBlocks.Domain.Abstractions;
using TherapyHub.BuildingBlocks.Infrastructure.DomainEvents;

namespace TherapyHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base for every module's DbContext (one per module, each in its own schema). Saves changes, then dispatches the
/// domain events raised by the tracked aggregates. Modules also register their context as
/// <see cref="ModuleDbContext"/> so hosts and tests can migrate every module without knowing their types.
/// </summary>
public abstract class ModuleDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    protected ModuleDbContext(DbContextOptions options, IDomainEventDispatcher domainEventDispatcher)
        : base(options) => _domainEventDispatcher = domainEventDispatcher;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Plain loop: no allocations when no aggregate raised events (the common case for updates).
        List<IDomainEvent>? domainEvents = null;
        foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
        {
            var aggregate = entry.Entity;
            if (aggregate.DomainEvents.Count == 0)
                continue;

            (domainEvents ??= []).AddRange(aggregate.DomainEvents);
            aggregate.ClearDomainEvents();
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatched after commit: handlers see persisted state. Use an outbox if handlers must be guaranteed to run.
        if (domainEvents is not null)
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return result;
    }
}