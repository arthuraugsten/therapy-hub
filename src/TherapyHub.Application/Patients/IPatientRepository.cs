using TherapyHub.Domain.Common;
using TherapyHub.Domain.Patients;
using TherapyHub.Domain.Psychologists;

namespace TherapyHub.Application.Patients;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(PatientId id, PsychologistId psychologistId, CancellationToken cancellationToken);

    Task<bool> IsEmailInUseAsync(PsychologistId psychologistId, Email email, CancellationToken cancellationToken);

    void Add(Patient patient);
}