using TherapyHub.BuildingBlocks.Domain.Abstractions;

namespace TherapyHub.BuildingBlocks.Application.Abstractions.Messaging;

public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}