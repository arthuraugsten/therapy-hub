using TherapyHub.Application.Abstractions.Messaging;

namespace TherapyHub.Application.Patients.GetPatientById;

public sealed record GetPatientByIdQuery(Guid PatientId) : IQuery<PatientDto>;