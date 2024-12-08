using TherapyHub.Patients.Application.Repositories;
using TherapyHub.Patients.Domain;

namespace TherapyHub.Patients.Infrastructure.Repositories;

public sealed class PatientRepository(PatientDbContext dbContext) : IPatientRepository
{
    public async Task AddAsync(Patient patient, CancellationToken cancellationToken)
    {
        await dbContext.Patients.AddAsync(patient, cancellationToken);
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
