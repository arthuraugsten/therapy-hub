using TherapyHub.Application.Abstractions.Authentication;
using TherapyHub.Application.Abstractions.Messaging;
using TherapyHub.Domain.Abstractions;
using TherapyHub.Domain.Patients;

namespace TherapyHub.Application.Patients.GetPatientById;

internal sealed class GetPatientByIdHandler : IQueryHandler<GetPatientByIdQuery, PatientDto>
{
    private readonly IPatientQueries _queries;
    private readonly ICurrentUser _currentUser;

    public GetPatientByIdHandler(IPatientQueries queries, ICurrentUser currentUser)
    {
        _queries = queries;
        _currentUser = currentUser;
    }

    public async Task<Result<PatientDto>> HandleAsync(GetPatientByIdQuery query, CancellationToken cancellationToken)
    {
        // Scoped to the current psychologist: someone else's patient is reported as "not found".
        var patient = await _queries.GetByIdAsync(
            new PatientId(query.PatientId),
            _currentUser.PsychologistId,
            cancellationToken);

        return patient is null ? PatientErrors.NotFound : patient;
    }
}