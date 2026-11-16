using TherapyHub.Application.Patients.PublicApi;
using TherapyHub.BuildingBlocks.Application.Abstractions.Authentication;
using TherapyHub.Domain.Patients;

namespace TherapyHub.Application.Patients;

internal sealed class PatientsApi : IPatientsApi
{
    private readonly IPatientQueries _queries;
    private readonly ICurrentUser _currentUser;

    public PatientsApi(IPatientQueries queries, ICurrentUser currentUser)
    {
        _queries = queries;
        _currentUser = currentUser;
    }

    public Task<bool> ExistsAsync(Guid patientId, CancellationToken cancellationToken) =>
        _queries.ExistsAsync(new PatientId(patientId), _currentUser.PsychologistId, cancellationToken);
}