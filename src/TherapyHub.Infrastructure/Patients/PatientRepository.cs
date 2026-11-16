using Microsoft.EntityFrameworkCore;

using TherapyHub.Application.Patients;
using TherapyHub.Domain.Common;
using TherapyHub.Domain.Patients;
using TherapyHub.Domain.Psychologists;
using TherapyHub.Infrastructure.Persistence;

namespace TherapyHub.Infrastructure.Patients;

internal sealed class PatientRepository(TherapyHubDbContext dbContext) : IPatientRepository
{
    public Task<Patient?> GetByIdAsync(PatientId id, PsychologistId psychologistId, CancellationToken cancellationToken) =>
        dbContext.Patients.FirstOrDefaultAsync(
            p => p.Id == id && p.PsychologistId == psychologistId,
            cancellationToken);

    public Task<bool> IsEmailInUseAsync(PsychologistId psychologistId, Email email, CancellationToken cancellationToken) =>
        dbContext.Patients
            .IgnoreQueryFilters() // Archived patients still hold their email (unique index).
            .AnyAsync(p => p.PsychologistId == psychologistId && p.Email == email, cancellationToken);

    public void Add(Patient patient) => dbContext.Patients.Add(patient);
}