using Microsoft.EntityFrameworkCore;

using TherapyHub.Application.Patients;
using TherapyHub.BuildingBlocks.Domain.Common;
using TherapyHub.Domain.Patients;
using TherapyHub.Infrastructure.Patients.Persistence;

namespace TherapyHub.Infrastructure.Patients;

internal sealed class PatientQueries : IPatientQueries
{
    private readonly PatientsDbContext _dbContext;

    public PatientQueries(PatientsDbContext dbContext) => _dbContext = dbContext;

    // Projects to the DTO: no entity materialization, no change tracking, only the columns the DTO needs.
    public Task<PatientDto?> GetByIdAsync(PatientId id, PsychologistId psychologistId, CancellationToken cancellationToken) =>
        _dbContext.Patients
            .Where(p => p.Id == id && p.PsychologistId == psychologistId)
            .Select(static p => new PatientDto(
                p.Id.Value,
                p.FullName,
                p.Email.Value,
                p.Cpf == null ? null : p.Cpf.Value,
                p.BirthDate,
                p.RegisteredAt))
            .FirstOrDefaultAsync(cancellationToken);

    // Covered by the primary key; the query filter hides archived patients.
    public Task<bool> ExistsAsync(PatientId id, PsychologistId psychologistId, CancellationToken cancellationToken) =>
        _dbContext.Patients.AnyAsync(p => p.Id == id && p.PsychologistId == psychologistId, cancellationToken);
}