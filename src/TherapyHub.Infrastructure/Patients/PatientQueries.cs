using Microsoft.EntityFrameworkCore;

using TherapyHub.Application.Patients;
using TherapyHub.Domain.Patients;
using TherapyHub.Domain.Psychologists;
using TherapyHub.Infrastructure.Persistence;

namespace TherapyHub.Infrastructure.Patients;

internal sealed class PatientQueries(TherapyHubDbContext dbContext) : IPatientQueries
{
    public async Task<PatientDto?> GetByIdAsync(PatientId id, PsychologistId psychologistId, CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.PsychologistId == psychologistId, cancellationToken);

        return patient is null ? null : PatientDto.From(patient);
    }
}