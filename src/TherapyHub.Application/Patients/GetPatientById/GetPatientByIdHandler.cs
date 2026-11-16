using TherapyHub.Application.Abstractions.Authentication;
using TherapyHub.Application.Abstractions.Messaging;
using TherapyHub.Domain.Abstractions;
using TherapyHub.Domain.Patients;

namespace TherapyHub.Application.Patients.GetPatientById;

internal sealed class GetPatientByIdHandler(
    IPatientQueries queries,
    ICurrentUser currentUser) : IQueryHandler<GetPatientByIdQuery, PatientDto>
{
    public async Task<Result<PatientDto>> HandleAsync(GetPatientByIdQuery query, CancellationToken cancellationToken)
    {
        // Scoped to the current psychologist: someone else's patient is reported as "not found".
        var patient = await queries.GetByIdAsync(
            new PatientId(query.PatientId),
            currentUser.PsychologistId,
            cancellationToken);

        return patient is null ? PatientErrors.NotFound : patient;
    }
}