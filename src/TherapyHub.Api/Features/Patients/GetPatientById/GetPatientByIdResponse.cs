using TherapyHub.Application.Patients;

namespace TherapyHub.Api.Features.Patients.GetPatientById;

public sealed record GetPatientByIdResponse(
    Guid Id,
    string FullName,
    string Email,
    string? Cpf,
    DateOnly? BirthDate,
    DateTimeOffset RegisteredAt)
{
    public static GetPatientByIdResponse From(PatientDto patient) => new(
        patient.Id,
        patient.FullName,
        patient.Email,
        patient.Cpf,
        patient.BirthDate,
        patient.RegisteredAt);
}