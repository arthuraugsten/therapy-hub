using TherapyHub.BuildingBlocks.Domain.Common;
using TherapyHub.Domain.Patients;

namespace TherapyHub.Application.Patients;

/// <summary>Read side for patients. Returns DTOs directly (no tracking, no aggregates).</summary>
public interface IPatientQueries
{
    Task<PatientDto?> GetByIdAsync(PatientId id, PsychologistId psychologistId, CancellationToken cancellationToken);

    /// <summary>True when an active (not archived) patient with this ID belongs to the psychologist.</summary>
    Task<bool> ExistsAsync(PatientId id, PsychologistId psychologistId, CancellationToken cancellationToken);
}