using TherapyHub.Domain.Patients;
using TherapyHub.Domain.Psychologists;

namespace TherapyHub.Application.Patients;

/// <summary>Read side for patients. Returns DTOs directly (no tracking, no aggregates).</summary>
public interface IPatientQueries
{
    Task<PatientDto?> GetByIdAsync(PatientId id, PsychologistId psychologistId, CancellationToken cancellationToken);
}