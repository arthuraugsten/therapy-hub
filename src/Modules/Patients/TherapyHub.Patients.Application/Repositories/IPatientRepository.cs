using TherapyHub.Patients.Domain;

namespace TherapyHub.Patients.Application.Repositories;

public interface IPatientRepository
{
    Task AddAsync(Patient patient, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
