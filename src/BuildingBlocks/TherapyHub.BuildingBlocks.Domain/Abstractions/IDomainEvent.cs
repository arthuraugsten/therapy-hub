namespace TherapyHub.BuildingBlocks.Domain.Abstractions;

/// <summary>Something that happened in the domain. Dispatched by Infrastructure after the changes are saved.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}