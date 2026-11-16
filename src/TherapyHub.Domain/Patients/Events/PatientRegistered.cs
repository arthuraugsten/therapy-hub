using TherapyHub.Domain.Abstractions;
using TherapyHub.Domain.Psychologists;

namespace TherapyHub.Domain.Patients.Events;

/// <summary>Raised when a patient is registered. Carries IDs only, never personal data.</summary>
public sealed record PatientRegistered(PatientId PatientId, PsychologistId PsychologistId, DateTimeOffset OccurredAt)
    : IDomainEvent;