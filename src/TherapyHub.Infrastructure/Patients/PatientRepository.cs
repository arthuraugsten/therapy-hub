using Microsoft.EntityFrameworkCore;

using TherapyHub.Application.Patients;
using TherapyHub.BuildingBlocks.Domain.Common;
using TherapyHub.Domain.Patients;
using TherapyHub.Infrastructure.Patients.Persistence;

namespace TherapyHub.Infrastructure.Patients;

internal sealed class PatientRepository : IPatientRepository
{
    private readonly PatientsDbContext _dbContext;

    public PatientRepository(PatientsDbContext dbContext) => _dbContext = dbContext;

    public Task<Patient?> GetByIdAsync(PatientId id, PsychologistId psychologistId, CancellationToken cancellationToken) =>
        _dbContext.Patients.FirstOrDefaultAsync(
            p => p.Id == id && p.PsychologistId == psychologistId,
            cancellationToken);

    public Task<bool> IsEmailInUseAsync(PsychologistId psychologistId, Email email, CancellationToken cancellationToken) =>
        _dbContext.Patients
            .IgnoreQueryFilters() // Archived patients still hold their email (unique index).
            .AnyAsync(p => p.PsychologistId == psychologistId && p.Email == email, cancellationToken);

    public void Add(Patient patient) => _dbContext.Patients.Add(patient);
}