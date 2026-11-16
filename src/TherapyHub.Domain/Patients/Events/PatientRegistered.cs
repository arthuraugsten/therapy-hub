using TherapyHub.BuildingBlocks.Domain.Abstractions;
using TherapyHub.BuildingBlocks.Domain.Common;

namespace TherapyHub.Domain.Patients.Events;

/// <summary>Raised when a patient is registered. Carries IDs only, never personal data.</summary>
public sealed record PatientRegistered(PatientId PatientId, PsychologistId PsychologistId, DateTimeOffset OccurredAt)
    : IDomainEvent;